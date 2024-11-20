using Npgsql;
using System.Diagnostics;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AnalisadoDeCodigo.Apresentacao
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //if (!Debugger.IsAttached)
            //    Debugger.Launch();

            using StreamReader reader = new("../AnalisadoDeCodigo.Apresentacao/AppSettings.json");
            var json = reader.ReadToEnd();
            JsonConfig jsonConfig = JsonConvert.DeserializeObject<JsonConfig>(json);

            string codigoFonte = "";
            int idAluno = 0; int idExercicio = 0; int idSubmissao = 0;

            if (args.Length > 0)
            {
                codigoFonte = args[0];
                idExercicio = Int32.Parse(args[1]); 
                idSubmissao = Int32.Parse(args[2]); 
                idAluno = Int32.Parse(args[3]);
            }
            else
            {
                Console.WriteLine("Falha: Não foi possível criar o arquivo para compilação, pois, não há argumentos fornecidos como entrada!");
                return;
            }

            //Criando o novo arquivo de texto
            File.WriteAllText(jsonConfig.PathCodigoAluno, codigoFonte);
          
            try
            {
                var processoCompilar = new Process{
                    StartInfo = new ProcessStartInfo
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        FileName = jsonConfig.Compilador, //FileName aceita arquivos comuns e executáveis, por isso aceita o gcc
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Arguments = $"\"{jsonConfig.PathCodigoAluno}\" -o \"{jsonConfig.NomeExecutavel}\""
                    }
                };

                processoCompilar.Start();
                processoCompilar.WaitForExit();

                var resultadoCompilacaoError = processoCompilar.StandardError.ReadToEnd();
                var objetoDb = new ExecutarComandosNpgsql(jsonConfig.ConnectionString.SupabaseConnection);

                if (!string.IsNullOrWhiteSpace(resultadoCompilacaoError))
                {
                    objetoDb.ExecutarUpdate(string.Format(jsonConfig.UpdateQuerie, 2, 0, idSubmissao));
                    throw new Exception("Não foi possível compilar o código fonte fornecido pelo aluno! O erro de compilação foi o seguinte:\n\n" + resultadoCompilacaoError);
                    //Console.WriteLine("Não foi possível compilar o código fonte fornecido pelo aluno! O erro de compilação foi o seguinte:\n\n" + resultadoCompilacaoError);
                }
                else
                {
                    try
                    {
                        var casosTesteDb = objetoDb.ExecutarQuery(string.Format(jsonConfig.QuerieCasosTeste, idExercicio));

                        if (casosTesteDb != null && casosTesteDb.Count > 0) // Se existem casos de teste
                        {
                            int acertos = 0;

                            foreach (var casoTeste in casosTesteDb)
                            {
                                var processoExecutarC = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        CreateNoWindow = false,
                                        UseShellExecute = false,
                                        FileName = jsonConfig.NomeExecutavel + ".exe",
                                        RedirectStandardOutput = true,
                                        RedirectStandardError = true,
                                        RedirectStandardInput = true
                                    }
                                };

                                string entradaFormatada = FormatarCasosTeste(casoTeste);

                                processoExecutarC.Start();

                                using (var writer = new StreamWriter(processoExecutarC.StandardInput.BaseStream))
                                {
                                    writer.Write(entradaFormatada);
                                    writer.Flush(); // força o envio dos dados do teclado
                                }

                                processoExecutarC.WaitForExit();
                                var resultadoExecucao = processoExecutarC.StandardOutput.ReadToEnd();

                                if (processoExecutarC.ExitCode == 0)
                                {
                                    if (resultadoExecucao == casoTeste.expected_output)
                                    {
                                        ++acertos;
                                    }
                                }
                            }

                            //Resultado final da minha execução
                            Console.WriteLine($"{acertos}/{casosTesteDb.Count}");

                            //Status do exercício = 1 --> Sucesso, 2 --> Não deu certo (compilação, por exemplo)
                            objetoDb.ExecutarUpdate(string.Format(jsonConfig.UpdateQuerie, 1, acertos, idSubmissao), true);
                        }
                        else
                        {
                            objetoDb.ExecutarUpdate(string.Format(jsonConfig.UpdateQuerie, 2, 0, idSubmissao));
                            throw new Exception("Não é possível fazer a validação do código, pois, nenhum caso de teste está cadastrado!");
                            //Console.WriteLine("Não é possível fazer a validação do código, pois, nenhum caso de teste está cadastrado!");
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Houve algum problema na execução do código C enviado pelo aluno!", ex.Message);
                        return;
                    }
                }

                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Não foi possível realizar a compilação do código fonte inserido pelo aluno!", ex.Message);
                return; 
            }

            return;
        }

        static string FormatarCasosTeste(TestCase casoTeste)
        {
            string[] entrada = casoTeste.input.Split(',');
            string entradaFormatada = "";

            foreach (string entradaSimples in entrada)
            {
                entradaFormatada += entradaSimples + Environment.NewLine;
            }

            return entradaFormatada;
        }
    }

    internal class TestCase
    {
        public int id {get;set;}
        public int exercise_id { get;set;}
        public string input { get; set;} 
        public string expected_output {get; set;}
    }

    internal class ExerciseSubmission
    {
        public int submission_id { get; set; }
        public int student_id {get; set;}
        public int exercise_id { get; set; }
        public int exercise_status { get; set; }
        public string student_code { get; set; }
        public int correct_test_cases { get; set; }
        public DateTimeOffset submission_date { get; set; }
    }

    internal class ExecutarComandosNpgsql
    {
        public readonly string connectionString = "";
        public readonly NpgsqlDataSource dataSource;

        public ExecutarComandosNpgsql(string connection)
        {
            connectionString = connection;
            dataSource = NpgsqlDataSource.Create(connectionString);
        }

        public List<TestCase> ExecutarQuery(string query)
        {
            List<TestCase> casosTeste = new List<TestCase>();

            try
            {
                using var command = dataSource.CreateCommand(query);
                using var reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                    {
                        var idSubmissao = (int)reader.GetValue(0);
                        var idExercicio = (int)reader.GetValue(1);
                        var entrada = (string)reader.GetValue(2);
                        var saida = (string)reader.GetValue(3);

                        casosTeste.Add(new TestCase
                        {
                            id = idSubmissao,
                            exercise_id = idExercicio,
                            input = entrada,
                            expected_output = saida
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Não foi possível ler os valores da tabela especificada!\n Erro: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Não foi possível completar a conexão com o Banco de dados!\n Erro: {ex.Message}");
            }

            return casosTeste;
        }

        public async Task<bool> ExecutarInsert(string sqlCommand)
        {
            try
            {
                await using (var cmd = dataSource.CreateCommand(sqlCommand))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }

        public bool ExecutarUpdate(string sqlCommand, bool sucessCompilation = false)
        {
            try
            {
                using (var cmd = dataSource.CreateCommand(sqlCommand))
                {
                    if(sucessCompilation)
                        cmd.ExecuteNonQueryAsync();
                    else
                        cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }
    }

    internal class JsonConfig
    {
        public ConnectionString ConnectionString { get; set; }
        public string PathCodigoAluno { get; set; }
        public string NomeExecutavel { get; set; }
        public string QuerieCasosTeste { get; set; }
        public string QuerieSubmissao { get; set; }
        public string UpdateQuerie { get; set; }
        public string Compilador { get; set; }
    }

    internal class ConnectionString
    {
        public string SupabaseConnection { get; set; }
    }

}

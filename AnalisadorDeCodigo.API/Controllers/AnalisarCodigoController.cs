using AnalisadorDeCodigo.Infra.Interfaces;
using AnalisadorDeCodigo.API.Classes;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Collections.Immutable;

namespace AnalisadorDeCodigo.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AnalisarCodigoController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IAnalisadorCodigo _bancoContext;

        public AnalisarCodigoController(IConfiguration configuration, IAnalisadorCodigo bancoContext)
        {
            _configuration = configuration;
            _bancoContext = bancoContext;
        }

        //string com o código fonte do aluno
        //id dos casos de teste
        //[HttpPost("ValidarCasosDeTest/{codigoFonte}/{idCasoTeste}")]
        [HttpPost("ValidarCasosDeTest")]
        public IActionResult ValidarCasosDeTeste([FromBody] CodigoFonteRequest codigoFonte) //Vínculo  do resultado ao aluno no final do script
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigoFonte.CodigoFonte))
                {
                    throw new Exception();
                }

                string pathValidator = _configuration["PathCodeValidator:Path"]; //Path do script C#
                string fullPath = Path.Combine(pathValidator, "AnalisadoDeCodigo.Apresentacao.exe"); // fica dentro da pasta bin
                string codigoFonteEscapado = codigoFonte
                                            .CodigoFonte
                                            .Replace("\"", "\\\"");

                //CRIANDO E CONFIGURANDO O PROCESSO DE EXECUÇÃO DO MEU SCRIPT C#
                var programa = new Process();
                programa.StartInfo.FileName = fullPath;
                programa.StartInfo.Arguments = $"\"{codigoFonteEscapado}\" {codigoFonte.idExercicio} {codigoFonte.idSubmissao} {codigoFonte.idAluno}";
                programa.StartInfo.RedirectStandardOutput = true;
                programa.StartInfo.RedirectStandardError = true;
                programa.StartInfo.UseShellExecute = false;
                programa.StartInfo.CreateNoWindow = true;

                programa.Start();

                // Ler o output do compilador
                string resultadoCompilacao = programa.StandardOutput.ReadToEnd();
                string errosCompilacao = programa.StandardError.ReadToEnd();

                programa.WaitForExit();

                if (programa.ExitCode == 0)
                {
                    Console.WriteLine("Compilação bem-sucedida!");
                    Console.WriteLine(resultadoCompilacao);

                    resultadoCompilacao = resultadoCompilacao.Substring(0, resultadoCompilacao.Length - 2);

                    //Configurando o JSON da minha resposta
                    return StatusCode(200, new { mensagem = "Sucesso ao realizar validações!", resultado = resultadoCompilacao });
                }
                else
                {
                    Console.WriteLine("Erro na compilação!");
                    Console.WriteLine(errosCompilacao);
                    return StatusCode(500, new { mensagem = "Houve erro na compilação do script que valida os casos de teste!" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro ao tentar realizar validações!", erro = ex.Message });
            }
        }
    }
}
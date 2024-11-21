# CODE EVALUATOR SERVICE

Microsserviço para avaliação do código submetido pelo aluno. 

# Comunicação com banco de dados
Para que o microsserviço funcione corretamente, o arquivo Appsettings.json deve estar corretamente configurado com as seguintes propriedades:

{
  "ConnectionString": {
    "SupabaseConnection": ""
  },
  "PathCodigoAluno": "",
  "NomeExecutavel": "",
  "QuerieCasosTeste": "",
  "QuerieSubmissao": "",
  "UpdateQuerie": "",
  "Compilador": ""
}

Como no projeto foi utilizada a plataforma Open Source Supabase, a string de conexão configurada deve ser aquela que corresponder à de comunicação com o banco de dados hospedado na plataforma.
A strig de conexão é fornecida pelo próprio supabase, e deve ser inserida neste parâmetro para que a correta comunicação com  o banco de dados aconteça.

# Local código aluno.c

Esse parâmetro descreve o local onde o arquivo do código do aluno, gerado pelo script de validação, será criado. É importante a correta configuração levando em consideração
os ambientes de debug e de produção, uma vez que os locais de criação do código podem mudar. Essa configuração deve sempre finalizar com o nome do arquivo seguido de .c, para que
o arquivo criado tenha um nome e a extensão de código em linguagem c. 
Ex: Se o código .c do aluno for criado na pasta anterior à atual, e supondo que será dado o nome ao arquivo .c de CodigoAluno, PathCodigoAluno ficar da seguinte maneira:
"PathCodigoAluno" : "..\\CodigoAluno.c" --> windows
ou
"PathCodigoAluno" : "../CodigoAluno.c" --> Linux

# Nome do executável 

Este parâmetro é somente uma configuração para o nome do executável gerado pelo script. O nome que será dado ao executável é de peferência do desenvolvedor.

# Buscando dados da tabela de casos de teste

O parâmetro QuerieCasosTeste descreve a consulta SQL necessária para se realizar a busca de todos os casos de teste cadastrados para um determinado exercício.
O resultado desta querie pode ser um ou mais objetos do banco de dados.

# QuerieSubmissao

Esse parâmetro corresponde à uma versão anterior de inserção no banco de dados de informações relativas à tabela de submissão do aluno, porém, ao final do projeto,
a querie nã se fez mais necessária, podendo ser ignorada completamente.

# UpdateQuerie

Esse parâmetro descreve o código SQL necessário para realizar a inserção da quantidade de casos de testes corretos que o código do aluno obteve, bem como
o status da submissão, sendo 1 para uma validação correta (independente da quantidade de acertos) e 2 para erros de compilação.

# Compilador

Esse parâmetro descreve o tipo de compilador que será utilizado para o código do aluno. Como o sistema está operando basicamente com validações de códigos em 
linguagem C, basta que o parâmetro "gcc" seja especificado.









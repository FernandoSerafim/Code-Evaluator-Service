using Microsoft.EntityFrameworkCore;
using AnalisadorDeCodigo.Infra;
using AnalisadorDeCodigo.Infra.Interfaces;

namespace AnalisadorDeCodigo.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //Configuração do meu contexto do banco de dados
            builder.Services.AddDbContext<BancoContext>(options =>
                 options.UseSqlServer(builder.Configuration.GetConnectionString("AnalisadorConnection")));

            //Configurando a injeção de dependência
            builder.Services.AddScoped<IAnalisadorCodigo, AnalisadorCodigoRepositorio>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            //app.Urls.Add("http://0.0.0.0:5000"); // Escuta em todas as interfaces na porta 5000

            app.Run();
        }
    }
}

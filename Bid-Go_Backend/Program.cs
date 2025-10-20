using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Data.Repositories.Requests;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stripe;
using System.Text;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BidGo",
        Version = "v1"
    });
});

builder.Services.AddDbContext<BidGoDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("default");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });



var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BidGo API v1");
    c.RoutePrefix = "";
});


app.UseExceptionHandler(config =>
{
    config.Run(async context =>
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";

        // Verificar se a exceção é de autorização
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is UnauthorizedAccessException)
        {
            var result = JsonSerializer.Serialize(new { message = "Acesso negado. Você não tem permissão para acessar este recurso." });
            await context.Response.WriteAsync(result);
        }
        else
        {
            // Caso seja outro tipo de erro
            var result = JsonSerializer.Serialize(new { message = "Ocorreu um erro inesperado." });
            await context.Response.WriteAsync(result);
        }
    });
});

app.MapControllers();

app.Run();
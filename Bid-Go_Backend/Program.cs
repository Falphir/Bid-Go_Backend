using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs;
using Bid_Go_Backend.Data.Repositories;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Data.Repositories.Requests;
using Bid_Go_Backend.Data.Repositories.Notifications;
using Bid_Go_Backend.Data.Repositories.Login;
using Bid_Go_Backend.Repositories.BidRepo;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using Stripe;
using Microsoft.Extensions.Options;
using Bid_Go_Backend.Data.Repositories.Login;
using Microsoft.Extensions.Caching.Memory;


var builder = WebApplication.CreateBuilder(args);

// Adicionar controllers
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

//EmailService (SMTP)
builder.Services.AddSingleton<EmailService>(sp =>
    new EmailService(
        smtpHost: "smtp.sapo.pt",
        smtpPort: 587,
        smtpUser: "bidandgo2025@sapo.pt",
        smtpPass: "Bidandgo2025"
    )
);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BidGo",
        Version = "v1"
    });

    // Configuração para usar JWT no Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

// Dependency Injection
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddDbContext<BidGoDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("default");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// JWT Authentication
var key = builder.Configuration["Jwt:Key"];
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    // Política para Drivers
    options.AddPolicy("DriverOnly", policy =>
        policy.RequireClaim("userType", "Driver"));

    // Política para Companies
    options.AddPolicy("CompanyOnly", policy =>
        policy.RequireClaim("userType", "Company"));
});


builder.Services.AddScoped<IBidCRUD, BidsCRUD>();
builder.Services.AddScoped<IRegisterCompanyRepository, RegisterCompanyRepository>();
builder.Services.AddScoped<ITransportRequestRepository, TransportRequestRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();



var app = builder.Build();



app.UseSwagger();
app.UseSwaggerUI(c=>
{

    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BidGo API v1");
c.RoutePrefix = "";
});

// Exception handler
app.UseExceptionHandler(config =>
{
    config.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        var feature = context.Features.Get<IExceptionHandlerFeature>();

        if (feature?.Error is UnauthorizedAccessException)
        {
            context.Response.StatusCode = 401;
            var result = JsonSerializer.Serialize(new { message = "Acesso negado. Você não tem permissão para acessar este recurso." });
            await context.Response.WriteAsync(result);
        }
        else
        {
            context.Response.StatusCode = 500; 
            var result = JsonSerializer.Serialize(new { message = feature?.Error.Message });
            await context.Response.WriteAsync(result);
        }
    });
});

// Autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapHub<NotificationHub>("/notificationHub");
app.MapControllers();

app.Run();

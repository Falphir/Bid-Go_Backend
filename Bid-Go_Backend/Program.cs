using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Data.Repositories.Review;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Repositories;
using Bid_Go_Backend.Data.Repositories.Bids;
using Bid_Go_Backend.Data.Repositories.Chat;
using Bid_Go_Backend.Data.Repositories.Notifications;
using Bid_Go_Backend.Data.Repositories.Payments;
using Bid_Go_Backend.Data.Repositories.Register;
using Bid_Go_Backend.Data.Repositories.Transport_Request;
using Bid_Go_Backend.Repositories.BidRepo;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Repositories.ProfileRepo;
using Bid_Go_Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stripe;
using System.Text;
using System.Text.Json;
using IHistoryRepository = Bid_Go_Backend.Data.Repositories.Interfaces.IHistoryRepository;
using HistoryRepository = Bid_Go_Backend.Data.Repositories.Requests.HistoryRepository;



var builder = WebApplication.CreateBuilder(args);

// Adicionar controllers
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();



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




builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));


var stripeSection = builder.Configuration.GetSection("Stripe");
StripeConfiguration.ApiKey = stripeSection["SecretKey"];
builder.Services.AddScoped<IBidsCRUD, BidsCRUD>();

builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddScoped<IRegisterCompanyRepository, RegisterCompanyRepository>();
builder.Services.AddScoped<ITransportRequestRepository, TransportRequestRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();


builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IChatService, ChatService>();


builder.Services.AddScoped<IAuthService, AuthService>();


builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITransportRequestsPageRepository, TransportRequestsPageRepository>();
builder.Services.AddScoped<IRegisterDriverRepository, RegisterDriverRepository>();
builder.Services.AddTransient<IAutomaticSelectionAlgorithmRepository, AutomaticSelectionAlgorithmRepository>();
builder.Services.AddScoped<IAcceptAndRejectBidManual, AcceptAndRejectBidManual>();
builder.Services.AddScoped<IProfileCRUD, ProfileCRUD>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IHistoryRepository, HistoryRepository>();
builder.Services.AddScoped<IReviewRequestServiceRepository, ReviewRequestServiceRepository>();
builder.Services.AddScoped<ITransportUpdateStatus, TransportUpdateStatusRepository>();

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
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var result = JsonSerializer.Serialize(new
            {
                message = "An unexpected error occurred.",
                error = feature?.Error.Message,
                stack = feature?.Error.StackTrace
            });
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

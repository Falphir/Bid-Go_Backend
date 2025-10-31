using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Repositories.Chat;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Data.Repositories.Transport_Request;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs;
using Bid_Go_Backend.Data.Repositories;
using Bid_Go_Backend.Data.Repositories.Requests;
using Bid_Go_Backend.Data.Repositories.Notifications;
using Bid_Go_Backend.Data.Repositories.Login;
using Bid_Go_Backend.Data.Repositories.Payments;
using Bid_Go_Backend.Repositories.BidRepo;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Repositories.ProfileRepo;
using Bid_Go_Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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



builder.Services.AddScoped<IAcceptAndRejectBidManual, AcceptAndRejectBidManual>();
builder.Services.AddScoped<IProfileCrud, ProfileCRUD>();
builder.Services.AddScoped<IBidCRUD, BidsCRUD>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();


builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));

// 2) já deixar o Stripe a usar a secret
var stripeSection = builder.Configuration.GetSection("Stripe");
StripeConfiguration.ApiKey = stripeSection["SecretKey"];
builder.Services.AddScoped<IBidsCRUD, BidsCRUD>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IRegisterCompanyRepository, RegisterCompanyRepository>();
builder.Services.AddScoped<ITransportRequestRepository, TransportRequestRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITransportRequestsPageRepository, TransportRequestsPageRepository>();




builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c=>
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
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            var result = JsonSerializer.Serialize(new
            {
                message = "Access denied. You are not allowed to access this resource."
            });
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

app.MapHub<NotificationHub>("/notificationHub");
app.MapControllers();

app.Run();

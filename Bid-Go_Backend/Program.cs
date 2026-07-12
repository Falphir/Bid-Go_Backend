using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Repositories.Authorization;
using Bid_Go_Backend.Repositories.Bids;
using Bid_Go_Backend.Repositories.Chat;
using Bid_Go_Backend.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.Login;
using Bid_Go_Backend.Repositories.Notifications;
using Bid_Go_Backend.Repositories.Payments;
using Bid_Go_Backend.Repositories.Profile;
using Bid_Go_Backend.Repositories.Register;
using Bid_Go_Backend.Repositories.Review;
using Bid_Go_Backend.Repositories.Transport_Request;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Auth;
using Bid_Go_Backend.Services.Bids;
using Bid_Go_Backend.Services.Email;
using Bid_Go_Backend.Services.History;
using Bid_Go_Backend.Services.Interfaces;
using Bid_Go_Backend.Services.Payments;
using Bid_Go_Backend.Services.Profile;
using Bid_Go_Backend.Services.Register;
using Bid_Go_Backend.Services.Review;
using Bid_Go_Backend.Services.Transport_Request;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stripe;
using System.Text;
using System.Text.Json;
using HistoryRepository = Bid_Go_Backend.Repositories.History.HistoryRepository;
using IHistoryRepository = Bid_Go_Backend.Repositories.Interfaces.IHistoryRepository;
using ITransportRequestsPageService = Bid_Go_Backend.Services.Transport_Request.ITransportRequestsPageService;



var builder = WebApplication.CreateBuilder(args);

// Container hosts (Railway, Render, Fly) assign the listening port at runtime.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var MyCors = "Frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyCors, p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
    );
});


// Add controllers
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BidGo",
        Version = "v1",
        Description = "BidGo API - Transport bidding platform"
    });

    // Include XML comments (requires <GenerateDocumentationFile>true</GenerateDocumentationFile> in csproj)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Configuration to use JWT in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
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


var connectionString = builder.Configuration.GetConnectionString("default");

if (string.IsNullOrWhiteSpace(connectionString) && builder.Environment.IsDevelopment())
{
    connectionString = "server=localhost;database=bidgo;user=root;password=root";
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No database connection string. Set ConnectionStrings__default in the environment.");
}

// Pinned rather than AutoDetect: AutoDetect opens a connection at startup, which crash-loops
// the container whenever the database is slower to come up than the app.
var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

builder.Services.AddDbContext<BidGoDbContext>(options =>
{
    options.UseMySql(connectionString, serverVersion, mysql => mysql.EnableRetryOnFailure());
});
// =========================================================================

// JWT Authentication
var key = builder.Configuration["Jwt:Key"];
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(key))
{
    throw new InvalidOperationException("No JWT signing key. Set Jwt__Key in the environment.");
}

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
    // Policy for Drivers
    options.AddPolicy("DriverOnly", policy =>
        policy.RequireClaim("userType", "Driver"));

    // Policy for Companies
    options.AddPolicy("CompanyOnly", policy =>
        policy.RequireClaim("userType", "Company"));
});



builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));


var stripeSection = builder.Configuration.GetSection("Stripe");
StripeConfiguration.ApiKey = stripeSection["SecretKey"];

builder.Services.AddHostedService<AutomaticSelectionBackgroundService>();

//Repositories
builder.Services.AddScoped<IBidsService, BidsService>();
builder.Services.AddScoped<IBidsRepository, BidsRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IRegisterCompanyRepository, RegisterCompanyRepository>();
builder.Services.AddScoped<IRegisterCompanyService, RegisterCompanyService>();
builder.Services.AddScoped<ITransportRequestRepository, TransportRequestRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITransportRequestService, TransportRequestService>();
builder.Services.AddScoped<ITransportRequestsPageRepository, TransportRequestsPageRepository>();
builder.Services.AddScoped<ITransportRequestsPageService, TransportRequestsPageService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRegisterDriverRepository, RegisterDriverRepository>();
builder.Services.AddTransient<IAutomaticSelectionAlgorithmRepository, AutomaticSelectionAlgorithmRepository>();
builder.Services.AddScoped<IAcceptAndRejectBidManualService, AcceptAndRejectBidManualService>();
builder.Services.AddScoped<IAcceptAndRejectBidManualRepository, AcceptAndRejectBidManualRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IRegisterDriverService, RegisterDriverService>();
builder.Services.AddScoped<IAutomaticSelectionAlgorithmRepository, AutomaticSelectionAlgorithmRepository>();
builder.Services.AddScoped<IAutomaticSelectionAlgorithmService, AutomaticSelectionAlgorithmService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>();
builder.Services.AddScoped<IHistoryService, HistoryService>();
builder.Services.AddScoped<IHistoryRepository, HistoryRepository>();
builder.Services.AddScoped<IReviewRequestService, ReviewRequestService>();
builder.Services.AddScoped<IReviewRequestRepository, ReviewRequestRepository>();
builder.Services.AddScoped<ITransportUpdateStatusService, TransportUpdateStatusService>();
builder.Services.AddScoped<ITransportUpdateStatus, TransportUpdateStatusRepository>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IAuthorizationRepository, AuthorizationRepository>();

builder.Services.AddSingleton<ICloudflareR2Service, CloudflareR2Service>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var accessKey = config["CloudflareR2:AccessKeyId"];
    var secretKey = config["CloudflareR2:SecretAccessKey"];
    var accountId = config["CloudflareR2:AccountId"];
    var bucketName = config["CloudflareR2:BucketName"];

    return new CloudflareR2Service(accessKey, secretKey, accountId, bucketName);
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });


var app = builder.Build();

// The demo deploys against an empty database, so bring the schema up on boot.
// Set RUN_MIGRATIONS=false to skip.
if (!string.Equals(Environment.GetEnvironmentVariable("RUN_MIGRATIONS"), "false", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BidGoDbContext>();
    db.Database.Migrate();
}

var fwd = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
};
fwd.KnownNetworks.Clear();
fwd.KnownProxies.Clear();
app.UseForwardedHeaders(fwd);

// No HTTPS redirection: the platform terminates TLS at the edge and forwards plain HTTP to the
// container, so redirecting here only produces loops and breaks the SignalR websocket handshake.


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
            var result = JsonSerializer.Serialize(new { message = "Access denied. You do not have permission to access this resource." });
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

// Authentication and authorization
app.UseRouting();
app.UseCors(MyCors);
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapHub<NotificationHub>("/notificationHub");
app.MapControllers();

app.Run();

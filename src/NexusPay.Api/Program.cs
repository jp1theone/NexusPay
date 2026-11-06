using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NexusPay.Api.Controllers;
using NexusPay.Api.Middleware;
using NexusPay.Api.Security;
using NexusPay.Application.Auth;
using NexusPay.Application.Customers;
using NexusPay.Application.Operations;
using NexusPay.Application.Providers;
using NexusPay.Infrastructure;
using NexusPay.Infrastructure.Identity;
using NexusPay.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger.MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "{Timestamp:O} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header, Description = "Paste the access token issued by /api/auth/login."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddCors(options => options.AddPolicy("FrontendDevelopment", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(options => options.AddPolicy("auth", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 5),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        })));

builder.Services.AddNexusPayInfrastructure(builder.Configuration);
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IValidator<CreateCustomerRequest>, CreateCustomerValidator>();
builder.Services.AddScoped<IValidator<UpdateCustomerRequest>, UpdateCustomerValidator>();
builder.Services.AddScoped<IValidator<CreateFinancialOperationRequest>, CreateFinancialOperationValidator>();
builder.Services.AddScoped<IValidator<ProcessFinancialOperationRequest>, ProcessFinancialOperationValidator>();
builder.Services.AddScoped<CreateCustomerUseCase>();
builder.Services.AddScoped<SearchCustomersUseCase>();
builder.Services.AddScoped<GetCustomerUseCase>();
builder.Services.AddScoped<UpdateCustomerUseCase>();
builder.Services.AddScoped<ActivateCustomerUseCase>();
builder.Services.AddScoped<DeactivateCustomerUseCase>();
builder.Services.AddScoped<CreateFinancialOperationUseCase>();
builder.Services.AddScoped<ProcessFinancialOperationUseCase>();
builder.Services.AddScoped<SearchFinancialOperationsUseCase>();
builder.Services.AddScoped<GetFinancialOperationUseCase>();
builder.Services.AddScoped<CancelFinancialOperationUseCase>();
builder.Services.AddScoped<ProviderManagementUseCases>();
builder.Services.AddIdentityCore<NexusPayUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<NexusPayDbContext>().AddSignInManager();

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
    throw new InvalidOperationException("Jwt:SigningKey is missing. Configure it with .NET User Secrets.");
byte[] jwtKeyBytes;
try { jwtKeyBytes = Convert.FromBase64String(jwtSigningKey); }
catch (FormatException exception) { throw new InvalidOperationException("Jwt:SigningKey must be base64 encoded.", exception); }
if (jwtKeyBytes.Length < 32) throw new InvalidOperationException("Jwt:SigningKey must contain at least 256 bits.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "NexusPay.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "NexusPay.Client";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwtIssuer,
        ValidateAudience = true, ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = ClaimTypes.Name, RoleClaimType = ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var manager = context.HttpContext.RequestServices.GetRequiredService<UserManager<NexusPayUser>>();
            var user = userId is null ? null : await manager.FindByIdAsync(userId);
            if (user is null || !user.IsActive) context.Fail("User account is inactive or no longer exists.");
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();

var app = builder.Build();
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<NexusPayDbContext>().Database.MigrateAsync();
}
await IdentityBootstrap.SeedAsync(app.Services, app.Configuration, app.Environment);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCors("FrontendDevelopment");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }

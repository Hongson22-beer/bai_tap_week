using System.Text;
using BtlThueXe.Api.Middlewares;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Core.Services;
using BtlThueXe.Infrastructure.Data;
using BtlThueXe.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

AppContext.SetSwitch(
    "Npgsql.EnableLegacyTimestampBehavior",
    true);

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. KẾT NỐI POSTGRESQL
// =========================================================

var connectionString =
    builder.Configuration
        .GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseNpgsql(connectionString));

// =========================================================
// 2. DEPENDENCY INJECTION
// =========================================================

// Person 1 - Auth / Customer / Vehicle / Category
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Long - Rental / Contract
builder.Services.AddScoped<IRentalService, RentalService>();
builder.Services.AddScoped<IContractService, ContractService>();

// Hưng - Payment / Operations / Evaluation / Audit
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IHandOverService, HandOverService>();
builder.Services.AddScoped<IReturnService, ReturnService>();
builder.Services.AddScoped<IExtensionService, ExtensionService>();
builder.Services.AddScoped<ICancellationService, CancellationService>();
builder.Services.AddScoped<IEvaluationService, EvaluationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// =========================================================
// 3. JWT AUTHENTICATION
// =========================================================

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? "DefaultSuperSecretKeyForDevelopmentOnly2026";

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? "BtlThueXeApi";

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? "BtlThueXeClient";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey))
            };
    });

builder.Services.AddAuthorization();

// =========================================================
// 4. CORS
// =========================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// =========================================================
// 5. CONTROLLERS
// =========================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// =========================================================
// 6. SWAGGER + JWT
// =========================================================

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "BtlThueXe.Api",
            Version = "v1"
        });

    c.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type =
                Microsoft.OpenApi.Models
                    .SecuritySchemeType.Http,

            Scheme = "bearer",
            BearerFormat = "JWT",

            In =
                Microsoft.OpenApi.Models
                    .ParameterLocation.Header,

            Description = "Nhập JWT token"
        });

    c.AddSecurityRequirement(
        new Microsoft.OpenApi.Models
            .OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models
                    .OpenApiSecurityScheme
                {
                    Reference =
                        new Microsoft.OpenApi.Models
                            .OpenApiReference
                        {
                            Type =
                                Microsoft.OpenApi.Models
                                    .ReferenceType
                                    .SecurityScheme,

                            Id = "Bearer"
                        }
                },

                Array.Empty<string>()
            }
        });
});

// =========================================================
// 7. BUILD APPLICATION
// =========================================================

var app = builder.Build();

// =========================================================
// 8. GLOBAL EXCEPTION HANDLING
// =========================================================

app.UseMiddleware<GlobalExceptionMiddleware>();

// =========================================================
// 9. SWAGGER
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// =========================================================
// 10. HTTPS
// =========================================================

app.UseHttpsRedirection();

// =========================================================
// 11. CORS
// =========================================================

app.UseCors("AllowAll");

// =========================================================
// 12. AUTHENTICATION / AUTHORIZATION
// =========================================================

app.UseAuthentication();
app.UseAuthorization();

// =========================================================
// 13. CONTROLLERS
// =========================================================

app.MapControllers();

// =========================================================
// 14. RUN
// =========================================================

app.Run();
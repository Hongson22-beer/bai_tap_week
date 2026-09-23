using System.Text;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Core.Services;
using BtlThueXe.Infrastructure.Data;
using BtlThueXe.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

AppContext.SetSwitch(
    "Npgsql.EnableLegacyTimestampBehavior",
    true);

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. KẾT NỐI POSTGRESQL
// =========================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection chưa được cấu hình.");

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseNpgsql(connectionString));

// =========================================================
// 2. DEPENDENCY INJECTION
// =========================================================

// Auth / Customer / Vehicle / Category
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Rental / Contract
builder.Services.AddScoped<IRentalService, RentalService>();
builder.Services.AddScoped<IContractService, ContractService>();

// Payment / Handover / Return / Extension / Cancellation
// Evaluation / Audit
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
    ?? throw new InvalidOperationException(
        "Jwt:Key chưa được cấu hình.");

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
        new OpenApiInfo
        {
            Title = "BtlThueXe.Api",
            Version = "v1"
        });

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Nhập JWT token"
        });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
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
// Dùng middleware có sẵn của ASP.NET Core
// =========================================================

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var feature =
            context.Features.Get<IExceptionHandlerFeature>();

        var exception = feature?.Error;

        var statusCode = exception switch
        {
            ArgumentNullException =>
                StatusCodes.Status400BadRequest,

            ArgumentException =>
                StatusCodes.Status400BadRequest,

            KeyNotFoundException =>
                StatusCodes.Status404NotFound,

            UnauthorizedAccessException =>
                StatusCodes.Status403Forbidden,

            InvalidOperationException =>
                StatusCodes.Status409Conflict,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        var error = statusCode switch
        {
            StatusCodes.Status400BadRequest =>
                "Bad Request",

            StatusCodes.Status403Forbidden =>
                "Forbidden",

            StatusCodes.Status404NotFound =>
                "Not Found",

            StatusCodes.Status409Conflict =>
                "Conflict",

            _ =>
                "Internal Server Error"
        };

        var message =
            statusCode == StatusCodes.Status500InternalServerError
                ? "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau."
                : exception?.Message ?? "Đã xảy ra lỗi.";

        context.Response.StatusCode = statusCode;
        context.Response.ContentType =
            "application/json; charset=utf-8";

        await context.Response.WriteAsJsonAsync(
            new
            {
                status = statusCode,
                error,
                message,
                path = context.Request.Path.Value,
                timestamp = DateTime.UtcNow
            });
    });
});

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
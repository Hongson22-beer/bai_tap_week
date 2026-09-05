using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ThueXe.Api.Data;
using ThueXe.Api.Infrastructure;
using ThueXe.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Cấu hình Database
var connectionString = builder.Configuration.GetConnectionString("RentalDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:RentalDb");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Đăng ký Business Services
builder.Services.AddScoped<IXeService, XeService>();
builder.Services.AddScoped<IHangXeService, HangXeService>();
builder.Services.AddScoped<ILoaiXeService, LoaiXeService>();
builder.Services.AddScoped<TokenService>();

// Cấu hình Authentication JwtBearer
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] 
    ?? "KhoaBiMatChoJWTThueXeApiItNhat32KyTu2026";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ThueXeApi",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ThueXeApiClients",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        };
    });

builder.Services.AddAuthorization();

// CORS Allowlist
builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaAllowlist", policy => policy
        .WithOrigins("http://localhost:5173", "http://localhost:3000")
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Authorization", "Content-Type"));
});

// Rate Limiting chống spam login
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 5,
            QueueLimit = 0,
        }));
});

// Swagger hỗ trợ gửi Bearer Token
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Dán access token nhận từ /api/auth/login",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Thứ tự Middleware
app.UseExceptionHandler();

// Security headers
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    await next();
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}
app.UseCors("SpaAllowlist");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();



app.MapControllers();

app.Run();
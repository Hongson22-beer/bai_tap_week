using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using BtlThueXe.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// SERVICES
// =========================================================

// Controller
builder.Services.AddControllers();

// =========================================================
// SWAGGER / OPENAPI
// =========================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =========================================================
// POSTGRESQL + EF CORE
// =========================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"));
});

// =========================================================
// DEPENDENCY INJECTION
// PHẦN BACKEND CỦA HƯNG
// =========================================================
builder.Services.AddScoped<
    IPaymentService,
    PaymentService>();

builder.Services.AddScoped<
    IHandOverService,
    HandOverService>();

builder.Services.AddScoped<
    IReturnService,
    ReturnService>();

builder.Services.AddScoped<
    IExtensionService,
    ExtensionService>();

builder.Services.AddScoped<
    ICancellationService,
    CancellationService>();

builder.Services.AddScoped<
    IEvaluationService,
    EvaluationService>();

builder.Services.AddScoped<
    IAuditLogService,
    AuditLogService>();

var app = builder.Build();

// =========================================================
// HTTP PIPELINE
// =========================================================

// Swagger chỉ bật ở Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Controllers
app.MapControllers();

app.Run();
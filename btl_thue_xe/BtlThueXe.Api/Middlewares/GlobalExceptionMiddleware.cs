using System.Net;
using System.Text.Json;

namespace BtlThueXe.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var (statusCode, error, message) = exception switch
        {
            ArgumentNullException => (
                HttpStatusCode.BadRequest,
                "Bad Request",
                exception.Message
            ),

            ArgumentException => (
                HttpStatusCode.BadRequest,
                "Bad Request",
                exception.Message
            ),

            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "Not Found",
                exception.Message
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                "Forbidden",
                exception.Message
            ),

            InvalidOperationException => (
                HttpStatusCode.Conflict,
                "Conflict",
                exception.Message
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau."
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. Method: {Method}, Path: {Path}",
                context.Request.Method,
                context.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request failed. StatusCode: {StatusCode}, Method: {Method}, Path: {Path}",
                (int)statusCode,
                context.Request.Method,
                context.Request.Path);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType =
            "application/json; charset=utf-8";

        var response = new
        {
            status = (int)statusCode,
            error,
            message,
            path = context.Request.Path.Value,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase
            });

        await context.Response.WriteAsync(json);
    }
}
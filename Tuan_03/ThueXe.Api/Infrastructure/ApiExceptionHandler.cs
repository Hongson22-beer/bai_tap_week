using Microsoft.AspNetCore.Diagnostics;

namespace ThueXe.Api.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            KeyNotFoundException => (404, "Resource not found"),
            InvalidOperationException => (409, "Business rule conflict"),
            _ => (500, "Unexpected server error")
        };

        if (status == 500)
            logger.LogError(exception, "Unhandled exception");

        await Results.Problem(
            statusCode: status,
            title: title,
            detail: exception.Message,
            instance: context.Request.Path)
            .ExecuteAsync(context);

        return true;
    }
}
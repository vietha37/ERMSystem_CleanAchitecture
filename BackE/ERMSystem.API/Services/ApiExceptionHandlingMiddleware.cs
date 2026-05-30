using System.Text.Json;

namespace ERMSystem.API.Services;

public class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, ex);
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                exception,
                "Khong the ghi error envelope vi response da bat dau. CorrelationId={CorrelationId}",
                context.TraceIdentifier);
            throw exception;
        }

        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Request canceled by client for {HttpMethod} {RequestPath}. ErrorCorrelationId={ErrorCorrelationId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.TraceIdentifier);

            context.Response.StatusCode = 499;
            return;
        }

        var (statusCode, errorCode, message, logLevel) = MapException(exception);

        _logger.Log(
            logLevel,
            exception,
            "Unhandled exception for {HttpMethod} {RequestPath}. ErrorCorrelationId={ErrorCorrelationId}",
            context.Request.Method,
            context.Request.Path.Value,
            context.TraceIdentifier);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = ApiErrorResponseFactory.Create(
            context,
            errorCode,
            message,
            context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                ? new { exception = exception.GetType().Name }
                : null);

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }

    private static (int StatusCode, string ErrorCode, string Message, LogLevel LogLevel) MapException(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException ex => (StatusCodes.Status401Unauthorized, "unauthorized", ex.Message, LogLevel.Warning),
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, "not_found", ex.Message, LogLevel.Warning),
            ArgumentException ex => (StatusCodes.Status400BadRequest, "invalid_request", ex.Message, LogLevel.Warning),
            InvalidOperationException ex => (StatusCodes.Status400BadRequest, "invalid_operation", ex.Message, LogLevel.Warning),
            _ => (StatusCodes.Status500InternalServerError, "unexpected_error", "An unexpected error occurred.", LogLevel.Error)
        };
    }
}

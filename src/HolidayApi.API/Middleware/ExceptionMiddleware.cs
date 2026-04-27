using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace HolidayApi.API.Middleware;

/// <summary>
/// Global exception handler middleware.
/// </summary>
public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ArgumentException  => (HttpStatusCode.BadRequest, "Invalid request argument"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            OperationCanceledException => (HttpStatusCode.BadGateway, "Request was cancelled"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        // Log with full stack trace for server errors, brief message for client errors
        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning("Client error {Status}: {Message}", (int)statusCode, exception.Message);

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title  = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}

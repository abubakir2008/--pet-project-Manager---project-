using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagement.Api.Middleware;

/// <summary>
/// Last line of defence: turns any unhandled exception into a ProblemDetails response
/// instead of an empty 500, and keeps the stack trace out of the client response.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected; nothing can be written to the response any more.
            _logger.LogInformation("Request {Path} was cancelled by the client.", context.Request.Path);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict",
                "The item was modified by someone else. Reload the page and try again."),
            DbUpdateException => (StatusCodes.Status409Conflict, "Conflict",
                "The operation conflicts with related data and was not applied."),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Invalid request",
                "The request could not be read."),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error",
                "An unexpected error occurred. Please try again later.")
        };

        _logger.Log(
            statusCode == StatusCodes.Status500InternalServerError ? LogLevel.Error : LogLevel.Warning,
            exception,
            "Unhandled exception while processing {Method} {Path}.",
            context.Request.Method,
            context.Request.Path);

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("The response had already started; the error could not be reported to the client.");
            return;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["message"] = detail;

        // The exception text is only exposed while developing.
        if (_environment.IsDevelopment())
            problem.Extensions["exception"] = exception.ToString();

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

using System.Net;
using System.Text.Json;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            NotFoundException e        => (HttpStatusCode.NotFound, e.Message),
            UnauthorizedException e    => (HttpStatusCode.Forbidden, e.Message),
            DuplicateTaskException e   => (HttpStatusCode.Conflict, e.Message),
            ValidationException e      => (HttpStatusCode.BadRequest, e.Message),
            DomainException e          => (HttpStatusCode.BadRequest, e.Message),
            _                          => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = JsonSerializer.Serialize(new
        {
            status = (int)statusCode,
            error = message
        });

        return context.Response.WriteAsync(response);
    }
}

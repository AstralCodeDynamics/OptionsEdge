using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace OptionsEdge.API.Infrastructure.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation(
                ex,
                "Request aborted by client. {Method} {Path} TraceId {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            if (!context.Response.HasStarted)
                context.Response.StatusCode = 499;
        }
        catch (BadHttpRequestException ex)
        {
            logger.LogWarning(
                ex,
                "Bad HTTP request. {Method} {Path} TraceId {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            await WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid request.",
                ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(
                ex,
                "Argument validation failed. {Method} {Path} TraceId {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            await WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid request.",
                ex.Message);
        }
        catch (Exception ex)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

            logger.LogError(
                ex,
                "Unhandled exception. {Method} {Path} TraceId {TraceId} UserId {UserId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier,
                userId);

            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "The server hit an unexpected error. Refer to the traceId and backend logs for details.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problem, cancellationToken: context.RequestAborted);
    }
}

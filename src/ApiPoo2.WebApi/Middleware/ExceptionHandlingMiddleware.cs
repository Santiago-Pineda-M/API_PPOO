using System.Text.Json;
using ApiPoo2.Application.Exceptions;
using ApiPoo2.Domain.Common;

namespace ApiPoo2.WebApi.Middleware;

public sealed class ExceptionHandlingMiddleware
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
        catch (BaseApplicationException ex)
        {
            await WriteErrorAsync(context, ex.HttpStatusCode, ex.Code, ex.Message, (ex as RequestValidationException)?.Errors);
        }
        catch (DomainValidationException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Code, ex.Message, ex.Errors);
        }
        catch (DomainException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Code, ex.Message, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "internal.error", "Ocurrió un error inesperado.", null);
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        IReadOnlyList<string>? errors)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        var body = new
        {
            status = statusCode,
            code,
            message,
            errors,
            traceId = context.TraceIdentifier,
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(body, WriteErrorJsonOptions);
        context.Response.ContentType = "application/problem+json; charset=utf-8";
        await context.Response.Body.WriteAsync(json, context.RequestAborted);
    }

    private static readonly JsonSerializerOptions WriteErrorJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

using System.Text;
using System.Text.Json;
using ApiPoo2.Application.Exceptions;
using ApiPoo2.Domain.Common;
using ApiPoo2.WebApi.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiPoo2.WebApiTests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    private static readonly JsonSerializerOptions Opciones = new() { PropertyNameCaseInsensitive = true };

    private static async Task<(int StatusCode, JsonElement Body)> Ejecutar(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            (_) => Task.FromException(exception),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var texto = await reader.ReadToEndAsync();

        return (context.Response.StatusCode, JsonDocument.Parse(texto).RootElement.Clone());
    }

    [Fact]
    public async Task NotFoundException_Devuelve404ProblemJson()
    {
        var (status, body) = await Ejecutar(new NotFoundException("vehicle.not_found", "El vehículo no existe."));

        status.Should().Be(404);
        body.GetProperty("status").GetInt32().Should().Be(404);
        body.GetProperty("code").GetString().Should().Be("vehicle.not_found");
        body.GetProperty("message").GetString().Should().Be("El vehículo no existe.");
        body.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ConflictException_Devuelve409ProblemJson()
    {
        var (status, body) = await Ejecutar(new ConflictException("vehiculo.placa.conflict", "La placa ya existe."));

        status.Should().Be(409);
        body.GetProperty("status").GetInt32().Should().Be(409);
        body.GetProperty("code").GetString().Should().Be("vehiculo.placa.conflict");
        body.GetProperty("message").GetString().Should().Be("La placa ya existe.");
    }

    [Fact]
    public async Task RequestValidationException_Devuelve400ConErrores()
    {
        var (status, body) = await Ejecutar(new RequestValidationException(["El campo es obligatorio."]));

        status.Should().Be(400);
        body.GetProperty("code").GetString().Should().Be("validation.failed");
        body.GetProperty("message").GetString().Should().Be("La solicitud contiene datos inválidos.");
        body.GetProperty("errors").GetArrayLength().Should().Be(1);
        body.GetProperty("errors")[0].GetString().Should().Be("El campo es obligatorio.");
    }

    [Fact]
    public async Task DomainValidationException_Devuelve400ConCodigoYErrores()
    {
        var (status, body) = await Ejecutar(new DomainValidationException(
            "vehiculo.placa", ["La placa debe tener seis caracteres."]));

        status.Should().Be(400);
        body.GetProperty("code").GetString().Should().Be("vehiculo.placa");
        body.GetProperty("errors")[0].GetString().Should().Be("La placa debe tener seis caracteres.");
    }

    [Fact]
    public async Task DomainException_Devuelve400SinErrores()
    {
        var (status, body) = await Ejecutar(new DomainException("domain.generic", "Error de dominio."));

        status.Should().Be(400);
        body.GetProperty("code").GetString().Should().Be("domain.generic");
        body.GetProperty("message").GetString().Should().Be("Error de dominio.");
        body.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ExcepcionNoControlada_Devuelve500InternalError()
    {
        var (status, body) = await Ejecutar(new InvalidOperationException("algo se rompió"));

        status.Should().Be(500);
        body.GetProperty("status").GetInt32().Should().Be(500);
        body.GetProperty("code").GetString().Should().Be("internal.error");
        body.GetProperty("message").GetString().Should().Be("Ocurrió un error inesperado.");
    }

    [Fact]
    public async Task ExcepcionDeAplicacion_DevuelveSuStatusCode()
    {
        var (status, body) = await Ejecutar(new ForbiddenException("bootstrap.disabled", "Ya existe un administrador."));

        status.Should().Be(403);
        body.GetProperty("code").GetString().Should().Be("bootstrap.disabled");
    }

    [Fact]
    public async Task Cuerpo_EsApplicationProblemJson()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            (_) => Task.FromException(new NotFoundException("x", "y")),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.ContentType.Should().StartWith("application/problem+json");
    }

    [Fact]
    public async Task Cuerpo_IncluyeElTraceId()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "trace-abc-123";

        var middleware = new ExceptionHandlingMiddleware(
            (_) => Task.FromException(new NotFoundException("x", "y")),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = JsonDocument.Parse(await reader.ReadToEndAsync()).RootElement;

        body.GetProperty("traceId").GetString().Should().Be("trace-abc-123");
    }

    [Fact]
    public async Task RespuestaYaIniciada_NoReescribeElCuerpo()
    {
        var context = new DefaultHttpContext();
        var feature = new RespuestaIniciada();
        context.Features.Set<IHttpResponseFeature>(feature);

        var middleware = new ExceptionHandlingMiddleware(
            (_) => Task.FromException(new NotFoundException("x", "y")),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        feature.StatusCode.Should().Be(200);
        feature.Body.Length.Should().Be(0);
    }

    private sealed class RespuestaIniciada : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = new MemoryStream();

        public bool HasStarted => true;

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }

    [Fact]
    public async Task RespuestaExitosa_PasaSinEscribir()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            (_) => Task.CompletedTask,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
        context.Response.Body.Length.Should().Be(0);
    }
}

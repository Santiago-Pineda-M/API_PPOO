using System.Net;
using ApiPoo2.Application.Exceptions;
using ApiPoo2.Infrastructure.Geoapify;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ApiPoo2.InfrastructureTests.Geoapify;

public sealed class GeoapifyServiceTests
{
    [Fact]
    public async Task GeoapifyService_BuscarDirecciones_DevuelveCoordenadas()
    {
        var handler = new StubHttpMessageHandler(
            """{"features":[{"properties":{"formatted":"Parque de los Novios, Bogotá","name":"Parque de los Novios","city":"Bogotá","country":"Colombia","lat":4.6584,"lon":-74.0935}}]}""");
        var service = CreateService(handler);

        var result = await service.SearchAddressesAsync("Parque de los Novios", 5, CancellationToken.None);

        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                Formatted = "Parque de los Novios, Bogotá",
                Name = "Parque de los Novios",
                City = "Bogotá",
                Country = "Colombia",
                Latitude = 4.6584,
                Longitude = -74.0935,
            });
        handler.RequestUri!.Query.Should().Contain("apiKey=test-api-key");
    }

    [Fact]
    public async Task GeoapifyService_CalcularRuta_ConvierteCoordenadasGeoJson()
    {
        var handler = new StubHttpMessageHandler(
            """{"features":[{"properties":{"distance":1250.5,"time":300},"geometry":{"coordinates":[[-74.0935,4.6584],[-74.0817,4.6097]]}}]}""");
        var service = CreateService(handler);

        var result = await service.CalculateRouteAsync(
            new(4.6584, -74.0935),
            new(4.6097, -74.0817),
            "drive",
            CancellationToken.None);

        result.DistanceMeters.Should().Be(1250.5);
        result.DurationSeconds.Should().Be(300);
        result.Coordinates.Should().BeEquivalentTo(
            new[]
            {
                new ApiPoo2.Application.IServices.GeoapifyCoordinate(4.6584, -74.0935),
                new ApiPoo2.Application.IServices.GeoapifyCoordinate(4.6097, -74.0817),
            },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GeoapifyService_BuscarDirecciones_ErrorProveedor_LanzaExcepcion()
    {
        var handler = new StubHttpMessageHandler("{}", HttpStatusCode.TooManyRequests);
        var service = CreateService(handler);

        var action = () => service.SearchAddressesAsync("Bogotá", 5, CancellationToken.None);

        await action.Should().ThrowAsync<ExternalServiceException>()
            .Where(exception => exception.HttpStatusCode == 502);
    }

    private static GeoapifyService CreateService(StubHttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Geoapify__ApiKey"] = "test-api-key",
            })
            .Build();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.geoapify.com/"),
        };

        return new GeoapifyService(httpClient, configuration);
    }

    private sealed class StubHttpMessageHandler(string response, HttpStatusCode statusCode = HttpStatusCode.OK)
        : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(response),
            });
        }
    }
}

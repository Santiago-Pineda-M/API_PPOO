using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiPoo2.Application.DTOs;
using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.UseCases.Auth.RevokeRefreshToken;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiPoo2.IntegrationTests;

[Collection("API")]
public sealed class AuthApiTests : IAsyncLifetime
{
    private const string StrongPassword = "Password123!";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.EnsureSchemaAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_Login_GetMe_Flow()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, StrongPassword);

        var pair = await LoginAsync(email, StrongPassword);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(pair.AccessTokenType, pair.AccessToken);
        var me = await _client.GetAsync("/api/users/me");

        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var meBody = await ReadAs<UserDto>(me);
        meBody.Email.Should().Be(email);
        meBody.IsActive.Should().BeTrue();
        meBody.Role.Should().Be(0);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Should_Conflict()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, StrongPassword);

        var response = await RegisterAsync(email, StrongPassword);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadAs<ErrorResponse>(response)).Code.Should().Be("email.conflict");
    }

    [Fact]
    public async Task Register_DuplicateEmailAndWeakPassword_Should_PrioritizePasswordPolicy()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, StrongPassword);

        var response = await RegisterAsync(email, "abc");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadAs<ErrorResponse>(response)).Code.Should().Be("password.policy");
    }

    [Fact]
    public async Task Register_WeakPassword_Should_BadRequest()
    {
        var response = await RegisterAsync(UniqueEmail(), "abc");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadAs<ErrorResponse>(response)).Code.Should().Be("password.policy");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Calls_Should_LockAccount()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, StrongPassword);

        for (var i = 0; i < 5; i++)
        {
            var login = await LoginRawAsync(email, "WrongPass!");
            login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await ReadAs<ErrorResponse>(login)).Code.Should().Be("credentials.invalid");
        }

        var locked = await LoginRawAsync(email, StrongPassword);

        locked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadAs<ErrorResponse>(locked)).Code.Should().Be("account.locked");
    }

    [Fact]
    public async Task Refresh_Should_RotateAnd_DetectReuse()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, StrongPassword);
        var pairA = await LoginAsync(email, StrongPassword);

        var rotateResponse = await _client.PostAsync("/api/auth/refresh", Json(new { pairA.RefreshToken }));
        rotateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var pairB = await ReadAs<TokenPairDto>(rotateResponse);
        pairB.RefreshToken.Should().NotBe(pairA.RefreshToken);

        var reuseResponse = await _client.PostAsync("/api/auth/refresh", Json(new { pairA.RefreshToken }));
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadAs<ErrorResponse>(reuseResponse)).Code.Should().Be("refresh.reuse");

        var afterBreachResponse = await _client.PostAsync("/api/auth/refresh", Json(new { pairB.RefreshToken }));
        afterBreachResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadAs<ErrorResponse>(afterBreachResponse)).Code.Should().Be("refresh.invalid");
    }

    [Fact]
    public async Task Logout_Should_RevokeRefreshAnd_BlacklistAccessToken()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, StrongPassword);
        var pair = await LoginAsync(email, StrongPassword);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(pair.AccessTokenType, pair.AccessToken);
        (await _client.GetAsync("/api/users/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        var logout = await _client.PostAsync("/api/auth/logout", Json(new { pair.RefreshToken }));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await _client.GetAsync("/api/users/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var refresh = await _client.PostAsync("/api/auth/refresh", Json(new { pair.RefreshToken }));
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_Should_RequireCurrentSecretAnd_InvalidateOldPassword()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, StrongPassword);
        var pair = await LoginAsync(email, StrongPassword);
        const string newPassword = "Nuevo#Password2";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(pair.AccessTokenType, pair.AccessToken);

        var wrong = await _client.PostAsync("/api/auth/change-password", Json(new { CurrentPassword = "WrongPass!", NewPassword = newPassword }));
        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var ok = await _client.PostAsync("/api/auth/change-password", Json(new { CurrentPassword = StrongPassword, NewPassword = newPassword }));
        ok.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var oldLogin = await LoginRawAsync(email, StrongPassword);
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newLogin = await LoginAsync(email, newPassword);
        newLogin.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ChangePassword_Should_InvalidateActiveSessions()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, StrongPassword);
        var pair = await LoginAsync(email, StrongPassword);
        const string newPassword = "Nuevo#Password2";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(pair.AccessTokenType, pair.AccessToken);

        var ok = await _client.PostAsync("/api/auth/change-password", Json(new { CurrentPassword = StrongPassword, NewPassword = newPassword }));
        ok.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await _client.GetAsync("/api/users/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var oldRefresh = await _client.PostAsync("/api/auth/refresh", Json(new { pair.RefreshToken }));
        oldRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var pairB = await LoginAsync(email, newPassword);
        pairB.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_ConcurrentDoubleSpend_Should_AllowAtMostOneSuccess()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, StrongPassword);
        var pair = await LoginAsync(email, StrongPassword);

        var first = _client.PostAsync("/api/auth/refresh", Json(new { pair.RefreshToken }));
        var second = _client.PostAsync("/api/auth/refresh", Json(new { pair.RefreshToken }));

        var results = await Task.WhenAll(first, second);

        results.Count(r => r.StatusCode == HttpStatusCode.OK).Should().BeLessThanOrEqualTo(1);
        results.Count(r => r.StatusCode == HttpStatusCode.Unauthorized).Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task RevokeToken_ForUnknownUser_Should_NotFound()
    {
        using var scope = _factory.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RevokeRefreshTokenUseCase>();

        var act = async () => await useCase.ExecuteAsync(
            new RevokeRefreshTokenInputDto(Guid.NewGuid(), "some-token"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private Task<HttpResponseMessage> RegisterAsync(string email, string password)
        => _client.PostAsync("/api/auth/register", Json(new { email, password }));

    private async Task<TokenPairDto> LoginAsync(string email, string password)
    {
        var response = await LoginRawAsync(email, password);
        response.EnsureSuccessStatusCode();
        return await ReadAs<TokenPairDto>(response);
    }

    private Task<HttpResponseMessage> LoginRawAsync(string email, string password)
        => _client.PostAsync("/api/auth/login", Json(new { email, password }));

    private static string UniqueEmail()
        => $"test-{Guid.NewGuid():N}@example.com";

    private static StringContent Json(object body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<T> ReadAs<T>(HttpResponseMessage response)
        => JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), JsonOptions)!;

    private sealed record ErrorResponse(int? Status, string? Code, string? Message);
    private sealed record TokenPairDto(string? AccessToken, string? AccessTokenType, string? RefreshToken);
    private sealed record UserDto(string? Email, int? Role, bool IsActive);
}
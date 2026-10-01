using ApiPoo2.Application;
using ApiPoo2.Infrastructure;
using ApiPoo2.Infrastructure.Persistence;
using ApiPoo2.WebApi.Auth;
using ApiPoo2.WebApi.Extensions;
using ApiPoo2.WebApi.Health;
using ApiPoo2.WebApi.Middleware;

EnvFileLoader.LoadFromRepositoryRoot();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// CORS por configuración (Cors__AllowedOrigins, separados por coma). Sin orígenes
// configurados, el navegador bloquea el cross-origin pero Postman/curl siguen funcionando.
var allowedOrigins = builder.Configuration
    .GetSection("Cors")["AllowedOrigins"]
    ?.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// AuthorizationHandler corre como singleton: no puede depender de un repositorio scoped.
// Por eso resuelve la APIKey a través de un IServiceScopeFactory.
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler>(
    sp => new ApiKeyHandler(
        sp.GetRequiredService<IServiceScopeFactory>()));
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ApiKeyRequirement.PolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new ApiKeyRequirement());
    });
});
builder.Services.AddSwaggerWithJwt();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres");

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwaggerWithJwt();

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;

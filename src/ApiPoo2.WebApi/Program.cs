using ApiPoo2.Application;
using ApiPoo2.Infrastructure;
using ApiPoo2.Infrastructure.Persistence;
using ApiPoo2.WebApi.Auth;
using ApiPoo2.WebApi.Extensions;
using ApiPoo2.WebApi.Middleware;

EnvFileLoader.LoadFromRepositoryRoot();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwaggerWithJwt();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;

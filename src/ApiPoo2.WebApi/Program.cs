using ApiPoo2.Application;
using ApiPoo2.Infrastructure;
using ApiPoo2.Infrastructure.Persistencia;
using ApiPoo2.WebApi.Extensions;
using ApiPoo2.WebApi.Middleware;

EnvFileLoader.LoadFromRepositoryRoot();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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
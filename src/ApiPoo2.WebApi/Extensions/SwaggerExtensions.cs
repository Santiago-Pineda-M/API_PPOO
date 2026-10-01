using ApiPoo2.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiPoo2.WebApi.Extensions;

public static class SwaggerExtensions
{
    public const string BearerSchemeId = "Bearer";

    public const string ApiKeySchemeId = "ApiKey";

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "API PPOO — Personas, Vehículos y Documentos",
                Version = "v1",
                Description = "API con JWT (access + refresh revocables) y APIKey como segunda autorización. " +
                    "Los servicios de escritura exigen `Authorization: Bearer` más el encabezado `X-Api-Key`; " +
                    "las consultas públicas no requieren credenciales.",
            });

            options.AddSecurityDefinition(BearerSchemeId, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Ingresá el token JWT de acceso. Ejemplo: Bearer {tu token}",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
            });

            options.AddSecurityDefinition(ApiKeySchemeId, new OpenApiSecurityScheme
            {
                Name = ApiKeyRequirement.HeaderName,
                Description = "APIKey del usuario autenticado. Se obtiene en el login o regenerándola.",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
            });

            options.OperationFilter<SecurityRequirementsOperationFilter>();
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerWithJwt(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "API PPOO v1");
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        });

        return app;
    }

    /// <summary>
    ///     Refleja en Swagger la seguridad real de cada endpoint: los públicos no piden nada,
    ///     los protegidos con JWT piden Bearer y los de la policy ApiKey piden Bearer más APIKey.
    /// </summary>
    private sealed class SecurityRequirementsOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

            if (metadata.OfType<AllowAnonymousAttribute>().Any())
            {
                return;
            }

            var authorize = metadata.OfType<AuthorizeAttribute>().ToList();
            if (authorize.Count == 0)
            {
                return;
            }

            operation.Security.Add(Require(BearerSchemeId));

            if (authorize.Any(a => a.Policy == ApiKeyRequirement.PolicyName))
            {
                operation.Security.Add(Require(ApiKeySchemeId));
            }
        }

        private static OpenApiSecurityRequirement Require(string schemeId) => new()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = schemeId,
                    },
                },
                Array.Empty<string>()
            },
        };
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiPoo2.WebApi.Extensions;

public sealed class MultipartFormDataOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formFileParameterNames = FindFormFileParameterNames(context.MethodInfo).ToList();
        if (formFileParameterNames.Count == 0)
        {
            return;
        }

        var parametersToRemove = operation.Parameters
            .Where(p => formFileParameterNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var p in parametersToRemove)
        {
            operation.Parameters.Remove(p);
        }

        if (operation.RequestBody is null)
        {
            var properties = new Dictionary<string, OpenApiSchema>();
            foreach (var name in formFileParameterNames)
            {
                properties[name] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                };
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = properties,
                            Required = new HashSet<string>(),
                        },
                    },
                },
            };
        }
    }

    private static IEnumerable<string> FindFormFileParameterNames(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            var type = p.ParameterType;
            if (type != typeof(IFormFile) && type != typeof(IFormCollection))
            {
                continue;
            }

            var fromBodyAttribute = p.GetCustomAttributes(typeof(FromBodyAttribute), inherit: true)
                .FirstOrDefault() as FromBodyAttribute;
            if (fromBodyAttribute is not null && p.Name is not null)
            {
                yield return p.Name;
            }
        }
    }
}

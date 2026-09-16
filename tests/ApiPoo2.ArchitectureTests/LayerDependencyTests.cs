using System.Reflection;
using ApiPoo2.Application.CQRS;
using ApiPoo2.Domain.Entities;
using ApiPoo2.Infrastructure.Persistencia;
using FluentAssertions;

namespace ApiPoo2.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private const string DomainName = "ApiPoo2.Domain";
    private const string ApplicationName = "ApiPoo2.Application";
    private const string InfrastructureName = "ApiPoo2.Infrastructure";
    private const string WebApiName = "ApiPoo2.WebApi";

    private static HashSet<string?> ReferencedAssemblies(System.Reflection.Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name).ToHashSet();

    [Fact]
    public void Domain_Should_NotDependOnAnyOtherProjectLayer()
    {
        var references = ReferencedAssemblies(typeof(User).Assembly);

        references.Should().NotContain(ApplicationName);
        references.Should().NotContain(InfrastructureName);
        references.Should().NotContain(WebApiName);
    }

    [Fact]
    public void Application_Should_DependOnlyOnDomain()
    {
        var references = ReferencedAssemblies(typeof(ICommand<>).Assembly);

        references.Should().Contain(DomainName);
        references.Should().NotContain(InfrastructureName);
        references.Should().NotContain(WebApiName);
    }

    [Fact]
    public void Infrastructure_Should_NotDependOnWebApi()
    {
        var references = ReferencedAssemblies(typeof(AppDbContext).Assembly);

        references.Should().Contain(DomainName);
        references.Should().Contain(ApplicationName);
        references.Should().NotContain(WebApiName);
    }

    [Fact]
    public void WebApi_Should_DependOnApplicationAndInfrastructure()
    {
        var webApiAssembly = typeof(ApiPoo2.WebApi.Controllers.UsersController).Assembly;
        var references = ReferencedAssemblies(webApiAssembly);

        references.Should().Contain(DomainName);
        references.Should().Contain(ApplicationName);
        references.Should().Contain(InfrastructureName);
    }

    [Fact]
    public void DomainEntities_Should_NotExposePublicSetters()
    {
        var entityTypes = new[]
        {
            typeof(User),
            typeof(RefreshToken),
            typeof(BlacklistedToken),
        };

        foreach (var type in entityTypes)
        {
            var publicSetterNames = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetSetMethod()?.IsPublic == true)
                .Select(p => p.Name);

            publicSetterNames.Should().BeEmpty($"{type.Name} debe mutar solo a través de métodos de negocio.");
        }
    }
}
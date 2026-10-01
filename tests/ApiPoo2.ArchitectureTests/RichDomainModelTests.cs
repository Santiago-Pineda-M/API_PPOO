using System.Reflection;
using ApiPoo2.Domain.Common;
using FluentAssertions;

namespace ApiPoo2.ArchitectureTests;

public sealed class RichDomainModelTests
{
    /// <summary>
    ///     Entidades con identidad <c>Guid</c>, que heredan de BaseEntity.
    /// </summary>
    private static IEnumerable<Type> DomainEntities() =>
        typeof(BaseEntity).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(BaseEntity).IsAssignableFrom(t));

    /// <summary>
    ///     El enunciado exige primary key compuesta para Usuario ((idpersona, login)) y para la
    ///     relación Vehiculo-Documento ((idvehiculo, iddocumento)), así que ninguna de las dos puede
    ///     heredar de BaseEntity. La excepción queda acá, explícita y testeada, para que nadie agregue
    ///     otra entidad fuera de BaseEntity sin darse cuenta.
    /// </summary>
    private static readonly (string Name, string Namespace)[] CompositeKeyEntities =
    [
        ("User", "ApiPoo2.Domain.Users"),
        ("VehiculoDocumento", "ApiPoo2.Domain.Documentos"),
    ];

    private static IEnumerable<Type> RichEntities() =>
        DomainEntities()
            .Concat(CompositeKeyEntities.Select(e =>
                typeof(BaseEntity).Assembly.GetType($"{e.Namespace}.{e.Name}")!));

    private static bool IsFrameworkDependency(Type type)
    {
        var ns = type.Namespace;

        if (ns is null)
        {
            return false;
        }

        return ns.StartsWith("Microsoft.", StringComparison.Ordinal) ||
               ns.StartsWith("Npgsql", StringComparison.Ordinal) ||
               ns.StartsWith("FluentValidation", StringComparison.Ordinal) ||
               ns.StartsWith("Swashbuckle", StringComparison.Ordinal);
    }

    [Fact]
    public void EntitiesOutsideBaseEntity_Should_BeExplicitlyAllowlisted()
    {
        // Una entidad tiene identidad temporal; un value object no. Ese es el discriminador.
        var entities = typeof(BaseEntity).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace is not null)
            .Where(t => t.Namespace!.StartsWith("ApiPoo2.Domain.", StringComparison.Ordinal))
            .Where(t => !typeof(BaseEntity).IsAssignableFrom(t))
            .Where(t => !t.IsEnum)
            .Where(t => !t.Name.StartsWith("<", StringComparison.Ordinal))
            .Where(t => !t.Name.EndsWith("Configuration", StringComparison.Ordinal))
            .Where(t => t.Name is not ("BaseEntity" or "DomainException" or "DomainValidationException"))
            .Where(t => t.GetProperty("CreatedAtUtc", BindingFlags.Public | BindingFlags.Instance) is not null)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var allowlisted = CompositeKeyEntities.Select(e => e.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();

        entities.Should().BeEquivalentTo(
            allowlisted,
            "toda entidad fuera de BaseEntity debe declararse acá con su justificación; " +
            "User y VehiculoDocumento tienen PK compuesta por requerimiento del enunciado");
    }

    [Fact]
    public void CompositeKeyEntities_Should_NotExposePublicSetters()
    {
        var offenders = new List<string>();

        foreach (var type in RichEntities())
        {
            var setters = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetSetMethod()?.IsPublic == true)
                .Select(p => p.Name);

            offenders.AddRange(setters.Select(s => $"{type.Name}.{s}"));
        }

        offenders.Should().BeEmpty("las entidades deben mutar solo a través de métodos de negocio del dominio");
    }

    [Fact]
    public void CompositeKeyEntities_Should_BeSealedAndBuiltByFactory()
    {
        foreach (var entity in CompositeKeyEntities)
        {
            var type = typeof(BaseEntity).Assembly.GetType($"{entity.Namespace}.{entity.Name}")!;

            type.IsSealed.Should().BeTrue("una entidad no sellada permite forzar la identidad");
            type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Should().BeEmpty(
                "el constructor público permitiría saltarse la factoría validante");
            type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Any(m => m.ReturnType == type)
                .Should().BeTrue("debe existir una factoría estática que valide los invariantes");
        }
    }

    [Fact]
    public void RichEntities_Should_NotExposeMutableCollections()
    {
        var offenders = new List<string>();

        foreach (var type in RichEntities())
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyType = property.PropertyType;

                var isMutable = propertyType.IsArray ||
                                propertyType.IsGenericType &&
                                propertyType.GetGenericArguments().Any(a => a.IsGenericType &&
                                    a.GetGenericArguments().Any(n => n.Name is "List" or "HashSet" or "Dictionary"));

                if (isMutable)
                {
                    offenders.Add($"{type.Name}.{property.Name}: {propertyType.Name}");
                }
            }
        }

        offenders.Should().BeEmpty("una colección mutable pública permite saltarse los invariantes del agregado");
    }

    [Fact]
    public void RichEntities_Should_ExposeBusinessMethods()
    {
        var offenders = RichEntities()
            .Select(type => new
            {
                Type = type,
                Methods = type
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => m.DeclaringType == type)
                    .Where(m => m.Name is not ("ToString" or "Equals" or "GetHashCode" or "GetType"))
                    .Where(m => !m.IsSpecialName)
                    .ToList(),
            })
            .Where(x => x.Methods.Count == 0)
            .Select(x => x.Type.Name)
            .ToList();

        offenders.Should().BeEmpty(
            "una entidad sin métodos de negocio propios es un contenedor de datos anémico; " +
            "la lógica debe vivir en el dominio, no en los casos de uso");
    }

    [Fact]
    public void RichEntities_Should_NotExposeInfrastructureDependencies()
    {
        var offenders = new List<string>();

        foreach (var type in RichEntities())
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                var memberType = member switch
                {
                    PropertyInfo p => p.PropertyType,
                    FieldInfo f => f.FieldType,
                    _ => null,
                };

                if (memberType is not null && IsFrameworkDependency(memberType))
                {
                    offenders.Add($"{type.Name}.{member.Name}: {memberType.FullName}");
                }
            }
        }

        offenders.Should().BeEmpty(
            "el dominio no debe exponer tipos de EF Core, ASP.NET ni de terceros; " +
            "los tipos del BCL (Guid, DateTime, string) sí están permitidos");
    }

    [Fact]
    public void RichEntities_Should_BeSealed()
    {
        var unsealed = RichEntities()
            .Where(t => !t.IsSealed)
            .Select(t => t.Name)
            .ToList();

        unsealed.Should().BeEmpty(
            "una entidad no sellada permite heredar y llamar a Initialize para forzar la identidad, " +
            "eludiendo la factoría que valida los invariantes");
    }

    [Fact]
    public void RichEntities_Should_NotExposePublicConstructors()
    {
        var offenders = new List<string>();

        foreach (var type in RichEntities())
        {
            var publicCtors = type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Select(c => c.GetParameters().Length);

            if (publicCtors.Any())
            {
                offenders.Add(type.Name);
            }
        }

        offenders.Should().BeEmpty(
            "las entidades se construyen solo por factoría estática con validación; " +
            "el constructor privado existe solo para que EF materialice");
    }

    [Fact]
    public void RichEntities_Should_OnlyBeInstantiatedThroughFactories()
    {
        var offenders = new List<string>();

        foreach (var type in RichEntities())
        {
            var hasFactory = type
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Any(m => m.ReturnType == type);

            if (!hasFactory)
            {
                offenders.Add(type.Name);
            }
        }

        offenders.Should().BeEmpty("cada entidad debe tener una factoría estática que valide sus invariantes de creación");
    }
}

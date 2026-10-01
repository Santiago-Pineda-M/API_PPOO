using ApiPoo2.Domain.Personas;
using ApiPoo2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ApiPoo2.IntegrationTests;

/// <summary>
///     Aísla cada ejecución en un schema propio de PostgreSQL usando <c>Search Path</c>, en vez de
///     mutar las tablas públicas compartidas. Como el historial de migraciones también se crea en el
///     primer schema del <c>Search Path</c>, cada ejecución reproduce todas las migraciones desde cero.
///     El schema se descarta al liberar la factory.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _schemaName = $"test_{Guid.NewGuid():N}";

    public ApiFactory()
    {
        // Carga temprana del entorno de pruebas para que Program no pueda sobrescribirlo
        // con el .env de producción al construir el host.
        EnvFileLoader.LoadTestEnvironment();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Error);
        });
        builder.ConfigureServices(services =>
        {
            var connectionString = BuildIsolatedConnectionString();

            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<AppDbContext>(options => options
                .UseNpgsql(connectionString)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connectionString = db.Database.GetConnectionString()!;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var createSchema = connection.CreateCommand();
        createSchema.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{_schemaName}\";";
        await createSchema.ExecuteNonQueryAsync();

        await db.Database.MigrateAsync();

        await SemillarAdministradorAsync(db);
    }

    /// <summary>
    ///     Persona ADMINISTRATIVO inicial. Existe porque el endpoint que crea personas exige token +
    ///     APIKey: sin una semilla no habría forma de obtener el primer usuario. Es una concesión
    ///     exclusiva del entorno de pruebas; en producción el alta es un proceso administrativo.
    /// </summary>
    private static async Task SemillarAdministradorAsync(AppDbContext db)
    {
        if (await db.Personas.AnyAsync(p => p.NumeroIdentificacion == NumeroIdentificacion.From("1")))
        {
            return;
        }

        var persona = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "1",
            "Root",
            "Semilla",
            "root.semilla@example.com",
            TipoPersona.Administrativo,
            DateTime.UtcNow);

        persona.CrearUsuario(SeedPasswordHash, DateTime.UtcNow);

        db.Personas.Add(persona);
        await db.SaveChangesAsync();
    }

    /// <summary>Login de la semilla.</summary>
    public const string SeedLogin = "RS1";

    /// <summary>Contraseña de la semilla.</summary>
    public const string SeedPassword = "Password123!";

    /// <summary>BCrypt work factor 12 de <see cref="SeedPassword" />.</summary>
    private const string SeedPasswordHash = "$2a$12$iIQeJwuFDFBrRwO6Afm08OU7eRA3/Rbp3rYFOK5yeEWofNVvB905G";

    public new async Task DisposeAsync()
    {
        await DropSchemaAsync();
        await base.DisposeAsync();
    }

    private string BuildIsolatedConnectionString()
    {
        EnvFileLoader.LoadTestEnvironment();
        var uri = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (string.IsNullOrEmpty(uri))
        {
            throw new InvalidOperationException(
                "No se encontró DATABASE_URL. Configurala en el archivo .env de la raíz.");
        }

        // Convert postgresql:// URI to Npgsql keyword format
        var builder = new NpgsqlConnectionStringBuilder();
        if (uri.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uriBuilder = new UriBuilder(uri);
            builder.Host = uriBuilder.Host;
            builder.Port = uriBuilder.Port > 0 ? uriBuilder.Port : 5432;
            builder.Database = uriBuilder.Path.TrimStart('/');
            if (!string.IsNullOrEmpty(uriBuilder.UserName))
            {
                builder.Username = Uri.UnescapeDataString(uriBuilder.UserName);
            }
            if (!string.IsNullOrEmpty(uriBuilder.Password))
            {
                builder.Password = Uri.UnescapeDataString(uriBuilder.Password);
            }
        }
        else
        {
            // Assume it's already in keyword format
            var baseBuilder = new NpgsqlConnectionStringBuilder(uri);
            builder.ConnectionString = baseBuilder.ConnectionString;
        }

        builder.SearchPath = _schemaName;

        return builder.ConnectionString;
    }

    private async Task DropSchemaAsync()
    {
        try
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var connectionString = db.Database.GetConnectionString()!;

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP SCHEMA IF EXISTS \"{_schemaName}\" CASCADE;";
            await command.ExecuteNonQueryAsync();
        }
        catch (NpgsqlException)
        {
            // Si la base no está disponible no enmascaramos el resultado real de las pruebas.
        }
    }
}

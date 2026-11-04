using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.BlacklistedTokens;
using ApiPoo2.Domain.Rutas;
using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Trayectos;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.RefreshTokens;
using ApiPoo2.Domain.Users;
using ApiPoo2.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<BlacklistedToken> BlacklistedTokens => Set<BlacklistedToken>();

    public DbSet<Persona> Personas => Set<Persona>();

    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();

    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();

    public DbSet<DocumentoVehiculo> DocumentosVehiculo => Set<DocumentoVehiculo>();

    public DbSet<ConductorVehiculo> ConductoresVehiculos => Set<ConductorVehiculo>();

    public DbSet<Ruta> Rutas => Set<Ruta>();

    public DbSet<Parada> Paradas => Set<Parada>();

    public DbSet<TrayectoTemporal> TrayectosTemporales => Set<TrayectoTemporal>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

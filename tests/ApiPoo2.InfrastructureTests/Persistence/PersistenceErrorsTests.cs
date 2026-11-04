using ApiPoo2.Application.Exceptions;
using ApiPoo2.Infrastructure.Persistence.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ApiPoo2.InfrastructureTests.Persistence;

public sealed class PersistenceErrorsTests
{
    [Fact]
    public void PersistenceErrors_IsUniqueViolation_SoloCoincideCon23505()
    {
        PersistenceErrors.IsUniqueViolation("23505").Should().BeTrue();
        PersistenceErrors.IsUniqueViolation("23507").Should().BeFalse();
        PersistenceErrors.IsUniqueViolation(null).Should().BeFalse();
    }

    [Fact]
    public void PersistenceErrors_MapUniqueViolation_IdentificacionGeneraConflicto()
    {
        var error = PersistenceErrors.MapUniqueViolation("personas_numero_identificacion");

        error.Should().NotBeNull();
        error!.Code.Should().Be("persona.identificacion.conflict");
        error.Should().BeAssignableTo<ConflictException>();
    }

    [Fact]
    public void PersistenceErrors_MapUniqueViolation_CorreoPersonaGeneraConflicto()
    {
        var error = PersistenceErrors.MapUniqueViolation("IX_personas_correo_electronico");

        error.Should().NotBeNull();
        error!.Code.Should().Be("persona.correo.conflict");
        error.Should().BeAssignableTo<ConflictException>();
    }

    [Fact]
    public void PersistenceErrors_MapUniqueViolation_PlacaGeneraConflicto()
    {
        var error = PersistenceErrors.MapUniqueViolation("vehiculos_placa");

        error!.Code.Should().Be("vehiculo.placa.conflict");
    }

    [Fact]
    public void PersistenceErrors_MapUniqueViolation_OtrasConstraintsDevuelvenNulo()
        => PersistenceErrors.MapUniqueViolation("otra_restriccion").Should().BeNull();

    [Fact]
    public void PersistenceErrors_MapUniqueViolation_RecorreExcepcionesInternas()
    {
        var postgres = CrearPostgresUniqueViolation("personas_numero_identificacion");

        var dbUpdate = new DbUpdateException("fallo", new InvalidOperationException("wrap", postgres));

        PersistenceErrors.Map(dbUpdate).Should().BeOfType<ConflictException>()
            .Which.Code.Should().Be("persona.identificacion.conflict");
    }

    [Fact]
    public void PersistenceErrors_MapUniqueViolation_DevuelveOriginalSinCoincidencia()
    {
        var dbUpdate = new DbUpdateException("fallo generico");

        PersistenceErrors.Map(dbUpdate).Should().BeSameAs(dbUpdate);
    }

    /// <summary>
    ///     PostgresException no expone ConstraintName con setter, así que se construye con el
    ///     constructor deNpgsql que sí lo recibe.
    /// </summary>
    private static PostgresException CrearPostgresUniqueViolation(string constraintName)
        => new(
            "duplicate key value violates unique constraint",
            "ERROR",
            "ERROR",
            "23505",
            constraintName: constraintName);
}

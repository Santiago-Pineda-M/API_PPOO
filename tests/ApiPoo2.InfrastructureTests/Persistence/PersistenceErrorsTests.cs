using ApiPoo2.Application.Exceptions;
using ApiPoo2.Infrastructure.Persistence.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ApiPoo2.UnitTests.Persistence;

public sealed class PersistenceErrorsTests
{
    [Fact]
    public void IsUniqueViolation_Should_OnlyMatchPostgres23505()
    {
        PersistenceErrors.IsUniqueViolation("23505").Should().BeTrue();
        PersistenceErrors.IsUniqueViolation("23507").Should().BeFalse();
        PersistenceErrors.IsUniqueViolation(null).Should().BeFalse();
    }

    [Fact]
    public void MapUniqueViolation_Should_MapPersonaIdentificacion_ToConflict()
    {
        var error = PersistenceErrors.MapUniqueViolation("personas_numero_identificacion");

        error.Should().NotBeNull();
        error!.Code.Should().Be("persona.identificacion.conflict");
        error.Should().BeAssignableTo<ConflictException>();
    }

    [Fact]
    public void MapUniqueViolation_Should_MapVehiculoPlaca_ToConflict()
    {
        var error = PersistenceErrors.MapUniqueViolation("vehiculos_placa");

        error!.Code.Should().Be("vehiculo.placa.conflict");
    }

    [Fact]
    public void MapUniqueViolation_Should_ReturnNull_ForOtherConstraints()
        => PersistenceErrors.MapUniqueViolation("otra_restriccion").Should().BeNull();

    [Fact]
    public void Map_Should_WalkInnerExceptions_ToFindConstraint()
    {
        var postgres = CrearPostgresUniqueViolation("personas_numero_identificacion");

        var dbUpdate = new DbUpdateException("fallo", new InvalidOperationException("wrap", postgres));

        PersistenceErrors.Map(dbUpdate).Should().BeOfType<ConflictException>()
            .Which.Code.Should().Be("persona.identificacion.conflict");
    }

    [Fact]
    public void Map_Should_ReturnOriginal_WhenNoConstraintMatches()
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

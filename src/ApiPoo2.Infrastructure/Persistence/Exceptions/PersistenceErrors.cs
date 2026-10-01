using ApiPoo2.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ApiPoo2.Infrastructure.Persistence.Exceptions;

internal static class PersistenceErrors
{
    public static Exception Map(DbUpdateException ex)
    {
        var constraint = FindUniqueViolationConstraint(ex);

        if (constraint is not null && MapUniqueViolation(constraint) is { } mapped)
        {
            return mapped;
        }

        return ex;
    }

    public static ConflictException? MapUniqueViolation(string constraintName)
        => constraintName.Contains("personas_numero_identificacion", StringComparison.Ordinal)
            ? new ConflictException("persona.identificacion.conflict", "Ya existe una persona con ese número de identificación.")
            : constraintName.Contains("vehiculos_placa", StringComparison.Ordinal)
                ? new ConflictException("vehiculo.placa.conflict", "Ya existe un vehículo con esa placa.")
                : null;

    public static bool IsUniqueViolation(string? sqlState)
        => string.Equals(sqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal);

    private static string? FindUniqueViolationConstraint(DbUpdateException ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres
                && IsUniqueViolation(postgres.SqlState)
                && !string.IsNullOrEmpty(postgres.ConstraintName))
            {
                return postgres.ConstraintName;
            }
        }

        return null;
    }
}

using ApiPoo2.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ApiPoo2.Infrastructure.Persistencia.Exceptions;

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
        => constraintName.Contains("users_email", StringComparison.Ordinal)
            ? new ConflictException("email.conflict", "Ya existe una cuenta con ese correo electrónico.")
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
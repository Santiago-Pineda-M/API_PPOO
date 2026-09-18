using ApiPoo2.Application.Exceptions;
using ApiPoo2.Infrastructure.Persistencia.Exceptions;
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
    public void MapUniqueViolation_Should_MapUserEmailConstraint_ToEmailConflict()
    {
        var error = PersistenceErrors.MapUniqueViolation("IX_users_email");

        error.Should().NotBeNull();
        error!.Code.Should().Be("email.conflict");
        error.Should().BeAssignableTo<ConflictException>();
    }

    [Fact]
    public void MapUniqueViolation_Should_ReturnNull_ForOtherConstraints()
    {
        PersistenceErrors.MapUniqueViolation("IX_refresh_tokens_token_hash").Should().BeNull();
    }

    [Fact]
    public void Map_Should_MapEmailUniqueViolation_FromNestedPostgresException()
    {
        var postgres = new PostgresException(
            "duplicate key value violates unique constraint \"IX_users_email\"",
            "ERROR", "ERROR", "23505", "detail", "hint", 0, 0, "query", "where",
            "schema", "users", "email", "text", "IX_users_email", "file", "1", "routine");

        var exception = new DbUpdateException("An error occurred while saving the entity changes.", postgres);

        var mapped = PersistenceErrors.Map(exception);

        mapped.Should().BeOfType<ConflictException>();
        mapped.As<ConflictException>().Code.Should().Be("email.conflict");
    }

    [Fact]
    public void Map_Should_ReturnOriginal_WhenConstraintIsNotMapped()
    {
        var exception = new DbUpdateException("boom");

        var mapped = PersistenceErrors.Map(exception);

        mapped.Should().BeSameAs(exception);
    }
}
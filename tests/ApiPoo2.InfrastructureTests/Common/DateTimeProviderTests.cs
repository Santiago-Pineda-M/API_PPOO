using ApiPoo2.Infrastructure.Common;
using FluentAssertions;

namespace ApiPoo2.InfrastructureTests.Common;

public sealed class DateTimeProviderTests
{
    [Fact]
    public void DateTimeProvider_UtcNow_DevuelveLaHoraUtcActual()
    {
        var provider = new DateTimeProvider();

        var antes = DateTime.UtcNow;
        var ahora = provider.UtcNow;
        var despues = DateTime.UtcNow;

        ahora.Kind.Should().Be(DateTimeKind.Utc);
        ahora.Should().BeOnOrAfter(antes).And.BeOnOrBefore(despues);
    }
}

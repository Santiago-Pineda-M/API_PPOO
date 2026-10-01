using ApiPoo2.Application.IServices;

namespace ApiPoo2.Infrastructure.Common;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

namespace ApiPoo2.Application.IServices;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
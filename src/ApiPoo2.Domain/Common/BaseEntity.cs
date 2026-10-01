namespace ApiPoo2.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    protected void Initialize(Guid id, DateTime createdAtUtc)
    {
        Id = id;
        CreatedAtUtc = createdAtUtc;
    }

    protected void MarkUpdated(DateTime utcNow) => UpdatedAtUtc = utcNow;
}

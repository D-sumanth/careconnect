namespace CareConnect.Domain.Common;

public abstract class SoftDeleteEntity : AuditableEntity
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}

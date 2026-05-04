using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public AuditAction Action { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? IpAddressHash { get; set; }
}

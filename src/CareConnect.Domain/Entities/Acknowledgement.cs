using CareConnect.Domain.Common;

namespace CareConnect.Domain.Entities;

public sealed class Acknowledgement : AuditableEntity
{
    public Guid InformationUpdateId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid LeadUserId { get; set; }
    public string StaffMemberName { get; set; } = string.Empty;
    public string SignatureText { get; set; } = string.Empty;
    public DateTimeOffset AcknowledgedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? IpAddressHash { get; set; }
    public string? UserAgent { get; set; }

    public InformationUpdate? InformationUpdate { get; set; }
    public Department? Department { get; set; }
}

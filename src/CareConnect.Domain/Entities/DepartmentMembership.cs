using CareConnect.Domain.Common;

namespace CareConnect.Domain.Entities;

public sealed class DepartmentMembership : SoftDeleteEntity
{
    public Guid DepartmentId { get; set; }
    public Guid UserId { get; set; }
    public bool IsLead { get; set; }

    public Department? Department { get; set; }
}

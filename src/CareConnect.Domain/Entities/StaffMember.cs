using CareConnect.Domain.Common;

namespace CareConnect.Domain.Entities;

public sealed class StaffMember : SoftDeleteEntity
{
    public Guid DepartmentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? EmployeeReference { get; set; }
    public bool IsActive { get; set; } = true;

    public Department? Department { get; set; }
    public ICollection<Acknowledgement> Acknowledgements { get; } = new List<Acknowledgement>();
}

using CareConnect.Domain.Common;

namespace CareConnect.Domain.Entities;

public sealed class Department : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ExpectedStaffCount { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<DepartmentMembership> Memberships { get; } = new List<DepartmentMembership>();
    public ICollection<StaffMember> StaffMembers { get; } = new List<StaffMember>();
    public ICollection<InformationUpdateDepartment> InformationUpdateDepartments { get; } = new List<InformationUpdateDepartment>();
    public ICollection<Acknowledgement> Acknowledgements { get; } = new List<Acknowledgement>();
}

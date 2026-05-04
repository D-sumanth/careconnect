namespace CareConnect.Domain.Entities;

public sealed class InformationUpdateDepartment
{
    public Guid InformationUpdateId { get; set; }
    public Guid DepartmentId { get; set; }

    public InformationUpdate? InformationUpdate { get; set; }
    public Department? Department { get; set; }
}

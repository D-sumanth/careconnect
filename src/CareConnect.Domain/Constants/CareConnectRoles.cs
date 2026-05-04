namespace CareConnect.Domain.Constants;

public static class CareConnectRoles
{
    public const string Admin = "Admin";
    public const string DepartmentLead = "DepartmentLead";
    public const string StaffViewer = "StaffViewer";

    public static readonly string[] All = [Admin, DepartmentLead, StaffViewer];
}

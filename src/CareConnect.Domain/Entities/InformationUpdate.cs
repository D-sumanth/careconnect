using CareConnect.Domain.Common;
using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

public sealed class InformationUpdate : SoftDeleteEntity
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string AuthorizedBy { get; set; } = string.Empty;
    public InformationUpdateType Type { get; set; } = InformationUpdateType.Routine;
    public InformationUpdateStatus Status { get; set; } = InformationUpdateStatus.Draft;
    public DateTimeOffset? PublishedAt { get; set; }
    public DateOnly? ReviewBy { get; set; }
    public DateOnly? ExpiresOn { get; set; }

    public ICollection<InformationUpdateDepartment> Departments { get; } = new List<InformationUpdateDepartment>();
    public ICollection<Acknowledgement> Acknowledgements { get; } = new List<Acknowledgement>();
}

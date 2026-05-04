namespace CareConnect.Application.Abstractions;

public interface IAdminReportService
{
    Task<string> ExportAcknowledgementsCsvAsync(AcknowledgementExportFilter filter, CancellationToken cancellationToken = default);
}

public sealed record AcknowledgementExportFilter(
    Guid? DepartmentId,
    Guid? InformationUpdateId,
    Guid? LeadUserId,
    DateOnly? From,
    DateOnly? To);

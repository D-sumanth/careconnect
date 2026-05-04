namespace CareConnect.Application.Abstractions;

public interface IAcknowledgementService
{
    Task<AcknowledgementResult> CreateAsync(AcknowledgementRequest request, CancellationToken cancellationToken = default);
}

public sealed record AcknowledgementRequest(
    Guid InformationUpdateId,
    Guid DepartmentId,
    Guid LeadUserId,
    string StaffMemberName,
    string? SignatureText,
    string? IpAddress,
    string? UserAgent);

public sealed record AcknowledgementResult(Guid Id, DateTimeOffset AcknowledgedAt);

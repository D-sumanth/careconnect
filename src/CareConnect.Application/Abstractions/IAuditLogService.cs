using CareConnect.Domain.Enums;

namespace CareConnect.Application.Abstractions;

public interface IAuditLogService
{
    Task RecordAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}

public sealed record AuditLogEntry(
    Guid? UserId,
    string? UserEmail,
    AuditAction Action,
    string EntityName,
    string? EntityId,
    string Description,
    string? IpAddress);

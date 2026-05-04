using CareConnect.Application.Abstractions;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Persistence;
using CareConnect.Infrastructure.Security;

namespace CareConnect.Infrastructure.Services;

public sealed class AuditLogService(AppDbContext dbContext, IDateTimeProvider dateTimeProvider) : IAuditLogService
{
    public async Task RecordAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = entry.UserId,
            UserEmail = entry.UserEmail,
            Action = entry.Action,
            EntityName = entry.EntityName,
            EntityId = entry.EntityId,
            Description = entry.Description,
            IpAddressHash = PrivacyHasher.Hash(entry.IpAddress),
            OccurredAt = dateTimeProvider.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

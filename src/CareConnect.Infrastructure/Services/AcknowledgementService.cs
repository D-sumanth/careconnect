using CareConnect.Application.Abstractions;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using CareConnect.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

public sealed class AcknowledgementService(
    AppDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IAuditLogService auditLogService) : IAcknowledgementService
{
    public async Task<AcknowledgementResult> CreateAsync(
        AcknowledgementRequest request,
        CancellationToken cancellationToken = default)
    {
        var staffName = request.StaffMemberName.Trim();
        if (staffName.Length < 2)
        {
            throw new InvalidOperationException("Staff member name is required.");
        }

        var update = await dbContext.InformationUpdates
            .Include(item => item.Departments)
            .FirstOrDefaultAsync(item =>
                item.Id == request.InformationUpdateId &&
                item.Status == InformationUpdateStatus.Published,
                cancellationToken);

        if (update is null)
        {
            throw new InvalidOperationException("This notice is not available for acknowledgement.");
        }

        var assignedToDepartment = update.Departments.Any(item => item.DepartmentId == request.DepartmentId);
        if (!assignedToDepartment)
        {
            throw new InvalidOperationException("This notice is not assigned to the selected department.");
        }

        var leadCanUseDepartment = await dbContext.DepartmentMemberships.AnyAsync(membership =>
            membership.DepartmentId == request.DepartmentId &&
            membership.UserId == request.LeadUserId &&
            membership.IsLead,
            cancellationToken);

        if (!leadCanUseDepartment)
        {
            throw new InvalidOperationException("The signed-in lead is not assigned to this department.");
        }

        var now = dateTimeProvider.UtcNow;
        var acknowledgement = new Acknowledgement
        {
            InformationUpdateId = request.InformationUpdateId,
            DepartmentId = request.DepartmentId,
            LeadUserId = request.LeadUserId,
            StaffMemberName = staffName,
            SignatureText = string.IsNullOrWhiteSpace(request.SignatureText) ? staffName : request.SignatureText.Trim(),
            AcknowledgedAt = now,
            CreatedAt = now,
            CreatedByUserId = request.LeadUserId,
            IpAddressHash = PrivacyHasher.Hash(request.IpAddress),
            UserAgent = request.UserAgent is { Length: > 500 } ? request.UserAgent[..500] : request.UserAgent
        };

        dbContext.Acknowledgements.Add(acknowledgement);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.RecordAsync(new AuditLogEntry(
            request.LeadUserId,
            null,
            AuditAction.AcknowledgementSubmitted,
            nameof(Acknowledgement),
            acknowledgement.Id.ToString(),
            $"Acknowledgement submitted for '{staffName}'.",
            request.IpAddress), cancellationToken);

        return new AcknowledgementResult(acknowledgement.Id, acknowledgement.AcknowledgedAt);
    }
}

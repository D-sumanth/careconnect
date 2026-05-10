using System.Text;
using CareConnect.Application.Abstractions;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

public sealed class AdminReportService(
    AppDbContext dbContext,
    IAuditLogService auditLogService) : IAdminReportService
{
    public async Task<string> ExportAcknowledgementsCsvAsync(
        AcknowledgementExportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Acknowledgements
            .AsNoTracking()
            .Include(ack => ack.Department)
            .Include(ack => ack.InformationUpdate)
            .Include(ack => ack.StaffMember)
            .AsQueryable();

        if (filter.DepartmentId is { } departmentId)
        {
            query = query.Where(ack => ack.DepartmentId == departmentId);
        }

        if (filter.InformationUpdateId is { } updateId)
        {
            query = query.Where(ack => ack.InformationUpdateId == updateId);
        }

        if (filter.LeadUserId is { } leadUserId)
        {
            query = query.Where(ack => ack.LeadUserId == leadUserId);
        }

        if (filter.From is { } from)
        {
            query = query.Where(ack => ack.AcknowledgedAt >= from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        }

        if (filter.To is { } to)
        {
            query = query.Where(ack => ack.AcknowledgedAt < to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        }

        var rows = await query
            .OrderByDescending(ack => ack.AcknowledgedAt)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine($"Exported at,{Escape(DateTimeOffset.UtcNow.ToString("u"))}");
        csv.AppendLine($"Exported by,{Escape(filter.RequestedByEmail ?? "Unknown")}");
        csv.AppendLine();
        csv.AppendLine("Notice,Department,Directory staff,Staff member,Signature,Lead user id,Acknowledged at,Status,Correction note");
        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(",", [
                Escape(row.InformationUpdate?.Title ?? string.Empty),
                Escape(row.Department?.Name ?? string.Empty),
                Escape(row.StaffMember?.FullName ?? string.Empty),
                Escape(row.StaffMemberName),
                Escape(row.SignatureText),
                Escape(row.LeadUserId.ToString()),
                Escape(row.AcknowledgedAt.ToString("u")),
                Escape(row.IsVoided ? "Voided" : "Active"),
                Escape(row.CorrectionNote ?? string.Empty)
            ]));
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            filter.RequestedByUserId,
            filter.RequestedByEmail,
            AuditAction.CsvExported,
            "Acknowledgement",
            null,
            $"Acknowledgement CSV exported. Notice={filter.InformationUpdateId?.ToString() ?? "all"} Department={filter.DepartmentId?.ToString() ?? "all"} From={filter.From?.ToString() ?? "none"} To={filter.To?.ToString() ?? "none"}.",
            filter.RequestIpAddress), cancellationToken);

        return csv.ToString();
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
        {
            return value;
        }

        return '"' + value.Replace("\"", "\"\"") + '"';
    }
}

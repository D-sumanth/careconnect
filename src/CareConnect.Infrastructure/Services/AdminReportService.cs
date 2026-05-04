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
        csv.AppendLine("Notice,Department,Staff member,Signature,Lead user id,Acknowledged at");
        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(",", [
                Escape(row.InformationUpdate?.Title ?? string.Empty),
                Escape(row.Department?.Name ?? string.Empty),
                Escape(row.StaffMemberName),
                Escape(row.SignatureText),
                Escape(row.LeadUserId.ToString()),
                Escape(row.AcknowledgedAt.ToString("u"))
            ]));
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            null,
            null,
            AuditAction.CsvExported,
            "Acknowledgement",
            null,
            "Acknowledgement CSV exported.",
            null), cancellationToken);

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

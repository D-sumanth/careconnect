using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Web.Pages.Admin.Models;

public sealed record NoticeProgressRow(
    Guid Id,
    string Title,
    string Type,
    string Status,
    string DepartmentNames,
    int ExpectedCount,
    int AcknowledgedCount,
    int OutstandingCount,
    DateTimeOffset? PublishedAt);

public static class NoticeProgressQueries
{
    public static async Task<IReadOnlyList<NoticeProgressRow>> GetRowsAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var notices = await dbContext.InformationUpdates
            .AsNoTracking()
            .Include(notice => notice.Departments)
            .ThenInclude(join => join.Department)
            .ThenInclude(department => department!.StaffMembers)
            .Include(notice => notice.Acknowledgements)
            .OrderByDescending(notice => notice.Status)
            .ThenByDescending(notice => notice.PublishedAt ?? notice.CreatedAt)
            .ToListAsync(cancellationToken);

        return notices
            .Select(notice =>
            {
                var expected = notice.Departments.Sum(join =>
                {
                    var directoryCount = join.Department?.StaffMembers.Count(staff => staff.IsActive) ?? 0;
                    return directoryCount > 0 ? directoryCount : join.Department?.ExpectedStaffCount ?? 0;
                });
                var acknowledged = notice.Acknowledgements.Count(ack => !ack.IsVoided);
                return new NoticeProgressRow(
                    notice.Id,
                    notice.Title,
                    notice.Type.ToString(),
                    notice.Status.ToString(),
                    string.Join(", ", notice.Departments.Select(join => join.Department!.Name)),
                    expected,
                    acknowledged,
                    Math.Max(expected - acknowledged, 0),
                    notice.PublishedAt);
            })
            .ToList();
    }
}

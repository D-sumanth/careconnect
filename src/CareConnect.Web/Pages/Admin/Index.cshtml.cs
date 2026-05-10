using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using CareConnect.Web.Pages.Admin.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Web.Pages.Admin;

public sealed class IndexModel(AppDbContext dbContext) : PageModel
{
    public NoticeSummary Summary { get; private set; } = new(0, 0, 0, 0);
    public IReadOnlyList<NoticeProgressRow> Notices { get; private set; } = [];
    public IReadOnlyList<NoticeProgressRow> NeedsAttention { get; private set; } = [];
    public IReadOnlyList<ActivityRow> RecentActivity { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Notices = await NoticeProgressQueries.GetRowsAsync(dbContext, cancellationToken);

        Summary = new NoticeSummary(
            Notices.Count(notice => notice.Status == InformationUpdateStatus.Published.ToString()),
            Notices.Count(notice => notice.Status == InformationUpdateStatus.Draft.ToString()),
            Notices.Sum(notice => notice.AcknowledgedCount),
            Notices.Sum(notice => notice.OutstandingCount));

        NeedsAttention = Notices
            .Where(notice => notice.Status == InformationUpdateStatus.Published.ToString() && notice.OutstandingCount > 0)
            .OrderByDescending(notice => notice.OutstandingCount)
            .Take(5)
            .ToList();

        RecentActivity = await dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(log => log.OccurredAt)
            .Take(6)
            .Select(log => new ActivityRow(log.Action.ToString(), log.Description, log.OccurredAt))
            .ToListAsync(cancellationToken);
    }

    public sealed record NoticeSummary(int ActiveNotices, int DraftNotices, int Acknowledgements, int Outstanding);
    public sealed record ActivityRow(string Action, string Description, DateTimeOffset OccurredAt);
}

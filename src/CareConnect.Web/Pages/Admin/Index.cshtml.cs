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

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Notices = await NoticeProgressQueries.GetRowsAsync(dbContext, cancellationToken);

        Summary = new NoticeSummary(
            Notices.Count(notice => notice.Status == InformationUpdateStatus.Published.ToString()),
            Notices.Count(notice => notice.Status == InformationUpdateStatus.Draft.ToString()),
            Notices.Sum(notice => notice.AcknowledgedCount),
            Notices.Sum(notice => notice.OutstandingCount));
    }

    public sealed record NoticeSummary(int ActiveNotices, int DraftNotices, int Acknowledgements, int Outstanding);
}

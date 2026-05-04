using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Web.Pages.Admin;

public sealed class IndexModel(AppDbContext dbContext) : PageModel
{
    public AdminSummary Summary { get; private set; } = new(0, 0, 0, 0);
    public IReadOnlyList<RecentAcknowledgement> RecentAcknowledgements { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Summary = new AdminSummary(
            await dbContext.Departments.CountAsync(cancellationToken),
            await dbContext.Users.CountAsync(cancellationToken),
            await dbContext.InformationUpdates.CountAsync(item => item.Status == InformationUpdateStatus.Published, cancellationToken),
            await dbContext.Acknowledgements.CountAsync(cancellationToken));

        RecentAcknowledgements = await dbContext.Acknowledgements
            .AsNoTracking()
            .Include(ack => ack.Department)
            .Include(ack => ack.InformationUpdate)
            .OrderByDescending(ack => ack.AcknowledgedAt)
            .Take(8)
            .Select(ack => new RecentAcknowledgement(
                ack.InformationUpdate!.Title,
                ack.Department!.Name,
                ack.StaffMemberName,
                ack.AcknowledgedAt))
            .ToListAsync(cancellationToken);
    }

    public sealed record AdminSummary(int DepartmentCount, int UserCount, int PublishedNoticeCount, int AcknowledgementCount);
    public sealed record RecentAcknowledgement(string NoticeTitle, string DepartmentName, string StaffMemberName, DateTimeOffset AcknowledgedAt);
}

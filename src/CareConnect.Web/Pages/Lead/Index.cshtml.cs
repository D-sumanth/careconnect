using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Identity;
using CareConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Web.Pages.Lead;

public sealed class IndexModel(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager) : PageModel
{
    public IReadOnlyList<NoticeListItem> Notices { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return;
        }

        var departmentIds = await dbContext.DepartmentMemberships
            .Where(membership => membership.UserId == user.Id && membership.IsLead)
            .Select(membership => membership.DepartmentId)
            .ToListAsync(cancellationToken);

        Notices = await dbContext.InformationUpdates
            .AsNoTracking()
            .Include(update => update.Departments)
            .ThenInclude(join => join.Department)
            .Where(update => update.Status == InformationUpdateStatus.Published &&
                update.Departments.Any(join => departmentIds.Contains(join.DepartmentId)))
            .OrderByDescending(update => update.PublishedAt)
            .Select(update => new NoticeListItem(
                update.Id,
                update.Title,
                update.Summary,
                update.Type.ToString(),
                update.Type == InformationUpdateType.Critical ? "badge-critical" :
                    update.Type == InformationUpdateType.EventBased ? "badge-event" : "badge-routine",
                update.PublishedAt,
                string.Join(", ", update.Departments.Select(join => join.Department!.Name))))
            .ToListAsync(cancellationToken);
    }

    public sealed record NoticeListItem(
        Guid Id,
        string Title,
        string Summary,
        string Type,
        string BadgeClass,
        DateTimeOffset? PublishedAt,
        string DepartmentNames);
}

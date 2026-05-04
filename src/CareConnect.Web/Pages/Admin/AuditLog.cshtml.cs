using CareConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Web.Pages.Admin;

public sealed class AuditLogModel(AppDbContext dbContext) : PageModel
{
    public IReadOnlyList<AuditLogRow> Rows { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Rows = await dbContext.AuditLogs.AsNoTracking()
            .OrderByDescending(log => log.OccurredAt)
            .Take(200)
            .Select(log => new AuditLogRow(log.OccurredAt, log.Action.ToString(), log.UserEmail ?? "System", log.EntityName, log.Description))
            .ToListAsync(cancellationToken);
    }

    public sealed record AuditLogRow(DateTimeOffset OccurredAt, string Action, string UserEmail, string EntityName, string Description);
}

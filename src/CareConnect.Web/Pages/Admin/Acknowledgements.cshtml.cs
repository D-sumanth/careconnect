using CareConnect.Application.Abstractions;
using CareConnect.Infrastructure.Identity;
using CareConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Web.Pages.Admin;

public sealed class AcknowledgementsModel(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAdminReportService adminReportService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid? DepartmentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? InformationUpdateId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? To { get; set; }

    public List<SelectListItem> DepartmentOptions { get; private set; } = [];
    public List<SelectListItem> NoticeOptions { get; private set; } = [];
    public IReadOnlyList<AcknowledgementRow> Rows { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);
        await LoadRowsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostExportAsync(CancellationToken cancellationToken)
    {
        var admin = await userManager.GetUserAsync(User);
        var csv = await adminReportService.ExportAcknowledgementsCsvAsync(new AcknowledgementExportFilter(DepartmentId, InformationUpdateId, null, From, To, admin?.Id, admin?.Email, HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"acknowledgements-{DateTimeOffset.UtcNow:yyyyMMddHHmm}.csv");
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        DepartmentOptions = await dbContext.Departments.AsNoTracking().OrderBy(department => department.Name).Select(department => new SelectListItem(department.Name, department.Id.ToString())).ToListAsync(cancellationToken);
        NoticeOptions = await dbContext.InformationUpdates.AsNoTracking().OrderByDescending(update => update.CreatedAt).Select(update => new SelectListItem(update.Title, update.Id.ToString())).ToListAsync(cancellationToken);
    }

    private async Task LoadRowsAsync(CancellationToken cancellationToken)
    {
        var query = dbContext.Acknowledgements.AsNoTracking().Include(ack => ack.Department).Include(ack => ack.InformationUpdate).AsQueryable();

        if (DepartmentId is { } departmentId)
        {
            query = query.Where(ack => ack.DepartmentId == departmentId);
        }

        if (InformationUpdateId is { } updateId)
        {
            query = query.Where(ack => ack.InformationUpdateId == updateId);
        }

        if (From is { } from)
        {
            query = query.Where(ack => ack.AcknowledgedAt >= from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        }

        if (To is { } to)
        {
            query = query.Where(ack => ack.AcknowledgedAt < to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        }

        Rows = await query.OrderByDescending(ack => ack.AcknowledgedAt)
            .Select(ack => new AcknowledgementRow(
                ack.InformationUpdate!.Title,
                ack.Department!.Name,
                ack.StaffMemberName,
                ack.SignatureText,
                ack.LeadUserId,
                ack.AcknowledgedAt,
                ack.IsVoided))
            .ToListAsync(cancellationToken);
    }

    public sealed record AcknowledgementRow(string NoticeTitle, string DepartmentName, string StaffMemberName, string SignatureText, Guid LeadUserId, DateTimeOffset AcknowledgedAt, bool IsVoided);
}

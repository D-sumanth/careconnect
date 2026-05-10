using System.ComponentModel.DataAnnotations;
using CareConnect.Application.Abstractions;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Identity;
using CareConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Web.Pages.Admin;

public sealed class NoticeModel(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAuditLogService auditLogService,
    IAdminReportService adminReportService) : PageModel
{
    [BindProperty]
    public NoticeInput Input { get; set; } = new();

    public NoticeDetails? Notice { get; private set; }
    public NoticeProgress Progress { get; private set; } = new(0, 0, 0);
    public IReadOnlyList<DepartmentProgressRow> DepartmentProgress { get; private set; } = [];
    public IReadOnlyList<AcknowledgementRow> Acknowledgements { get; private set; } = [];
    public List<SelectListItem> DepartmentOptions { get; private set; } = [];
    public List<SelectListItem> TypeOptions { get; } = Enum.GetValues<InformationUpdateType>().Select(type => new SelectListItem(type.ToString(), type.ToString())).ToList();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        await LoadAsync(id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(id, cancellationToken);
            return Page();
        }

        var notice = await dbContext.InformationUpdates
            .Include(item => item.Departments)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (notice is null)
        {
            return NotFound();
        }

        var admin = await userManager.GetUserAsync(User);
        notice.Title = Input.Title.Trim();
        notice.Summary = Input.Summary.Trim();
        notice.Body = Input.Body.Trim();
        notice.AuthorizedBy = Input.AuthorizedBy.Trim();
        notice.Type = Input.Type;
        notice.UpdatedAt = DateTimeOffset.UtcNow;
        notice.UpdatedByUserId = admin?.Id;

        notice.Departments.Clear();
        foreach (var departmentId in Input.DepartmentIds.Distinct())
        {
            notice.Departments.Add(new InformationUpdateDepartment { InformationUpdateId = notice.Id, DepartmentId = departmentId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.RecordAsync(new AuditLogEntry(admin?.Id, admin?.Email, AuditAction.NoticeUpdated, nameof(InformationUpdate), notice.Id.ToString(), $"Notice '{notice.Title}' updated.", HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id, CancellationToken cancellationToken)
    {
        return await SetStatusAsync(id, InformationUpdateStatus.Published, AuditAction.NoticePublished, cancellationToken);
    }

    public async Task<IActionResult> OnPostArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        return await SetStatusAsync(id, InformationUpdateStatus.Archived, AuditAction.NoticeArchived, cancellationToken);
    }

    public async Task<IActionResult> OnPostExportAsync(Guid id, CancellationToken cancellationToken)
    {
        var csv = await adminReportService.ExportAcknowledgementsCsvAsync(new AcknowledgementExportFilter(null, id, null, null, null), cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"notice-{id}-acknowledgements.csv");
    }

    private async Task<IActionResult> SetStatusAsync(Guid id, InformationUpdateStatus status, AuditAction action, CancellationToken cancellationToken)
    {
        var notice = await dbContext.InformationUpdates.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (notice is null)
        {
            return NotFound();
        }

        var admin = await userManager.GetUserAsync(User);
        notice.Status = status;
        notice.PublishedAt = status == InformationUpdateStatus.Published ? DateTimeOffset.UtcNow : notice.PublishedAt;
        notice.UpdatedAt = DateTimeOffset.UtcNow;
        notice.UpdatedByUserId = admin?.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.RecordAsync(new AuditLogEntry(admin?.Id, admin?.Email, action, nameof(InformationUpdate), notice.Id.ToString(), $"Notice '{notice.Title}' changed to {status}.", HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
        return RedirectToPage(new { id });
    }

    private async Task LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        DepartmentOptions = await dbContext.Departments
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .Select(department => new SelectListItem(department.Name, department.Id.ToString()))
            .ToListAsync(cancellationToken);

        var notice = await dbContext.InformationUpdates
            .AsNoTracking()
            .Include(item => item.Departments).ThenInclude(join => join.Department)
            .Include(item => item.Acknowledgements).ThenInclude(ack => ack.Department)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (notice is null)
        {
            return;
        }

        Notice = new NoticeDetails(notice.Id, notice.Title, notice.Summary, notice.Type.ToString(), notice.Status.ToString());
        Input = new NoticeInput
        {
            Title = notice.Title,
            Summary = notice.Summary,
            Body = notice.Body,
            AuthorizedBy = notice.AuthorizedBy,
            Type = notice.Type,
            DepartmentIds = notice.Departments.Select(join => join.DepartmentId).ToList()
        };

        DepartmentProgress = notice.Departments
            .Select(join =>
            {
                var acknowledged = notice.Acknowledgements.Count(ack => ack.DepartmentId == join.DepartmentId);
                var expected = join.Department?.ExpectedStaffCount ?? 0;
                return new DepartmentProgressRow(join.Department?.Name ?? "Unknown", expected, acknowledged, Math.Max(expected - acknowledged, 0));
            })
            .OrderBy(row => row.DepartmentName)
            .ToList();

        var expectedTotal = DepartmentProgress.Sum(row => row.ExpectedCount);
        var acknowledgedTotal = notice.Acknowledgements.Count;
        Progress = new NoticeProgress(expectedTotal, acknowledgedTotal, Math.Max(expectedTotal - acknowledgedTotal, 0));

        Acknowledgements = notice.Acknowledgements
            .OrderByDescending(ack => ack.AcknowledgedAt)
            .Select(ack => new AcknowledgementRow(ack.StaffMemberName, ack.Department?.Name ?? "Unknown", ack.SignatureText, ack.AcknowledgedAt))
            .ToList();
    }

    public sealed class NoticeInput
    {
        [Required, StringLength(180)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Summary { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        [Display(Name = "Authorized by")]
        [Required, StringLength(160)]
        public string AuthorizedBy { get; set; } = string.Empty;

        [Required]
        public InformationUpdateType Type { get; set; } = InformationUpdateType.Routine;

        [Display(Name = "Departments")]
        [Required]
        public List<Guid> DepartmentIds { get; set; } = [];
    }

    public sealed record NoticeDetails(Guid Id, string Title, string Summary, string Type, string Status);
    public sealed record NoticeProgress(int ExpectedCount, int AcknowledgedCount, int OutstandingCount);
    public sealed record DepartmentProgressRow(string DepartmentName, int ExpectedCount, int AcknowledgedCount, int MissingCount);
    public sealed record AcknowledgementRow(string StaffMemberName, string DepartmentName, string SignatureText, DateTimeOffset AcknowledgedAt);
}

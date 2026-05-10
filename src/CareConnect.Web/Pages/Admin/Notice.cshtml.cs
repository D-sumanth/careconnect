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
    INameNormalizer nameNormalizer,
    IAdminReportService adminReportService) : PageModel
{
    [BindProperty]
    public NoticeInput Input { get; set; } = new();

    public NoticeDetails? Notice { get; private set; }
    public NoticeProgress Progress { get; private set; } = new(0, 0, 0);
    public IReadOnlyList<DepartmentProgressRow> DepartmentProgress { get; private set; } = [];
    public IReadOnlyList<MissingStaffRow> MissingStaff { get; private set; } = [];
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
        notice.ReviewBy = Input.ReviewBy;
        notice.ExpiresOn = Input.ExpiresOn;
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
        var admin = await userManager.GetUserAsync(User);
        var csv = await adminReportService.ExportAcknowledgementsCsvAsync(new AcknowledgementExportFilter(null, id, null, null, null, admin?.Id, admin?.Email, HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"notice-{id}-acknowledgements.csv");
    }

    public async Task<IActionResult> OnPostVoidAcknowledgementAsync(Guid id, Guid acknowledgementId, string? reason, CancellationToken cancellationToken)
    {
        var admin = await userManager.GetUserAsync(User);
        var acknowledgement = await dbContext.Acknowledgements.FirstOrDefaultAsync(ack => ack.Id == acknowledgementId && ack.InformationUpdateId == id, cancellationToken);
        if (acknowledgement is null)
        {
            return NotFound();
        }

        acknowledgement.IsVoided = true;
        acknowledgement.VoidReason = string.IsNullOrWhiteSpace(reason) ? "Voided by admin." : reason.Trim();
        acknowledgement.VoidedAt = DateTimeOffset.UtcNow;
        acknowledgement.VoidedByUserId = admin?.Id;
        acknowledgement.UpdatedAt = DateTimeOffset.UtcNow;
        acknowledgement.UpdatedByUserId = admin?.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.RecordAsync(new AuditLogEntry(admin?.Id, admin?.Email, AuditAction.AcknowledgementVoided, nameof(Acknowledgement), acknowledgement.Id.ToString(), $"Acknowledgement for '{acknowledgement.StaffMemberName}' voided. Reason: {acknowledgement.VoidReason}", HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCorrectAcknowledgementAsync(Guid id, Guid acknowledgementId, string staffMemberName, string? note, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(staffMemberName))
        {
            await LoadAsync(id, cancellationToken);
            ModelState.AddModelError(string.Empty, "Corrected staff member name is required.");
            return Page();
        }

        var admin = await userManager.GetUserAsync(User);
        var acknowledgement = await dbContext.Acknowledgements.FirstOrDefaultAsync(ack => ack.Id == acknowledgementId && ack.InformationUpdateId == id, cancellationToken);
        if (acknowledgement is null)
        {
            return NotFound();
        }

        var originalName = acknowledgement.StaffMemberName;
        acknowledgement.StaffMemberName = staffMemberName.Trim();
        acknowledgement.NormalizedStaffMemberName = nameNormalizer.Normalize(staffMemberName);
        acknowledgement.SignatureText = staffMemberName.Trim();
        acknowledgement.CorrectionNote = string.IsNullOrWhiteSpace(note) ? $"Corrected from '{originalName}'." : note.Trim();
        acknowledgement.UpdatedAt = DateTimeOffset.UtcNow;
        acknowledgement.UpdatedByUserId = admin?.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.RecordAsync(new AuditLogEntry(admin?.Id, admin?.Email, AuditAction.AcknowledgementCorrected, nameof(Acknowledgement), acknowledgement.Id.ToString(), $"Acknowledgement corrected from '{originalName}' to '{acknowledgement.StaffMemberName}'.", HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);

        return RedirectToPage(new { id });
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
            .Include(item => item.Departments).ThenInclude(join => join.Department).ThenInclude(department => department!.StaffMembers)
            .Include(item => item.Acknowledgements).ThenInclude(ack => ack.Department)
            .Include(item => item.Acknowledgements).ThenInclude(ack => ack.StaffMember)
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
            ReviewBy = notice.ReviewBy,
            ExpiresOn = notice.ExpiresOn,
            DepartmentIds = notice.Departments.Select(join => join.DepartmentId).ToList()
        };

        DepartmentProgress = notice.Departments
            .Select(join =>
            {
                var directoryCount = join.Department?.StaffMembers.Count(staff => staff.IsActive) ?? 0;
                var expected = directoryCount > 0 ? directoryCount : join.Department?.ExpectedStaffCount ?? 0;
                var acknowledged = notice.Acknowledgements.Count(ack => ack.DepartmentId == join.DepartmentId && !ack.IsVoided);
                return new DepartmentProgressRow(join.Department?.Name ?? "Unknown", expected, acknowledged, Math.Max(expected - acknowledged, 0));
            })
            .OrderBy(row => row.DepartmentName)
            .ToList();

        var expectedTotal = DepartmentProgress.Sum(row => row.ExpectedCount);
        var acknowledgedTotal = notice.Acknowledgements.Count(ack => !ack.IsVoided);
        Progress = new NoticeProgress(expectedTotal, acknowledgedTotal, Math.Max(expectedTotal - acknowledgedTotal, 0));

        var acknowledgedKeys = notice.Acknowledgements
            .Where(ack => !ack.IsVoided)
            .Select(ack => $"{ack.DepartmentId:N}:{ack.NormalizedStaffMemberName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        MissingStaff = notice.Departments
            .SelectMany(join => join.Department?.StaffMembers
                .Where(staff => staff.IsActive)
                .Where(staff => !acknowledgedKeys.Contains($"{staff.DepartmentId:N}:{staff.NormalizedName}"))
                .Select(staff => new MissingStaffRow(staff.FullName, join.Department?.Name ?? "Unknown")) ?? [])
            .OrderBy(row => row.DepartmentName)
            .ThenBy(row => row.FullName)
            .ToList();

        Acknowledgements = notice.Acknowledgements
            .OrderByDescending(ack => ack.AcknowledgedAt)
            .Select(ack => new AcknowledgementRow(ack.Id, ack.StaffMemberName, ack.Department?.Name ?? "Unknown", ack.SignatureText, ack.AcknowledgedAt, ack.IsVoided, ack.CorrectionNote, ack.VoidReason))
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

        [Display(Name = "Review by")]
        public DateOnly? ReviewBy { get; set; }

        [Display(Name = "Expires on")]
        public DateOnly? ExpiresOn { get; set; }

        [Display(Name = "Departments")]
        [Required]
        public List<Guid> DepartmentIds { get; set; } = [];
    }

    public sealed record NoticeDetails(Guid Id, string Title, string Summary, string Type, string Status);
    public sealed record NoticeProgress(int ExpectedCount, int AcknowledgedCount, int OutstandingCount);
    public sealed record DepartmentProgressRow(string DepartmentName, int ExpectedCount, int AcknowledgedCount, int MissingCount);
    public sealed record MissingStaffRow(string FullName, string DepartmentName);
    public sealed record AcknowledgementRow(Guid Id, string StaffMemberName, string DepartmentName, string SignatureText, DateTimeOffset AcknowledgedAt, bool IsVoided, string? CorrectionNote, string? VoidReason);
}

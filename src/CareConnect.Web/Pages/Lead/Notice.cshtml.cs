using System.ComponentModel.DataAnnotations;
using CareConnect.Application.Abstractions;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Identity;
using CareConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Web.Pages.Lead;

public sealed class NoticeModel(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAcknowledgementService acknowledgementService) : PageModel
{
    [BindProperty]
    public AcknowledgementInput Input { get; set; } = new();

    public NoticeDetails? Notice { get; private set; }
    public List<SelectListItem> DepartmentOptions { get; private set; } = [];
    public string? SuccessMessage { get; private set; }
    public SessionProgress Progress { get; private set; } = new(0, 0, 0);
    public IReadOnlyList<RecentAcknowledgement> RecentAcknowledgements { get; private set; } = [];
    public IReadOnlyList<string> StaffSuggestions { get; private set; } = [];
    public IReadOnlyList<MissingStaffRow> MissingStaff { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        await LoadAsync(id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        await LoadAsync(id, cancellationToken);
        if (Notice is null)
        {
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result = await acknowledgementService.CreateAsync(new AcknowledgementRequest(
                id,
                Input.DepartmentId,
                user.Id,
                Input.StaffMemberName,
                Input.SignatureText,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()), cancellationToken);

            SuccessMessage = $"Acknowledgement recorded at {result.AcknowledgedAt.ToLocalTime():g}.";
            Input = new AcknowledgementInput { DepartmentId = Input.DepartmentId };
            ModelState.Clear();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return Page();
    }

    private async Task LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return;
        }

        var departments = await dbContext.DepartmentMemberships
            .AsNoTracking()
            .Include(membership => membership.Department)
            .Where(membership => membership.UserId == user.Id && membership.IsLead)
            .Select(membership => new { membership.DepartmentId, membership.Department!.Name })
            .ToListAsync(cancellationToken);

        DepartmentOptions = departments
            .Select(department => new SelectListItem(department.Name, department.DepartmentId.ToString()))
            .ToList();

        var departmentIds = departments.Select(department => department.DepartmentId).ToList();
        var update = await dbContext.InformationUpdates
            .AsNoTracking()
            .Include(item => item.Departments)
            .ThenInclude(join => join.Department)
            .ThenInclude(department => department!.StaffMembers)
            .Include(item => item.Acknowledgements)
            .FirstOrDefaultAsync(item =>
                item.Id == id &&
                item.Status == InformationUpdateStatus.Published &&
                item.Departments.Any(join => departmentIds.Contains(join.DepartmentId)),
                cancellationToken);

        if (update is null)
        {
            return;
        }

        Notice = new NoticeDetails(
            update.Id,
            update.Title,
            update.Summary,
            update.Body,
            update.AuthorizedBy,
            update.Type.ToString(),
            update.Type == InformationUpdateType.Critical ? "badge-critical" :
                update.Type == InformationUpdateType.EventBased ? "badge-event" : "badge-routine");

        StaffSuggestions = update.Departments
            .Where(join => departmentIds.Contains(join.DepartmentId))
            .SelectMany(join => join.Department?.StaffMembers.Where(staff => staff.IsActive).Select(staff => staff.FullName) ?? [])
            .Distinct()
            .Order()
            .ToList();

        var expected = update.Departments
            .Where(join => departmentIds.Contains(join.DepartmentId))
            .Sum(join =>
            {
                var directoryCount = join.Department?.StaffMembers.Count(staff => staff.IsActive) ?? 0;
                return directoryCount > 0 ? directoryCount : join.Department?.ExpectedStaffCount ?? 0;
            });
        var acknowledged = update.Acknowledgements.Count(ack => departmentIds.Contains(ack.DepartmentId) && !ack.IsVoided);
        Progress = new SessionProgress(expected, acknowledged, Math.Max(expected - acknowledged, 0));

        var acknowledgedKeys = update.Acknowledgements
            .Where(ack => departmentIds.Contains(ack.DepartmentId) && !ack.IsVoided)
            .Select(ack => $"{ack.DepartmentId:N}:{ack.NormalizedStaffMemberName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        MissingStaff = update.Departments
            .Where(join => departmentIds.Contains(join.DepartmentId))
            .SelectMany(join => join.Department?.StaffMembers
                .Where(staff => staff.IsActive)
                .Where(staff => !acknowledgedKeys.Contains($"{staff.DepartmentId:N}:{staff.NormalizedName}"))
                .Select(staff => new MissingStaffRow(staff.FullName, join.Department?.Name ?? "Unknown")) ?? [])
            .OrderBy(row => row.DepartmentName)
            .ThenBy(row => row.FullName)
            .ToList();

        RecentAcknowledgements = update.Acknowledgements
            .Where(ack => departmentIds.Contains(ack.DepartmentId) && !ack.IsVoided)
            .OrderByDescending(ack => ack.AcknowledgedAt)
            .Take(10)
            .Select(ack => new RecentAcknowledgement(ack.StaffMemberName, ack.AcknowledgedAt))
            .ToList();

        if (Input.DepartmentId == Guid.Empty && DepartmentOptions.Count > 0)
        {
            Input.DepartmentId = Guid.Parse(DepartmentOptions[0].Value);
        }
    }

    public sealed class AcknowledgementInput
    {
        [Display(Name = "Department")]
        [Required]
        public Guid DepartmentId { get; set; }

        [Display(Name = "Team member name")]
        [Required, StringLength(160, MinimumLength = 2)]
        public string StaffMemberName { get; set; } = string.Empty;

        [Display(Name = "Typed signature")]
        [StringLength(160)]
        public string? SignatureText { get; set; }
    }

    public sealed record NoticeDetails(
        Guid Id,
        string Title,
        string Summary,
        string Body,
        string AuthorizedBy,
        string Type,
        string BadgeClass);

    public sealed record SessionProgress(int ExpectedCount, int AcknowledgedCount, int OutstandingCount);
    public sealed record RecentAcknowledgement(string StaffMemberName, DateTimeOffset AcknowledgedAt);
    public sealed record MissingStaffRow(string FullName, string DepartmentName);
}

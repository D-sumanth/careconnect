using System.ComponentModel.DataAnnotations;
using CareConnect.Application.Abstractions;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Identity;
using CareConnect.Infrastructure.Persistence;
using CareConnect.Web.Pages.Admin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Web.Pages.Admin;

public sealed class NoticesModel(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAuditLogService auditLogService) : PageModel
{
    [BindProperty]
    public NoticeInput Input { get; set; } = new();

    public IReadOnlyList<NoticeProgressRow> Notices { get; private set; } = [];
    public List<SelectListItem> DepartmentOptions { get; private set; } = [];
    public List<SelectListItem> TypeOptions { get; } = Enum.GetValues<InformationUpdateType>().Select(type => new SelectListItem(type.ToString(), type.ToString())).ToList();

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        var admin = await userManager.GetUserAsync(User);
        var status = Input.PublishNow ? InformationUpdateStatus.Published : InformationUpdateStatus.Draft;
        var notice = new InformationUpdate
        {
            Title = Input.Title.Trim(),
            Summary = Input.Summary.Trim(),
            Body = Input.Body.Trim(),
            AuthorizedBy = Input.AuthorizedBy.Trim(),
            Type = Input.Type,
            Status = status,
            PublishedAt = status == InformationUpdateStatus.Published ? DateTimeOffset.UtcNow : null,
            CreatedByUserId = admin?.Id
        };

        foreach (var departmentId in Input.DepartmentIds.Distinct())
        {
            notice.Departments.Add(new InformationUpdateDepartment { DepartmentId = departmentId });
        }

        dbContext.InformationUpdates.Add(notice);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.RecordAsync(new AuditLogEntry(admin?.Id, admin?.Email, Input.PublishNow ? AuditAction.NoticePublished : AuditAction.NoticeCreated, nameof(InformationUpdate), notice.Id.ToString(), $"Notice '{notice.Title}' created.", HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        DepartmentOptions = await dbContext.Departments.AsNoTracking().OrderBy(department => department.Name).Select(department => new SelectListItem(department.Name, department.Id.ToString())).ToListAsync(cancellationToken);
        Notices = await NoticeProgressQueries.GetRowsAsync(dbContext, cancellationToken);
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

        [Display(Name = "Publish immediately")]
        public bool PublishNow { get; set; } = true;
    }
}

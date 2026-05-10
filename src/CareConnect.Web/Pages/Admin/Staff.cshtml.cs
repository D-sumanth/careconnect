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

public sealed class StaffModel(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAuditLogService auditLogService,
    INameNormalizer nameNormalizer) : PageModel
{
    [BindProperty]
    public StaffInput Input { get; set; } = new();

    public IReadOnlyList<StaffRow> Staff { get; private set; } = [];
    public List<SelectListItem> DepartmentOptions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        var admin = await userManager.GetUserAsync(User);
        var fullName = Input.FullName.Trim();
        var normalized = nameNormalizer.Normalize(fullName);
        var exists = await dbContext.StaffMembers.AnyAsync(staff =>
            staff.DepartmentId == Input.DepartmentId &&
            staff.NormalizedName == normalized,
            cancellationToken);

        if (exists)
        {
            ModelState.AddModelError(nameof(Input.FullName), "This staff member already exists in the selected department.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        var staffMember = new StaffMember
        {
            DepartmentId = Input.DepartmentId,
            FullName = fullName,
            NormalizedName = normalized,
            EmployeeReference = Input.EmployeeReference?.Trim(),
            CreatedByUserId = admin?.Id
        };

        dbContext.StaffMembers.Add(staffMember);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.RecordAsync(new AuditLogEntry(admin?.Id, admin?.Email, AuditAction.StaffChanged, nameof(StaffMember), staffMember.Id.ToString(), $"Staff member '{staffMember.FullName}' created.", HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var admin = await userManager.GetUserAsync(User);
        var staffMember = await dbContext.StaffMembers.FirstOrDefaultAsync(staff => staff.Id == id, cancellationToken);
        if (staffMember is null)
        {
            return NotFound();
        }

        staffMember.IsActive = false;
        staffMember.IsDeleted = true;
        staffMember.DeletedAt = DateTimeOffset.UtcNow;
        staffMember.DeletedByUserId = admin?.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.RecordAsync(new AuditLogEntry(admin?.Id, admin?.Email, AuditAction.StaffChanged, nameof(StaffMember), staffMember.Id.ToString(), $"Staff member '{staffMember.FullName}' deactivated.", HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);

        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        DepartmentOptions = await dbContext.Departments
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .Select(department => new SelectListItem(department.Name, department.Id.ToString()))
            .ToListAsync(cancellationToken);

        Staff = await dbContext.StaffMembers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(staff => staff.Department)
            .OrderBy(staff => staff.Department!.Name)
            .ThenBy(staff => staff.FullName)
            .Select(staff => new StaffRow(staff.Id, staff.FullName, staff.EmployeeReference, staff.Department!.Name, staff.IsActive && !staff.IsDeleted))
            .ToListAsync(cancellationToken);
    }

    public sealed class StaffInput
    {
        [Display(Name = "Department")]
        [Required]
        public Guid DepartmentId { get; set; }

        [Display(Name = "Full name")]
        [Required, StringLength(160, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Employee reference")]
        [StringLength(80)]
        public string? EmployeeReference { get; set; }
    }

    public sealed record StaffRow(Guid Id, string FullName, string? EmployeeReference, string DepartmentName, bool IsActive);
}

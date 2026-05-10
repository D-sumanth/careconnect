using System.ComponentModel.DataAnnotations;
using CareConnect.Application.Abstractions;
using CareConnect.Domain.Constants;
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

public sealed class UsersModel(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAuditLogService auditLogService) : PageModel
{
    [BindProperty]
    public UserInput Input { get; set; } = new();

    public IReadOnlyList<ApplicationUser> Users { get; private set; } = [];
    public List<SelectListItem> DepartmentOptions { get; private set; } = [];
    public List<SelectListItem> RoleOptions { get; } = CareConnectRoles.All.Select(role => new SelectListItem(role, role)).ToList();

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        var admin = await userManager.GetUserAsync(User);
        var user = new ApplicationUser
        {
            UserName = Input.Email.Trim(),
            Email = Input.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = Input.DisplayName.Trim()
        };

        var result = await userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAsync(cancellationToken);
            return Page();
        }

        await userManager.AddToRoleAsync(user, Input.Role);

        if (Input.DepartmentId is { } departmentId)
        {
            dbContext.DepartmentMemberships.Add(new DepartmentMembership
            {
                DepartmentId = departmentId,
                UserId = user.Id,
                IsLead = Input.IsDepartmentLead,
                CreatedByUserId = admin?.Id
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await auditLogService.RecordAsync(new AuditLogEntry(admin?.Id, admin?.Email, AuditAction.UserChanged, nameof(ApplicationUser), user.Id.ToString(), $"User '{user.Email}' created.", HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var admin = await userManager.GetUserAsync(User);
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = false;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        await userManager.UpdateAsync(user);
        await auditLogService.RecordAsync(new AuditLogEntry(admin?.Id, admin?.Email, AuditAction.UserChanged, nameof(ApplicationUser), user.Id.ToString(), $"User '{user.Email}' deactivated.", HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var admin = await userManager.GetUserAsync(User);
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = true;
        user.LockoutEnd = null;
        await userManager.UpdateAsync(user);
        await auditLogService.RecordAsync(new AuditLogEntry(admin?.Id, admin?.Email, AuditAction.UserChanged, nameof(ApplicationUser), user.Id.ToString(), $"User '{user.Email}' reactivated.", HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(Guid id, string temporaryPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(temporaryPassword) || temporaryPassword.Length < 8)
        {
            ModelState.AddModelError(string.Empty, "Temporary password must be at least 8 characters.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        var admin = await userManager.GetUserAsync(User);
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, temporaryPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAsync(cancellationToken);
            return Page();
        }

        await auditLogService.RecordAsync(new AuditLogEntry(admin?.Id, admin?.Email, AuditAction.UserChanged, nameof(ApplicationUser), user.Id.ToString(), $"Password reset for user '{user.Email}'.", HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Users = await dbContext.Users.AsNoTracking().OrderBy(user => user.DisplayName).ToListAsync(cancellationToken);
        DepartmentOptions = await dbContext.Departments.AsNoTracking().OrderBy(department => department.Name).Select(department => new SelectListItem(department.Name, department.Id.ToString())).ToListAsync(cancellationToken);
    }

    public sealed class UserInput
    {
        [Required, StringLength(160)]
        public string DisplayName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = CareConnectRoles.DepartmentLead;

        [Display(Name = "Department")]
        public Guid? DepartmentId { get; set; }

        [Display(Name = "Department lead")]
        public bool IsDepartmentLead { get; set; } = true;
    }
}

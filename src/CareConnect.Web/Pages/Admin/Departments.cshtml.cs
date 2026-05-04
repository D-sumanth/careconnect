using System.ComponentModel.DataAnnotations;
using CareConnect.Application.Abstractions;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Identity;
using CareConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Web.Pages.Admin;

public sealed class DepartmentsModel(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAuditLogService auditLogService) : PageModel
{
    [BindProperty]
    public DepartmentInput Input { get; set; } = new();

    public IReadOnlyList<Department> Departments { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        var user = await userManager.GetUserAsync(User);
        var department = new Department
        {
            Name = Input.Name.Trim(),
            Description = Input.Description?.Trim(),
            CreatedByUserId = user?.Id
        };

        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogService.RecordAsync(new AuditLogEntry(user?.Id, user?.Email, AuditAction.DepartmentChanged, nameof(Department), department.Id.ToString(), $"Department '{department.Name}' created.", HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);

        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Departments = await dbContext.Departments.AsNoTracking().OrderBy(department => department.Name).ToListAsync(cancellationToken);
    }

    public sealed class DepartmentInput
    {
        [Required, StringLength(140)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}

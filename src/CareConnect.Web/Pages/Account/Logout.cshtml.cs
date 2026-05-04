using CareConnect.Application.Abstractions;
using CareConnect.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CareConnect.Web.Pages.Account;

[Authorize]
public sealed class LogoutModel(
    SignInManager<CareConnect.Infrastructure.Identity.ApplicationUser> signInManager,
    IAuditLogService auditLogService) : PageModel
{
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await auditLogService.RecordAsync(new AuditLogEntry(
            null,
            User.Identity?.Name,
            AuditAction.Logout,
            "Session",
            null,
            "User signed out.",
            HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
        await signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }
}

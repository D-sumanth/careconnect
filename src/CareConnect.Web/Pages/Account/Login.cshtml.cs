using System.ComponentModel.DataAnnotations;
using CareConnect.Application.Abstractions;
using CareConnect.Domain.Constants;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CareConnect.Web.Pages.Account;

public sealed class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IAuditLogService auditLogService) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null || !user.IsActive)
        {
            await RecordLoginAsync(null, AuditAction.LoginFailed, "Login failed for unknown or inactive account.", cancellationToken);
            ModelState.AddModelError(string.Empty, "Invalid sign-in attempt.");
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            await RecordLoginAsync(user, AuditAction.LoginFailed, "Login failed.", cancellationToken);
            ModelState.AddModelError(string.Empty, "Invalid sign-in attempt.");
            return Page();
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);
        await RecordLoginAsync(user, AuditAction.LoginSucceeded, "Login succeeded.", cancellationToken);

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        if (await userManager.IsInRoleAsync(user, CareConnectRoles.Admin))
        {
            return RedirectToPage("/Admin/Index");
        }

        return RedirectToPage("/Lead/Index");
    }

    private Task RecordLoginAsync(
        ApplicationUser? user,
        AuditAction action,
        string description,
        CancellationToken cancellationToken)
    {
        return auditLogService.RecordAsync(new AuditLogEntry(
            user?.Id,
            user?.Email ?? Input.Email,
            action,
            "ApplicationUser",
            user?.Id.ToString(),
            description,
            HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);
    }

    public sealed class LoginInput
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Keep me signed in on this device")]
        public bool RememberMe { get; set; }
    }
}

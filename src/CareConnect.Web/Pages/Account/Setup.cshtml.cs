using System.ComponentModel.DataAnnotations;
using CareConnect.Application.Abstractions;
using CareConnect.Domain.Constants;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CareConnect.Web.Pages.Account;

public sealed class SetupModel(
    IConfiguration configuration,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    SignInManager<ApplicationUser> signInManager,
    IAuditLogService auditLogService) : PageModel
{
    [BindProperty]
    public SetupInput Input { get; set; } = new();

    public bool CanSetup { get; private set; }
    public string StatusMessage { get; private set; } = string.Empty;

    public async Task OnGetAsync([FromQuery] string? token)
    {
        await LoadStateAsync(token);
        Input.Token = token ?? string.Empty;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadStateAsync(Input.Token);
        if (!CanSetup)
        {
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        foreach (var role in CareConnectRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                if (!roleResult.Succeeded)
                {
                    AddIdentityErrors(roleResult);
                    return Page();
                }
            }
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email.Trim(),
            Email = Input.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = Input.DisplayName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, Input.Password);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return Page();
        }

        var roleAddResult = await userManager.AddToRoleAsync(user, CareConnectRoles.Admin);
        if (!roleAddResult.Succeeded)
        {
            AddIdentityErrors(roleAddResult);
            return Page();
        }

        await auditLogService.RecordAsync(new AuditLogEntry(
            user.Id,
            user.Email,
            AuditAction.UserChanged,
            nameof(ApplicationUser),
            user.Id.ToString(),
            "First production admin account created.",
            HttpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToPage("/Admin/Index");
    }

    private async Task LoadStateAsync(string? token)
    {
        var configuredToken = configuration["FirstAdminSetup:Token"];
        if (string.IsNullOrWhiteSpace(configuredToken))
        {
            CanSetup = false;
            StatusMessage = "First-admin setup is not configured. Add FirstAdminSetup__Token in hosting settings.";
            return;
        }

        if (!string.Equals(token, configuredToken, StringComparison.Ordinal))
        {
            CanSetup = false;
            StatusMessage = "Invalid or missing setup token.";
            return;
        }

        var admins = await userManager.GetUsersInRoleAsync(CareConnectRoles.Admin);
        if (admins.Count > 0)
        {
            CanSetup = false;
            StatusMessage = "An admin account already exists. This setup page is now disabled.";
            return;
        }

        CanSetup = true;
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    public sealed class SetupInput
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Display(Name = "Full name")]
        [Required, StringLength(160)]
        public string DisplayName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Confirm password")]
        [Required, DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

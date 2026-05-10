using CareConnect.Application.Abstractions;
using CareConnect.Domain.Constants;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Identity;
using CareConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

public sealed class SeedDataService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IDateTimeProvider dateTimeProvider) : ISeedDataService
{
    public async Task SeedDevelopmentAsync(CancellationToken cancellationToken = default)
    {
        foreach (var role in CareConnectRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var admin = await EnsureUserAsync(
            "admin@careconnect.local",
            "CareConnect Admin",
            "Admin123!",
            CareConnectRoles.Admin);

        var lead = await EnsureUserAsync(
            "lead@careconnect.local",
            "Head Chef",
            "Lead123!",
            CareConnectRoles.DepartmentLead);

        if (!await dbContext.Departments.AnyAsync(cancellationToken))
        {
            var catering = new Department
            {
                Name = "Catering",
                Description = "Kitchen and catering team",
                ExpectedStaffCount = 8,
                CreatedAt = dateTimeProvider.UtcNow,
                CreatedByUserId = admin.Id
            };
            var housekeeping = new Department
            {
                Name = "Housekeeping",
                Description = "Housekeeping and domestic team",
                ExpectedStaffCount = 6,
                CreatedAt = dateTimeProvider.UtcNow,
                CreatedByUserId = admin.Id
            };

            dbContext.Departments.AddRange(catering, housekeeping);
            dbContext.DepartmentMemberships.Add(new DepartmentMembership
            {
                Department = catering,
                UserId = lead.Id,
                IsLead = true,
                CreatedAt = dateTimeProvider.UtcNow,
                CreatedByUserId = admin.Id
            });

            var update = new InformationUpdate
            {
                Title = "Updated soft diet guidance",
                Summary = "Please review the current guidance for residents requiring soft diet support.",
                Body = "Confirm the resident-specific guidance with the nurse in charge before service. Record any concerns immediately and escalate allergy or choking-risk changes.",
                AuthorizedBy = "Care Home Manager",
                Type = InformationUpdateType.Critical,
                Status = InformationUpdateStatus.Published,
                PublishedAt = dateTimeProvider.UtcNow,
                CreatedAt = dateTimeProvider.UtcNow,
                CreatedByUserId = admin.Id
            };
            update.Departments.Add(new InformationUpdateDepartment { InformationUpdate = update, Department = catering });

            dbContext.InformationUpdates.Add(update);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<ApplicationUser> EnsureUserAsync(
        string email,
        string displayName,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                CreatedAt = dateTimeProvider.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}

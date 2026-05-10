using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using CareConnect.Web.Pages.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Tests;

public sealed class NoticeProgressTests
{
    [Fact]
    public async Task GetRowsAsync_ComputesExpectedAcknowledgedAndOutstandingCounts()
    {
        await using var dbContext = CreateDbContext();
        var department = new Department { Name = "Catering", ExpectedStaffCount = 4 };
        var notice = new InformationUpdate
        {
            Title = "Allergen update",
            Summary = "Read before service.",
            Body = "Use the new checklist.",
            AuthorizedBy = "Manager",
            Status = InformationUpdateStatus.Published,
            Type = InformationUpdateType.Critical,
            PublishedAt = DateTimeOffset.UtcNow
        };
        notice.Departments.Add(new InformationUpdateDepartment { Department = department, InformationUpdate = notice });
        notice.Acknowledgements.Add(new Acknowledgement
        {
            Department = department,
            StaffMemberName = "Alex Green",
            NormalizedStaffMemberName = "ALEX GREEN",
            SignatureText = "Alex Green",
            LeadUserId = Guid.NewGuid()
        });

        dbContext.InformationUpdates.Add(notice);
        await dbContext.SaveChangesAsync();

        var row = Assert.Single(await NoticeProgressQueries.GetRowsAsync(dbContext, CancellationToken.None));

        Assert.Equal(4, row.ExpectedCount);
        Assert.Equal(1, row.AcknowledgedCount);
        Assert.Equal(3, row.OutstandingCount);
    }

    [Fact]
    public async Task GetRowsAsync_UsesStaffDirectoryCountBeforeManualExpectedCount()
    {
        await using var dbContext = CreateDbContext();
        var department = new Department { Name = "Catering", ExpectedStaffCount = 10 };
        department.StaffMembers.Add(new StaffMember { FullName = "Alex Green", NormalizedName = "ALEX GREEN" });
        department.StaffMembers.Add(new StaffMember { FullName = "Priya Shah", NormalizedName = "PRIYA SHAH" });
        var notice = new InformationUpdate
        {
            Title = "Allergen update",
            Summary = "Read before service.",
            Body = "Use the new checklist.",
            AuthorizedBy = "Manager",
            Status = InformationUpdateStatus.Published,
            Type = InformationUpdateType.Critical,
            PublishedAt = DateTimeOffset.UtcNow
        };
        notice.Departments.Add(new InformationUpdateDepartment { Department = department, InformationUpdate = notice });

        dbContext.InformationUpdates.Add(notice);
        await dbContext.SaveChangesAsync();

        var row = Assert.Single(await NoticeProgressQueries.GetRowsAsync(dbContext, CancellationToken.None));

        Assert.Equal(2, row.ExpectedCount);
        Assert.Equal(2, row.OutstandingCount);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}

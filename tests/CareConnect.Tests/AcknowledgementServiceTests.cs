using CareConnect.Application.Abstractions;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using CareConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Tests;

public sealed class AcknowledgementServiceTests
{
    [Fact]
    public async Task CreateAsync_CapturesAcknowledgementDepartmentLeadAndAudit()
    {
        await using var dbContext = CreateDbContext();
        var clock = new FixedDateTimeProvider(new DateTimeOffset(2026, 5, 4, 12, 30, 0, TimeSpan.Zero));
        var audit = new AuditLogService(dbContext, clock);
        var service = new AcknowledgementService(dbContext, clock, audit);
        var seed = await SeedPublishedNoticeAsync(dbContext);

        var result = await service.CreateAsync(new AcknowledgementRequest(
            seed.UpdateId,
            seed.DepartmentId,
            seed.LeadUserId,
            "Sam Taylor",
            "Sam Taylor",
            "127.0.0.1",
            "unit-test"));

        var acknowledgement = await dbContext.Acknowledgements.SingleAsync();
        Assert.Equal(result.Id, acknowledgement.Id);
        Assert.Equal(seed.UpdateId, acknowledgement.InformationUpdateId);
        Assert.Equal(seed.DepartmentId, acknowledgement.DepartmentId);
        Assert.Equal(seed.LeadUserId, acknowledgement.LeadUserId);
        Assert.Equal("Sam Taylor", acknowledgement.StaffMemberName);
        Assert.Equal(clock.UtcNow, acknowledgement.AcknowledgedAt);
        Assert.NotNull(acknowledgement.IpAddressHash);
        Assert.Equal(AuditAction.AcknowledgementSubmitted, (await dbContext.AuditLogs.SingleAsync()).Action);
    }

    [Fact]
    public async Task CreateAsync_AllowsMultipleTeamMembersForSameNotice()
    {
        await using var dbContext = CreateDbContext();
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);
        var service = new AcknowledgementService(dbContext, clock, new AuditLogService(dbContext, clock));
        var seed = await SeedPublishedNoticeAsync(dbContext);

        await service.CreateAsync(new AcknowledgementRequest(seed.UpdateId, seed.DepartmentId, seed.LeadUserId, "Alex Green", null, null, null));
        await service.CreateAsync(new AcknowledgementRequest(seed.UpdateId, seed.DepartmentId, seed.LeadUserId, "Priya Shah", null, null, null));

        Assert.Equal(2, await dbContext.Acknowledgements.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_RejectsUnassignedLead()
    {
        await using var dbContext = CreateDbContext();
        var clock = new FixedDateTimeProvider(DateTimeOffset.UtcNow);
        var service = new AcknowledgementService(dbContext, clock, new AuditLogService(dbContext, clock));
        var seed = await SeedPublishedNoticeAsync(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new AcknowledgementRequest(
            seed.UpdateId,
            seed.DepartmentId,
            Guid.NewGuid(),
            "Alex Green",
            null,
            null,
            null)));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<(Guid UpdateId, Guid DepartmentId, Guid LeadUserId)> SeedPublishedNoticeAsync(AppDbContext dbContext)
    {
        var leadUserId = Guid.NewGuid();
        var department = new Department { Name = "Catering" };
        var update = new InformationUpdate
        {
            Title = "Food safety update",
            Summary = "Read before lunch service.",
            Body = "Use the updated allergen checklist.",
            AuthorizedBy = "Manager",
            Status = InformationUpdateStatus.Published,
            Type = InformationUpdateType.Critical,
            PublishedAt = DateTimeOffset.UtcNow
        };

        dbContext.Departments.Add(department);
        dbContext.DepartmentMemberships.Add(new DepartmentMembership
        {
            Department = department,
            UserId = leadUserId,
            IsLead = true
        });
        update.Departments.Add(new InformationUpdateDepartment { Department = department, InformationUpdate = update });
        dbContext.InformationUpdates.Add(update);
        await dbContext.SaveChangesAsync();

        return (update.Id, department.Id, leadUserId);
    }

    private sealed class FixedDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}

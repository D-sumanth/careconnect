using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DepartmentMembership> DepartmentMemberships => Set<DepartmentMembership>();
    public DbSet<InformationUpdate> InformationUpdates => Set<InformationUpdate>();
    public DbSet<InformationUpdateDepartment> InformationUpdateDepartments => Set<InformationUpdateDepartment>();
    public DbSet<Acknowledgement> Acknowledgements => Set<Acknowledgement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(160);
            entity.Property(user => user.IsActive).HasDefaultValue(true);
        });

        builder.Entity<Department>(entity =>
        {
            entity.Property(department => department.Name).HasMaxLength(140).IsRequired();
            entity.Property(department => department.Description).HasMaxLength(500);
            entity.HasIndex(department => department.Name).IsUnique();
            entity.HasQueryFilter(department => !department.IsDeleted);
        });

        builder.Entity<DepartmentMembership>(entity =>
        {
            entity.HasIndex(membership => new { membership.DepartmentId, membership.UserId }).IsUnique();
            entity.HasQueryFilter(membership => !membership.IsDeleted);
            entity.HasOne(membership => membership.Department)
                .WithMany(department => department.Memberships)
                .HasForeignKey(membership => membership.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InformationUpdate>(entity =>
        {
            entity.Property(update => update.Title).HasMaxLength(180).IsRequired();
            entity.Property(update => update.Summary).HasMaxLength(500);
            entity.Property(update => update.Body).IsRequired();
            entity.Property(update => update.AuthorizedBy).HasMaxLength(160).IsRequired();
            entity.Property(update => update.Type).HasConversion<string>().HasMaxLength(40);
            entity.Property(update => update.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasQueryFilter(update => !update.IsDeleted);
        });

        builder.Entity<InformationUpdateDepartment>(entity =>
        {
            entity.HasKey(join => new { join.InformationUpdateId, join.DepartmentId });
            entity.HasOne(join => join.InformationUpdate)
                .WithMany(update => update.Departments)
                .HasForeignKey(join => join.InformationUpdateId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(join => join.Department)
                .WithMany(department => department.InformationUpdateDepartments)
                .HasForeignKey(join => join.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Acknowledgement>(entity =>
        {
            entity.Property(ack => ack.StaffMemberName).HasMaxLength(160).IsRequired();
            entity.Property(ack => ack.SignatureText).HasMaxLength(160).IsRequired();
            entity.Property(ack => ack.IpAddressHash).HasMaxLength(128);
            entity.Property(ack => ack.UserAgent).HasMaxLength(500);
            entity.HasIndex(ack => new { ack.InformationUpdateId, ack.DepartmentId, ack.StaffMemberName });
            entity.HasOne(ack => ack.InformationUpdate)
                .WithMany(update => update.Acknowledgements)
                .HasForeignKey(ack => ack.InformationUpdateId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(ack => ack.Department)
                .WithMany(department => department.Acknowledgements)
                .HasForeignKey(ack => ack.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.Property(log => log.UserEmail).HasMaxLength(256);
            entity.Property(log => log.Action).HasConversion<string>().HasMaxLength(80);
            entity.Property(log => log.EntityName).HasMaxLength(120).IsRequired();
            entity.Property(log => log.EntityId).HasMaxLength(80);
            entity.Property(log => log.Description).HasMaxLength(1000).IsRequired();
            entity.Property(log => log.IpAddressHash).HasMaxLength(128);
            entity.HasIndex(log => log.OccurredAt);
            entity.HasIndex(log => log.Action);
        });
    }
}

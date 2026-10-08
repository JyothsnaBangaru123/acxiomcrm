using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Infrastructure.Data;

public class AcxiomCrmDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public AcxiomCrmDbContext(DbContextOptions<AcxiomCrmDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            b.Property(u => u.Department).HasMaxLength(100);
        });

        // Customer configuration
        builder.Entity<Customer>(b =>
        {
            b.HasKey(c => c.CustomerId);
            b.Property(c => c.CustomerCode).HasMaxLength(50).IsRequired();
            b.Property(c => c.CustomerName).HasMaxLength(150).IsRequired();
            b.Property(c => c.Email).HasMaxLength(150).IsRequired();
            b.Property(c => c.Phone).HasMaxLength(20).IsRequired();
            b.Property(c => c.CompanyName).HasMaxLength(150);
            b.Property(c => c.Address).HasMaxLength(300);
            b.Property(c => c.City).HasMaxLength(100);
            b.Property(c => c.State).HasMaxLength(100);
            b.Property(c => c.CreatedBy).HasMaxLength(150);

            b.HasIndex(c => c.CustomerCode).IsUnique();
            b.HasIndex(c => c.Email);
            b.HasIndex(c => c.Phone);

            b.HasOne(c => c.Owner)
             .WithMany(u => u.AssignedCustomers)
             .HasForeignKey(c => c.OwnerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Lead configuration
        builder.Entity<Lead>(b =>
        {
            b.HasKey(l => l.LeadId);
            b.Property(l => l.LeadCode).HasMaxLength(50).IsRequired();
            b.Property(l => l.LeadName).HasMaxLength(150).IsRequired();
            b.Property(l => l.Email).HasMaxLength(150).IsRequired();
            b.Property(l => l.Phone).HasMaxLength(20).IsRequired();
            b.Property(l => l.CompanyName).HasMaxLength(150);
            b.Property(l => l.ExpectedValue).HasPrecision(18, 2);
            b.Property(l => l.Notes).HasMaxLength(2000);

            b.HasIndex(l => l.LeadCode).IsUnique();
            b.HasIndex(l => l.Email);
            b.HasIndex(l => l.Status);

            b.HasOne(l => l.AssignedUser)
             .WithMany(u => u.AssignedLeads)
             .HasForeignKey(l => l.AssignedTo)
             .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(l => l.ConvertedCustomer)
             .WithMany(c => c.Leads)
             .HasForeignKey(l => l.ConvertedCustomerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Opportunity configuration
        builder.Entity<Opportunity>(b =>
        {
            b.HasKey(o => o.OpportunityId);
            b.Property(o => o.OpportunityName).HasMaxLength(150).IsRequired();
            b.Property(o => o.Amount).HasPrecision(18, 2);
            b.Property(o => o.Notes).HasMaxLength(2000);

            b.HasIndex(o => o.Stage);
            b.HasIndex(o => o.Status);

            b.HasOne(o => o.Customer)
             .WithMany(c => c.Opportunities)
             .HasForeignKey(o => o.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(o => o.Lead)
             .WithMany(l => l.Opportunities)
             .HasForeignKey(o => o.LeadId)
             .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(o => o.AssignedUser)
             .WithMany(u => u.AssignedOpportunities)
             .HasForeignKey(o => o.AssignedTo)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // FollowUp configuration
        builder.Entity<FollowUp>(b =>
        {
            b.HasKey(f => f.FollowUpId);
            b.Property(f => f.Subject).HasMaxLength(200);
            b.Property(f => f.Remarks).HasMaxLength(2000).IsRequired();

            b.HasIndex(f => f.FollowUpDate);
            b.HasIndex(f => f.Status);

            b.HasOne(f => f.Customer)
             .WithMany(c => c.FollowUps)
             .HasForeignKey(f => f.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(f => f.Lead)
             .WithMany(l => l.FollowUps)
             .HasForeignKey(f => f.LeadId)
             .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(f => f.Opportunity)
             .WithMany(o => o.FollowUps)
             .HasForeignKey(f => f.OpportunityId)
             .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(f => f.AssignedUser)
             .WithMany(u => u.AssignedFollowUps)
             .HasForeignKey(f => f.AssignedTo)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Activity configuration
        builder.Entity<Activity>(b =>
        {
            b.HasKey(a => a.ActivityId);
            b.Property(a => a.Subject).HasMaxLength(200).IsRequired();
            b.Property(a => a.Description).HasMaxLength(2000);

            b.HasIndex(a => a.ActivityDate);
            b.HasIndex(a => a.ActivityType);

            b.HasOne(a => a.Customer)
             .WithMany(c => c.Activities)
             .HasForeignKey(a => a.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.Lead)
             .WithMany(l => l.Activities)
             .HasForeignKey(a => a.LeadId)
             .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(a => a.AssignedUser)
             .WithMany(u => u.AssignedActivities)
             .HasForeignKey(a => a.AssignedTo)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // AuditLog configuration
        builder.Entity<AuditLog>(b =>
        {
            b.HasKey(a => a.AuditLogId);
            b.Property(a => a.Action).HasMaxLength(100).IsRequired();
            b.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
            b.Property(a => a.RecordId).HasMaxLength(100);
            b.Property(a => a.UserName).HasMaxLength(150);
            b.Property(a => a.IpAddress).HasMaxLength(100);

            b.HasIndex(a => a.CreatedDate);
            b.HasIndex(a => a.EntityName);
            b.HasIndex(a => a.Action);
            b.HasIndex(a => a.UserId);
        });
    }
}

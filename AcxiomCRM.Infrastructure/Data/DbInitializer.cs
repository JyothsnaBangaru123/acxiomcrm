using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AcxiomCrmDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        // Ensure database is created or migrated
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        foreach (var roleName in AppRoles.AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName, $"Default {roleName} role"));
            }
        }

        // 2. Seed Users
        var adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@acxiomcrm.com",
                Email = "admin@acxiomcrm.com",
                FullName = "System Administrator",
                Department = "IT / Administration",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@12345!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            }
        }

        var managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.com");
        if (managerUser == null)
        {
            managerUser = new ApplicationUser
            {
                UserName = "manager@acxiomcrm.com",
                Email = "manager@acxiomcrm.com",
                FullName = "Sarah Manager",
                Department = "Sales Management",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            };
            var result = await userManager.CreateAsync(managerUser, "Manager@12345!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, AppRoles.Manager);
            }
        }

        var salesUser1 = await userManager.FindByEmailAsync("sales@acxiomcrm.com");
        if (salesUser1 == null)
        {
            salesUser1 = new ApplicationUser
            {
                UserName = "sales@acxiomcrm.com",
                Email = "sales@acxiomcrm.com",
                FullName = "Alex Executive",
                Department = "Direct Sales",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-5)
            };
            var result = await userManager.CreateAsync(salesUser1, "Sales@12345!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(salesUser1, AppRoles.SalesExecutive);
            }
        }

        var salesUser2 = await userManager.FindByEmailAsync("john.sales@acxiomcrm.com");
        if (salesUser2 == null)
        {
            salesUser2 = new ApplicationUser
            {
                UserName = "john.sales@acxiomcrm.com",
                Email = "john.sales@acxiomcrm.com",
                FullName = "John Representative",
                Department = "Direct Sales",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddMonths(-4)
            };
            var result = await userManager.CreateAsync(salesUser2, "Sales@12345!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(salesUser2, AppRoles.SalesExecutive);
            }
        }

        // 3. Seed Customers if empty
        if (!await context.Customers.AnyAsync())
        {
            var cust1 = new Customer
            {
                CustomerCode = "CUST-0001",
                CustomerName = "Jane Smith",
                Email = "jane.smith@acmetech.com",
                Phone = "+1-555-010123",
                CompanyName = "Acme Technologies Inc",
                Address = "100 Tech Blvd, Suite 400",
                City = "San Jose",
                State = "CA",
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow.AddMonths(-3),
                CreatedBy = "Admin",
                OwnerId = salesUser1.Id
            };

            var cust2 = new Customer
            {
                CustomerCode = "CUST-0002",
                CustomerName = "Robert King",
                Email = "robert.king@apexretail.com",
                Phone = "+1-555-010245",
                CompanyName = "Apex Retail Group",
                Address = "500 Michigan Ave",
                City = "Chicago",
                State = "IL",
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow.AddMonths(-2),
                CreatedBy = "Admin",
                OwnerId = salesUser2.Id
            };

            var cust3 = new Customer
            {
                CustomerCode = "CUST-0003",
                CustomerName = "Lisa Ray",
                Email = "lisa.ray@globallogistics.com",
                Phone = "+1-555-010378",
                CompanyName = "Global Logistics Corp",
                Address = "88 Port Highway",
                City = "Seattle",
                State = "WA",
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow.AddMonths(-1),
                CreatedBy = "Admin",
                OwnerId = salesUser1.Id
            };

            await context.Customers.AddRangeAsync(cust1, cust2, cust3);
            await context.SaveChangesAsync();

            // 4. Seed Leads
            var lead1 = new Lead
            {
                LeadCode = "LEAD-0001",
                LeadName = "David Miller",
                Email = "david.miller@cloudscale.io",
                Phone = "+1-555-020111",
                CompanyName = "CloudScale Systems",
                Source = LeadSource.Website,
                Status = LeadStatus.New,
                Priority = LeadPriority.High,
                ExpectedValue = 45000,
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                AssignedTo = salesUser1.Id,
                Notes = "Inquired through website product demo request."
            };

            var lead2 = new Lead
            {
                LeadCode = "LEAD-0002",
                LeadName = "Elena Gomez",
                Email = "elena.gomez@nextgenbio.com",
                Phone = "+1-555-020222",
                CompanyName = "NextGen BioTech",
                Source = LeadSource.Referral,
                Status = LeadStatus.Contacted,
                Priority = LeadPriority.Urgent,
                ExpectedValue = 85000,
                CreatedDate = DateTime.UtcNow.AddDays(-7),
                AssignedTo = salesUser1.Id,
                Notes = "Discussed CRM cloud migration timeline."
            };

            var lead3 = new Lead
            {
                LeadCode = "LEAD-0003",
                LeadName = "Marcus Vance",
                Email = "marcus.vance@sterlingfin.com",
                Phone = "+1-555-020333",
                CompanyName = "Sterling Financials",
                Source = LeadSource.ColdCall,
                Status = LeadStatus.Qualified,
                Priority = LeadPriority.High,
                ExpectedValue = 120000,
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                AssignedTo = salesUser2.Id,
                Notes = "Ready for conversion and proposal meeting."
            };

            var lead4 = new Lead
            {
                LeadCode = "LEAD-0004",
                LeadName = "Chloe Bennett",
                Email = "chloe.bennett@horizonmedia.com",
                Phone = "+1-555-020444",
                CompanyName = "Horizon Media",
                Source = LeadSource.SocialMedia,
                Status = LeadStatus.Lost,
                Priority = LeadPriority.Low,
                ExpectedValue = 25000,
                CreatedDate = DateTime.UtcNow.AddDays(-20),
                AssignedTo = salesUser2.Id,
                Notes = "Budget deferred to next fiscal year."
            };

            await context.Leads.AddRangeAsync(lead1, lead2, lead3, lead4);
            await context.SaveChangesAsync();

            // 5. Seed Opportunities
            var opp1 = new Opportunity
            {
                OpportunityName = "Enterprise CRM Implementation",
                CustomerId = cust1.CustomerId,
                Amount = 125000,
                Stage = OpportunityStage.Proposal,
                Probability = 60,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                Status = OpportunityStatus.Open,
                CreatedDate = DateTime.UtcNow.AddDays(-15),
                AssignedTo = salesUser1.Id,
                Notes = "Finalizing scope of custom API integrations."
            };

            var opp2 = new Opportunity
            {
                OpportunityName = "POS Analytics Suite Upgrade",
                CustomerId = cust2.CustomerId,
                Amount = 68000,
                Stage = OpportunityStage.Negotiation,
                Probability = 80,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(10),
                Status = OpportunityStatus.Open,
                CreatedDate = DateTime.UtcNow.AddDays(-12),
                AssignedTo = salesUser2.Id,
                Notes = "Price negotiations in final stage."
            };

            var opp3 = new Opportunity
            {
                OpportunityName = "Fleet Route Optimization Software",
                CustomerId = cust3.CustomerId,
                Amount = 95000,
                Stage = OpportunityStage.Won,
                Probability = 100,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(-2),
                Status = OpportunityStatus.Won,
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                AssignedTo = salesUser1.Id,
                Notes = "Contract signed and onboarded."
            };

            var opp4 = new Opportunity
            {
                OpportunityName = "Support Desk Add-on Module",
                CustomerId = cust1.CustomerId,
                Amount = 35000,
                Stage = OpportunityStage.Qualification,
                Probability = 30,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(40),
                Status = OpportunityStatus.Open,
                CreatedDate = DateTime.UtcNow.AddDays(-4),
                AssignedTo = salesUser1.Id,
                Notes = "Customer considering Q4 rollout."
            };

            await context.Opportunities.AddRangeAsync(opp1, opp2, opp3, opp4);
            await context.SaveChangesAsync();

            // 6. Seed Follow-ups
            var fu1 = new FollowUp
            {
                CustomerId = cust1.CustomerId,
                OpportunityId = opp1.OpportunityId,
                FollowUpDate = DateTime.UtcNow.AddDays(2),
                FollowUpType = FollowUpType.Meeting,
                Subject = "Proposal walkthrough with CFO",
                Remarks = "Review payment milestones and SLA agreements.",
                Status = FollowUpStatus.Planned,
                AssignedTo = salesUser1.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            };

            var fu2 = new FollowUp
            {
                LeadId = lead2.LeadId,
                FollowUpDate = DateTime.UtcNow.AddDays(1),
                FollowUpType = FollowUpType.Call,
                Subject = "Technical scoping call",
                Remarks = "Connect with lead architect regarding cloud security.",
                Status = FollowUpStatus.Planned,
                AssignedTo = salesUser1.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            };

            var fu3 = new FollowUp
            {
                CustomerId = cust2.CustomerId,
                OpportunityId = opp2.OpportunityId,
                FollowUpDate = DateTime.UtcNow.AddDays(-3),
                FollowUpType = FollowUpType.Meeting,
                Subject = "Contract review meeting",
                Remarks = "Completed contract discussion with legal counsel.",
                Status = FollowUpStatus.Completed,
                CompletedDate = DateTime.UtcNow.AddDays(-3),
                AssignedTo = salesUser2.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-6)
            };

            await context.FollowUps.AddRangeAsync(fu1, fu2, fu3);
            await context.SaveChangesAsync();

            // 7. Seed Activities
            var act1 = new Activity
            {
                ActivityType = ActivityType.Call,
                Subject = "Initial Introduction Call",
                Description = "Discussed overall company size and CRM needs.",
                ActivityDate = DateTime.UtcNow.AddDays(-10),
                CustomerId = cust1.CustomerId,
                AssignedTo = salesUser1.Id,
                Status = ActivityStatus.Completed,
                CreatedDate = DateTime.UtcNow.AddDays(-10)
            };

            var act2 = new Activity
            {
                ActivityType = ActivityType.Meeting,
                Subject = "Demo Presentation",
                Description = "Presented interactive dashboard and workflow features.",
                ActivityDate = DateTime.UtcNow.AddDays(-7),
                CustomerId = cust1.CustomerId,
                AssignedTo = salesUser1.Id,
                Status = ActivityStatus.Completed,
                CreatedDate = DateTime.UtcNow.AddDays(-7)
            };

            var act3 = new Activity
            {
                ActivityType = ActivityType.Email,
                Subject = "Sent Product Whitepaper",
                Description = "Forwarded security whitepaper to lead contact.",
                ActivityDate = DateTime.UtcNow.AddDays(-4),
                LeadId = lead2.LeadId,
                AssignedTo = salesUser1.Id,
                Status = ActivityStatus.Completed,
                CreatedDate = DateTime.UtcNow.AddDays(-4)
            };

            await context.Activities.AddRangeAsync(act1, act2, act3);
            await context.SaveChangesAsync();

            // 8. Seed Initial Audit Logs
            var audit1 = new AuditLog
            {
                UserId = adminUser.Id,
                UserName = adminUser.Email,
                Action = "Seed",
                EntityName = "System",
                RecordId = "0",
                NewValue = "Initial system bootstrap and demo data loaded.",
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                IpAddress = "127.0.0.1"
            };

            var audit2 = new AuditLog
            {
                UserId = salesUser1.Id,
                UserName = salesUser1.Email,
                Action = "Create",
                EntityName = "Opportunity",
                RecordId = opp1.OpportunityId.ToString(),
                NewValue = $"Created opportunity '{opp1.OpportunityName}' with amount ${opp1.Amount}.",
                CreatedDate = DateTime.UtcNow.AddDays(-15),
                IpAddress = "127.0.0.1"
            };

            await context.AuditLogs.AddRangeAsync(audit1, audit2);
            await context.SaveChangesAsync();
        }
    }
}

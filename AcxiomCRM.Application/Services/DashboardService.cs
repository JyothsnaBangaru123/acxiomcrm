using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs.Activities;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.DTOs.Dashboard;
using AcxiomCRM.Application.DTOs.FollowUps;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Domain.Interfaces;

namespace AcxiomCRM.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardViewModelDto> GetDashboardDataAsync(UserContext userContext, string dateFilter = "All", CancellationToken cancellationToken = default)
    {
        var result = new DashboardViewModelDto { DateFilter = dateFilter };

        // 1. Scoped base queries
        IQueryable<Customer> custQuery = _unitOfWork.Customers.Query();
        IQueryable<Lead> leadQuery = _unitOfWork.Leads.Query();
        IQueryable<Opportunity> oppQuery = _unitOfWork.Opportunities.Query();
        IQueryable<FollowUp> fuQuery = _unitOfWork.FollowUps.Query();
        IQueryable<Activity> actQuery = _unitOfWork.Activities.Query();

        if (userContext.IsSalesExecutive)
        {
            custQuery = custQuery.Where(c => c.OwnerId == userContext.UserId);
            leadQuery = leadQuery.Where(l => l.AssignedTo == userContext.UserId);
            oppQuery = oppQuery.Where(o => o.AssignedTo == userContext.UserId || (o.Customer != null && o.Customer.OwnerId == userContext.UserId));
            fuQuery = fuQuery.Where(f => f.AssignedTo == userContext.UserId);
            actQuery = actQuery.Where(a => a.AssignedTo == userContext.UserId);
        }

        // Date filtering
        var now = DateTime.UtcNow;
        DateTime? startDate = dateFilter switch
        {
            "Today" => now.Date,
            "ThisWeek" => now.Date.AddDays(-(int)now.DayOfWeek),
            "ThisMonth" => new DateTime(now.Year, now.Month, 1),
            "ThisQuarter" => new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1),
            "ThisYear" => new DateTime(now.Year, 1, 1),
            _ => null
        };

        if (startDate.HasValue)
        {
            leadQuery = leadQuery.Where(l => l.CreatedDate >= startDate.Value);
            oppQuery = oppQuery.Where(o => o.CreatedDate >= startDate.Value);
            fuQuery = fuQuery.Where(f => f.CreatedDate >= startDate.Value);
            actQuery = actQuery.Where(a => a.CreatedDate >= startDate.Value);
        }

        // 2. Compute KPIs
        result.Kpis.TotalCustomers = await custQuery.CountAsync(cancellationToken);
        result.Kpis.TotalLeads = await leadQuery.CountAsync(cancellationToken);
        result.Kpis.OpenLeads = await leadQuery.CountAsync(l => l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost, cancellationToken);
        result.Kpis.ConvertedLeads = await leadQuery.CountAsync(l => l.Status == LeadStatus.Converted, cancellationToken);

        result.Kpis.TotalOpportunities = await oppQuery.CountAsync(cancellationToken);
        result.Kpis.OpenOpportunities = await oppQuery.CountAsync(o => o.Status == OpportunityStatus.Open, cancellationToken);
        result.Kpis.WonOpportunities = await oppQuery.CountAsync(o => o.Status == OpportunityStatus.Won, cancellationToken);
        result.Kpis.LostOpportunities = await oppQuery.CountAsync(o => o.Status == OpportunityStatus.Lost, cancellationToken);

        var openOpps = await oppQuery.Where(o => o.Status == OpportunityStatus.Open).ToListAsync(cancellationToken);
        result.Kpis.TotalPipelineValue = openOpps.Sum(o => o.Amount);
        result.Kpis.WeightedPipelineValue = Math.Round(openOpps.Sum(o => o.Amount * o.Probability / 100m), 2);
        result.Kpis.TotalWonRevenue = await oppQuery.Where(o => o.Status == OpportunityStatus.Won).SumAsync(o => o.Amount, cancellationToken);

        var today = DateTime.UtcNow.Date;
        result.Kpis.PendingFollowUps = await fuQuery.CountAsync(f => f.Status == FollowUpStatus.Planned, cancellationToken);
        result.Kpis.OverdueFollowUps = await fuQuery.CountAsync(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date < today, cancellationToken);
        result.Kpis.TotalActivities = await actQuery.CountAsync(cancellationToken);

        // 3. Lead Status Chart
        var leadStatuses = Enum.GetValues<LeadStatus>();
        var leadCounts = await leadQuery
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        result.LeadStatusChart = new ChartDataDto
        {
            Labels = leadStatuses.Select(s => s.ToString()).ToList(),
            Values = leadStatuses.Select(s => (decimal)(leadCounts.FirstOrDefault(c => c.Status == s)?.Count ?? 0)).ToList(),
            BackgroundColors = new List<string> { "#3b82f6", "#06b6d4", "#10b981", "#64748b", "#8b5cf6", "#ef4444" }
        };

        // 4. Opportunity Pipeline Chart (Stage Amount)
        var stages = Enum.GetValues<OpportunityStage>();
        var stageAmounts = await oppQuery
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, Total = g.Sum(o => o.Amount) })
            .ToListAsync(cancellationToken);

        result.OpportunityPipelineChart = new ChartDataDto
        {
            Labels = stages.Select(s => s.ToString()).ToList(),
            Values = stages.Select(s => stageAmounts.FirstOrDefault(a => a.Stage == s)?.Total ?? 0m).ToList(),
            BackgroundColors = new List<string> { "#6366f1", "#0284c7", "#f59e0b", "#10b981", "#ef4444" }
        };

        // 5. Monthly Sales Trend (Last 6 Months)
        var months = new List<string>();
        var monthlyTotals = new List<decimal>();

        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = DateTime.UtcNow.AddMonths(-i);
            var mStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
            var mEnd = mStart.AddMonths(1).AddTicks(-1);

            var monthName = targetMonth.ToString("MMM yyyy");
            months.Add(monthName);

            var wonTotal = await oppQuery
                .Where(o => o.Status == OpportunityStatus.Won && o.ExpectedCloseDate >= mStart && o.ExpectedCloseDate <= mEnd)
                .SumAsync(o => (decimal?)o.Amount, cancellationToken) ?? 0m;

            monthlyTotals.Add(wonTotal);
        }

        result.MonthlySalesChart = new ChartDataDto
        {
            Labels = months,
            Values = monthlyTotals,
            BackgroundColors = Enumerable.Repeat("#10b981", months.Count).ToList()
        };

        // 6. Recent Opportunities
        result.RecentOpportunities = await oppQuery
            .OrderByDescending(o => o.CreatedDate)
            .Take(5)
            .Select(o => new OpportunitySummaryDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status
            })
            .ToListAsync(cancellationToken);

        // 7. Upcoming Follow-ups
        result.UpcomingFollowUps = await fuQuery
            .Where(f => f.Status == FollowUpStatus.Planned)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedTo = f.AssignedTo,
                AssignedUserName = f.AssignedUser != null ? f.AssignedUser.FullName : "Unassigned"
            })
            .ToListAsync(cancellationToken);

        // 8. Recent Activities
        result.RecentActivities = await actQuery
            .OrderByDescending(a => a.ActivityDate)
            .Take(5)
            .Select(a => new ActivityDto
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                Description = a.Description,
                ActivityDate = a.ActivityDate,
                CustomerId = a.CustomerId,
                CustomerName = a.Customer != null ? a.Customer.CustomerName : null,
                LeadId = a.LeadId,
                LeadName = a.Lead != null ? a.Lead.LeadName : null,
                AssignedTo = a.AssignedTo,
                AssignedUserName = a.AssignedUser != null ? a.AssignedUser.FullName : "Unassigned",
                Status = a.Status
            })
            .ToListAsync(cancellationToken);

        return result;
    }
}

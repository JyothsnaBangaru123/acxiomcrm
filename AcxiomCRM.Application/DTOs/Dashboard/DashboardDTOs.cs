namespace AcxiomCRM.Application.DTOs.Dashboard;

public class DashboardKpiDto
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public decimal TotalWonRevenue { get; set; }
    public int PendingFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }
    public int TotalActivities { get; set; }
}

public class ChartDataDto
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Values { get; set; } = new();
    public List<string> BackgroundColors { get; set; } = new();
}

public class DashboardViewModelDto
{
    public DashboardKpiDto Kpis { get; set; } = new();
    public ChartDataDto LeadStatusChart { get; set; } = new();
    public ChartDataDto OpportunityPipelineChart { get; set; } = new();
    public ChartDataDto MonthlySalesChart { get; set; } = new();
    public List<Customers.OpportunitySummaryDto> RecentOpportunities { get; set; } = new();
    public List<FollowUps.FollowUpDto> UpcomingFollowUps { get; set; } = new();
    public List<Activities.ActivityDto> RecentActivities { get; set; } = new();
    public string DateFilter { get; set; } = "All"; // Today, ThisWeek, ThisMonth, ThisQuarter, ThisYear, All
}

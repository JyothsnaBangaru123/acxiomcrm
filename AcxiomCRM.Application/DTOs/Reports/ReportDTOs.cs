using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.DTOs.Reports;

public class ReportFilterDto
{
    public string? ReportType { get; set; } = "Pipeline";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? AssignedTo { get; set; }
    public string? Status { get; set; }
    public string? Stage { get; set; }
}

public class PipelineStageReportItemDto
{
    public OpportunityStage Stage { get; set; }
    public string StageName => Stage.ToString();
    public int OpportunityCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class OwnerPipelineReportItemDto
{
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int ActiveOpportunitiesCount { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WonAmount { get; set; }
    public int WonCount { get; set; }
}

public class ConversionRateReportDto
{
    public int TotalLeads { get; set; }
    public int QualifiedLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public int LostLeads { get; set; }
    public decimal QualificationRate => TotalLeads == 0 ? 0 : Math.Round((decimal)QualifiedLeads / TotalLeads * 100m, 1);
    public decimal ConversionRate => TotalLeads == 0 ? 0 : Math.Round((decimal)ConvertedLeads / TotalLeads * 100m, 1);
    public decimal TotalWonRevenue { get; set; }
}

public class UserActivityReportItemDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int TotalCalls { get; set; }
    public int TotalMeetings { get; set; }
    public int TotalEmails { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedFollowUps { get; set; }
    public int PendingFollowUps { get; set; }
    public int TotalActivitiesCount => TotalCalls + TotalMeetings + TotalEmails + TotalTasks;
}

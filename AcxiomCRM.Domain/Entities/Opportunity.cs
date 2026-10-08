using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Entities;

public class Opportunity
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public virtual Lead? Lead { get; set; }

    public decimal Amount { get; set; }
    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;
    public int Probability { get; set; } = 10;
    public DateTime ExpectedCloseDate { get; set; }
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
    public string? Notes { get; set; }

    // Assignment
    public string? AssignedTo { get; set; }
    public virtual ApplicationUser? AssignedUser { get; set; }

    // Calculated Weighted Pipeline
    public decimal WeightedPipeline => Math.Round(Amount * Probability / 100m, 2);

    // Navigations
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}

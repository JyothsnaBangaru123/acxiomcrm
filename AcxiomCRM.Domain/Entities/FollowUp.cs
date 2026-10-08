using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Entities;

public class FollowUp
{
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }
    public virtual Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }
    public virtual Opportunity? Opportunity { get; set; }

    public DateTime FollowUpDate { get; set; }
    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;
    public string? Subject { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    public string? AssignedTo { get; set; }
    public virtual ApplicationUser? AssignedUser { get; set; }

    public DateTime? CompletedDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

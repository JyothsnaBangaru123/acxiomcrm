using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Entities;

public class Lead
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public LeadSource Source { get; set; } = LeadSource.Website;
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public LeadPriority Priority { get; set; } = LeadPriority.Medium;
    public decimal ExpectedValue { get; set; } = 0m;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
    public string? Notes { get; set; }

    // Assignment to Sales Executive / User
    public string? AssignedTo { get; set; }
    public virtual ApplicationUser? AssignedUser { get; set; }

    // Conversion details
    public int? ConvertedCustomerId { get; set; }
    public virtual Customer? ConvertedCustomer { get; set; }
    public DateTime? ConvertedDate { get; set; }

    // Navigations
    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}

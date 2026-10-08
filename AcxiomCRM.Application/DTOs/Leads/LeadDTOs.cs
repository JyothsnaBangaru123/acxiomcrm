using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.DTOs.Leads;

public class LeadDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public LeadSource Source { get; set; }
    public LeadStatus Status { get; set; }
    public LeadPriority Priority { get; set; }
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public int? ConvertedCustomerId { get; set; }
    public string? ConvertedCustomerName { get; set; }
    public DateTime? ConvertedDate { get; set; }
    public string? Notes { get; set; }
}

public class CreateLeadDto
{
    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(150, ErrorMessage = "Lead Name cannot exceed 150 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^\+?([0-9()\-. \s]{7,25})$", ErrorMessage = "Enter a valid phone number (10-15 digits).")]
    [StringLength(25, ErrorMessage = "Phone cannot exceed 25 characters.")]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Display(Name = "Lead Source")]
    public LeadSource Source { get; set; } = LeadSource.Website;

    [Display(Name = "Status")]
    public LeadStatus Status { get; set; } = LeadStatus.New;

    [Display(Name = "Priority")]
    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(0, 100000000, ErrorMessage = "Expected value must be a valid positive amount.")]
    [Display(Name = "Expected Value ($)")]
    public decimal ExpectedValue { get; set; } = 0;

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedTo { get; set; }

    [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
    public string? Notes { get; set; }
}

public class UpdateLeadDto : CreateLeadDto
{
    [Required]
    public int LeadId { get; set; }
}

public class ConvertLeadDto
{
    [Required]
    public int LeadId { get; set; }

    [Display(Name = "Create an Opportunity upon conversion")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    public string? OpportunityName { get; set; }

    [Display(Name = "Opportunity Amount ($)")]
    [Range(0.01, 100000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal? OpportunityAmount { get; set; }

    [Display(Name = "Expected Close Date")]
    public DateTime? ExpectedCloseDate { get; set; }

    [Display(Name = "Opportunity Stage")]
    public OpportunityStage? OpportunityStage { get; set; } = Domain.Enums.OpportunityStage.Qualification;

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Probability (%)")]
    public int? Probability { get; set; } = 20;
}

public class LeadFilterDto
{
    public string? SearchTerm { get; set; }
    public LeadStatus? Status { get; set; }
    public LeadSource? Source { get; set; }
    public LeadPriority? Priority { get; set; }
    public string? AssignedTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

public class LeadDetailDto : LeadDto
{
    public List<Customers.OpportunitySummaryDto> Opportunities { get; set; } = new();
    public List<Customers.FollowUpSummaryDto> FollowUps { get; set; } = new();
    public List<Customers.ActivitySummaryDto> Activities { get; set; } = new();
}

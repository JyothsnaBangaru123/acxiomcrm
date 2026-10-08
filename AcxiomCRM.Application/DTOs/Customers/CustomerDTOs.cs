using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.DTOs.Customers;

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public CustomerStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public int OpportunitiesCount { get; set; }
    public decimal TotalPipelineAmount { get; set; }
}

public class CreateCustomerDto
{
    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(150, ErrorMessage = "Customer Name cannot exceed 150 characters.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

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

    [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
    public string? Address { get; set; }

    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
    public string? State { get; set; }

    [Display(Name = "Status")]
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    [Display(Name = "Assigned Sales Executive")]
    public string? OwnerId { get; set; }
}

public class UpdateCustomerDto : CreateCustomerDto
{
    [Required]
    public int CustomerId { get; set; }
}

public class CustomerFilterDto
{
    public string? SearchTerm { get; set; }
    public string? City { get; set; }
    public CustomerStatus? Status { get; set; }
    public string? OwnerId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

public class CustomerDetailDto : CustomerDto
{
    public List<LeadSummaryDto> Leads { get; set; } = new();
    public List<OpportunitySummaryDto> Opportunities { get; set; } = new();
    public List<FollowUpSummaryDto> FollowUps { get; set; } = new();
    public List<ActivitySummaryDto> Activities { get; set; } = new();
}

public class LeadSummaryDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public LeadStatus Status { get; set; }
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class OpportunitySummaryDto
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public OpportunityStage Stage { get; set; }
    public int Probability { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public OpportunityStatus Status { get; set; }
}

public class FollowUpSummaryDto
{
    public int FollowUpId { get; set; }
    public DateTime FollowUpDate { get; set; }
    public FollowUpType FollowUpType { get; set; }
    public string? Subject { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public FollowUpStatus Status { get; set; }
}

public class ActivitySummaryDto
{
    public int ActivityId { get; set; }
    public ActivityType ActivityType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public DateTime ActivityDate { get; set; }
    public ActivityStatus Status { get; set; }
}

using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.DTOs.FollowUps;

public class FollowUpDto
{
    public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public int? OpportunityId { get; set; }
    public string? OpportunityName { get; set; }
    public DateTime FollowUpDate { get; set; }
    public FollowUpType FollowUpType { get; set; }
    public string? Subject { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public FollowUpStatus Status { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsOverdue => Status == FollowUpStatus.Planned && FollowUpDate.Date < DateTime.UtcNow.Date;
}

public class CreateFollowUpDto
{
    [Display(Name = "Related Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Related Opportunity")]
    public int? OpportunityId { get; set; }

    [Required(ErrorMessage = "Follow-up Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Follow-up Date")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.Date;

    [Display(Name = "Activity Type")]
    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Remarks are required.")]
    [StringLength(2000, ErrorMessage = "Remarks cannot exceed 2000 characters.")]
    public string Remarks { get; set; } = string.Empty;

    [Display(Name = "Status")]
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [Display(Name = "Assigned User")]
    public string? AssignedTo { get; set; }
}

public class UpdateFollowUpDto : CreateFollowUpDto
{
    [Required]
    public int FollowUpId { get; set; }
}

public class RescheduleFollowUpDto
{
    [Required]
    public int FollowUpId { get; set; }

    [Required(ErrorMessage = "New follow-up date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "New Follow-up Date")]
    public DateTime NewFollowUpDate { get; set; } = DateTime.UtcNow.Date.AddDays(1);

    [Required(ErrorMessage = "Reason for rescheduling is required.")]
    [StringLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters.")]
    [Display(Name = "Reschedule Notes / Reason")]
    public string Reason { get; set; } = string.Empty;
}

public class CompleteFollowUpDto
{
    [Required]
    public int FollowUpId { get; set; }

    [Required(ErrorMessage = "Completion remarks are required.")]
    [StringLength(2000, ErrorMessage = "Completion remarks cannot exceed 2000 characters.")]
    [Display(Name = "Completion Remarks")]
    public string CompletionRemarks { get; set; } = string.Empty;
}

public class FollowUpFilterDto
{
    public string? SearchTerm { get; set; }
    public FollowUpStatus? Status { get; set; }
    public FollowUpType? Type { get; set; }
    public string? AssignedTo { get; set; }
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool OnlyOverdue { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

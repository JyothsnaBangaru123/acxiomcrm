using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.FollowUps;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Domain.Interfaces;
using AcxiomCRM.Domain.Rules;

namespace AcxiomCRM.Application.Services;

public class FollowUpService : IFollowUpService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public FollowUpService(IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    private IQueryable<FollowUp> ApplyAuthorization(IQueryable<FollowUp> query, UserContext userContext)
    {
        if (userContext.IsAdmin || userContext.IsManager)
        {
            return query;
        }

        return query.Where(f => f.AssignedTo == userContext.UserId);
    }

    private async Task<bool> CanUserAccessFollowUpAsync(int followUpId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (userContext.IsAdmin || userContext.IsManager) return true;
        return await _unitOfWork.FollowUps.ExistsAsync(f => f.FollowUpId == followUpId && f.AssignedTo == userContext.UserId, cancellationToken);
    }

    public async Task<PagedResult<FollowUpDto>> GetFollowUpsAsync(FollowUpFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default)
    {
        var query = ApplyAuthorization(_unitOfWork.FollowUps.Query(), userContext);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(f => (f.Subject != null && f.Subject.ToLower().Contains(term))
                                  || f.Remarks.ToLower().Contains(term)
                                  || (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(term))
                                  || (f.Lead != null && f.Lead.LeadName.ToLower().Contains(term)));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(f => f.Status == filter.Status.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(f => f.FollowUpType == filter.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
        {
            query = query.Where(f => f.AssignedTo == filter.AssignedTo);
        }

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(f => f.CustomerId == filter.CustomerId.Value);
        }

        if (filter.LeadId.HasValue)
        {
            query = query.Where(f => f.LeadId == filter.LeadId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(f => f.FollowUpDate.Date >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(f => f.FollowUpDate.Date <= filter.ToDate.Value.Date);
        }

        if (filter.OnlyOverdue)
        {
            query = query.Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date < DateTime.UtcNow.Date);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(f => f.FollowUpDate)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                OpportunityId = f.OpportunityId,
                OpportunityName = f.Opportunity != null ? f.Opportunity.OpportunityName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedTo = f.AssignedTo,
                AssignedUserName = f.AssignedUser != null ? f.AssignedUser.FullName : "Unassigned",
                CompletedDate = f.CompletedDate,
                CreatedDate = f.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<FollowUpDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<FollowUpDto?> GetByIdAsync(int followUpId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessFollowUpAsync(followUpId, userContext, cancellationToken))
        {
            return null;
        }

        var f = await _unitOfWork.FollowUps.GetWithDetailsAsync(followUpId, cancellationToken);
        if (f == null) return null;

        return new FollowUpDto
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer?.CustomerName,
            LeadId = f.LeadId,
            LeadName = f.Lead?.LeadName,
            OpportunityId = f.OpportunityId,
            OpportunityName = f.Opportunity?.OpportunityName,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType,
            Subject = f.Subject,
            Remarks = f.Remarks,
            Status = f.Status,
            AssignedTo = f.AssignedTo,
            AssignedUserName = f.AssignedUser?.FullName ?? "Unassigned",
            CompletedDate = f.CompletedDate,
            CreatedDate = f.CreatedDate
        };
    }

    public async Task<ApiResponse<FollowUpDto>> CreateFollowUpAsync(CreateFollowUpDto dto, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Remarks))
        {
            return ApiResponse<FollowUpDto>.Fail("Remarks are required.");
        }

        // Validate date rule
        var (isValidDate, dateError) = FollowUpRules.ValidateDate(dto.FollowUpDate, dto.Status);
        if (!isValidDate)
        {
            return ApiResponse<FollowUpDto>.Fail(dateError!);
        }

        var assignedTo = userContext.IsSalesExecutive ? userContext.UserId : (dto.AssignedTo ?? userContext.UserId);

        var followUp = new FollowUp
        {
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId,
            FollowUpDate = dto.FollowUpDate,
            FollowUpType = dto.FollowUpType,
            Subject = dto.Subject?.Trim(),
            Remarks = dto.Remarks.Trim(),
            Status = dto.Status,
            AssignedTo = assignedTo,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.FollowUps.AddAsync(followUp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Create",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            null,
            $"Created {followUp.FollowUpType} FollowUp for {followUp.FollowUpDate:yyyy-MM-dd}",
            userContext.IpAddress,
            cancellationToken);

        var result = new FollowUpDto
        {
            FollowUpId = followUp.FollowUpId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            OpportunityId = followUp.OpportunityId,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Subject = followUp.Subject,
            Remarks = followUp.Remarks,
            Status = followUp.Status,
            AssignedTo = followUp.AssignedTo,
            CreatedDate = followUp.CreatedDate
        };

        return ApiResponse<FollowUpDto>.Ok(result, "Follow-up scheduled successfully.");
    }

    public async Task<ApiResponse<FollowUpDto>> UpdateFollowUpAsync(UpdateFollowUpDto dto, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessFollowUpAsync(dto.FollowUpId, userContext, cancellationToken))
        {
            return ApiResponse<FollowUpDto>.Fail("Unauthorized access to follow-up record.");
        }

        var followUp = await _unitOfWork.FollowUps.GetByIdAsync(dto.FollowUpId, cancellationToken);
        if (followUp == null)
        {
            return ApiResponse<FollowUpDto>.Fail("Follow-up not found.");
        }

        if (string.IsNullOrWhiteSpace(dto.Remarks))
        {
            return ApiResponse<FollowUpDto>.Fail("Remarks are required.");
        }

        var oldSummary = $"Date: {followUp.FollowUpDate:yyyy-MM-dd}, Status: {followUp.Status}";

        followUp.CustomerId = dto.CustomerId;
        followUp.LeadId = dto.LeadId;
        followUp.OpportunityId = dto.OpportunityId;
        followUp.FollowUpDate = dto.FollowUpDate;
        followUp.FollowUpType = dto.FollowUpType;
        followUp.Subject = dto.Subject?.Trim();
        followUp.Remarks = dto.Remarks.Trim();
        followUp.Status = dto.Status;

        if (!userContext.IsSalesExecutive && !string.IsNullOrWhiteSpace(dto.AssignedTo))
        {
            followUp.AssignedTo = dto.AssignedTo;
        }

        await _unitOfWork.FollowUps.UpdateAsync(followUp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var newSummary = $"Date: {followUp.FollowUpDate:yyyy-MM-dd}, Status: {followUp.Status}";

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Update",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            oldSummary,
            newSummary,
            userContext.IpAddress,
            cancellationToken);

        var result = new FollowUpDto
        {
            FollowUpId = followUp.FollowUpId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            OpportunityId = followUp.OpportunityId,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Subject = followUp.Subject,
            Remarks = followUp.Remarks,
            Status = followUp.Status,
            AssignedTo = followUp.AssignedTo,
            CreatedDate = followUp.CreatedDate
        };

        return ApiResponse<FollowUpDto>.Ok(result, "Follow-up updated successfully.");
    }

    public async Task<ApiResponse<bool>> RescheduleFollowUpAsync(RescheduleFollowUpDto dto, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessFollowUpAsync(dto.FollowUpId, userContext, cancellationToken))
        {
            return ApiResponse<bool>.Fail("Unauthorized access to follow-up record.");
        }

        var followUp = await _unitOfWork.FollowUps.GetByIdAsync(dto.FollowUpId, cancellationToken);
        if (followUp == null)
        {
            return ApiResponse<bool>.Fail("Follow-up not found.");
        }

        var (isValidDate, dateError) = FollowUpRules.ValidateDate(dto.NewFollowUpDate, FollowUpStatus.Planned);
        if (!isValidDate)
        {
            return ApiResponse<bool>.Fail(dateError!);
        }

        var oldDate = followUp.FollowUpDate;
        followUp.FollowUpDate = dto.NewFollowUpDate;
        followUp.Status = FollowUpStatus.Planned;
        followUp.Remarks = $"{followUp.Remarks}\n[Rescheduled from {oldDate:yyyy-MM-dd}]: {dto.Reason.Trim()}";

        await _unitOfWork.FollowUps.UpdateAsync(followUp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Reschedule",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            $"Date: {oldDate:yyyy-MM-dd}",
            $"Rescheduled to {dto.NewFollowUpDate:yyyy-MM-dd}. Reason: {dto.Reason}",
            userContext.IpAddress,
            cancellationToken);

        return ApiResponse<bool>.Ok(true, "Follow-up rescheduled successfully.");
    }

    public async Task<ApiResponse<bool>> CompleteFollowUpAsync(CompleteFollowUpDto dto, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessFollowUpAsync(dto.FollowUpId, userContext, cancellationToken))
        {
            return ApiResponse<bool>.Fail("Unauthorized access to follow-up record.");
        }

        var followUp = await _unitOfWork.FollowUps.GetByIdAsync(dto.FollowUpId, cancellationToken);
        if (followUp == null)
        {
            return ApiResponse<bool>.Fail("Follow-up not found.");
        }

        followUp.Status = FollowUpStatus.Completed;
        followUp.CompletedDate = DateTime.UtcNow;
        followUp.Remarks = $"{followUp.Remarks}\n[Completed on {DateTime.UtcNow:yyyy-MM-dd}]: {dto.CompletionRemarks.Trim()}";

        // Also record an Activity for history tracking
        var activity = new Activity
        {
            ActivityType = followUp.FollowUpType switch
            {
                FollowUpType.Call => ActivityType.Call,
                FollowUpType.Meeting => ActivityType.Meeting,
                FollowUpType.Email => ActivityType.Email,
                _ => ActivityType.Task
            },
            Subject = followUp.Subject ?? $"Completed {followUp.FollowUpType}",
            Description = dto.CompletionRemarks,
            ActivityDate = DateTime.UtcNow,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            AssignedTo = followUp.AssignedTo,
            Status = ActivityStatus.Completed,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.Activities.AddAsync(activity, cancellationToken);
        await _unitOfWork.FollowUps.UpdateAsync(followUp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Complete",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            "Status: Planned",
            $"FollowUp marked as Completed: {dto.CompletionRemarks}",
            userContext.IpAddress,
            cancellationToken);

        return ApiResponse<bool>.Ok(true, "Follow-up completed successfully.");
    }

    public async Task<ApiResponse<bool>> CancelFollowUpAsync(int followUpId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessFollowUpAsync(followUpId, userContext, cancellationToken))
        {
            return ApiResponse<bool>.Fail("Unauthorized access to follow-up record.");
        }

        var followUp = await _unitOfWork.FollowUps.GetByIdAsync(followUpId, cancellationToken);
        if (followUp == null)
        {
            return ApiResponse<bool>.Fail("Follow-up not found.");
        }

        followUp.Status = FollowUpStatus.Cancelled;
        await _unitOfWork.FollowUps.UpdateAsync(followUp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Cancel",
            "FollowUp",
            followUpId.ToString(),
            "Status: Planned",
            "Status: Cancelled",
            userContext.IpAddress,
            cancellationToken);

        return ApiResponse<bool>.Ok(true, "Follow-up cancelled.");
    }

    public async Task<ApiResponse<bool>> DeleteFollowUpAsync(int followUpId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessFollowUpAsync(followUpId, userContext, cancellationToken))
        {
            return ApiResponse<bool>.Fail("Unauthorized access to follow-up record.");
        }

        var followUp = await _unitOfWork.FollowUps.GetByIdAsync(followUpId, cancellationToken);
        if (followUp == null)
        {
            return ApiResponse<bool>.Fail("Follow-up not found.");
        }

        await _unitOfWork.FollowUps.DeleteAsync(followUp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Delete",
            "FollowUp",
            followUpId.ToString(),
            $"FollowUp {followUpId}",
            "Deleted",
            userContext.IpAddress,
            cancellationToken);

        return ApiResponse<bool>.Ok(true, "Follow-up deleted successfully.");
    }
}

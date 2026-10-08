using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Activities;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Interfaces;

namespace AcxiomCRM.Application.Services;

public class ActivityService : IActivityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public ActivityService(IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    private IQueryable<Activity> ApplyAuthorization(IQueryable<Activity> query, UserContext userContext)
    {
        if (userContext.IsAdmin || userContext.IsManager)
        {
            return query;
        }

        return query.Where(a => a.AssignedTo == userContext.UserId);
    }

    public async Task<PagedResult<ActivityDto>> GetActivitiesAsync(ActivityFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default)
    {
        var query = ApplyAuthorization(_unitOfWork.Activities.Query(), userContext);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(a => a.Subject.ToLower().Contains(term)
                                  || (a.Description != null && a.Description.ToLower().Contains(term)));
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(a => a.ActivityType == filter.Type.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(a => a.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
        {
            query = query.Where(a => a.AssignedTo == filter.AssignedTo);
        }

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(a => a.CustomerId == filter.CustomerId.Value);
        }

        if (filter.LeadId.HasValue)
        {
            query = query.Where(a => a.LeadId == filter.LeadId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(a => a.ActivityDate.Date >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(a => a.ActivityDate.Date <= filter.ToDate.Value.Date);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.ActivityDate)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
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
                Status = a.Status,
                CreatedDate = a.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ActivityDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<ApiResponse<ActivityDto>> CreateActivityAsync(CreateActivityDto dto, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Subject))
        {
            return ApiResponse<ActivityDto>.Fail("Subject is required.");
        }

        var assignedTo = userContext.IsSalesExecutive ? userContext.UserId : (dto.AssignedTo ?? userContext.UserId);

        var activity = new Activity
        {
            ActivityType = dto.ActivityType,
            Subject = dto.Subject.Trim(),
            Description = dto.Description?.Trim(),
            ActivityDate = dto.ActivityDate,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            AssignedTo = assignedTo,
            Status = dto.Status,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.Activities.AddAsync(activity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Create",
            "Activity",
            activity.ActivityId.ToString(),
            null,
            $"Created {activity.ActivityType} activity '{activity.Subject}'",
            userContext.IpAddress,
            cancellationToken);

        var result = new ActivityDto
        {
            ActivityId = activity.ActivityId,
            ActivityType = activity.ActivityType,
            Subject = activity.Subject,
            Description = activity.Description,
            ActivityDate = activity.ActivityDate,
            CustomerId = activity.CustomerId,
            LeadId = activity.LeadId,
            AssignedTo = activity.AssignedTo,
            Status = activity.Status,
            CreatedDate = activity.CreatedDate
        };

        return ApiResponse<ActivityDto>.Ok(result, "Activity recorded successfully.");
    }

    public async Task<IReadOnlyList<ActivityDto>> GetRecentActivitiesForRecordAsync(int? customerId, int? leadId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        var query = ApplyAuthorization(_unitOfWork.Activities.Query(), userContext);

        if (customerId.HasValue)
        {
            query = query.Where(a => a.CustomerId == customerId.Value);
        }
        else if (leadId.HasValue)
        {
            query = query.Where(a => a.LeadId == leadId.Value);
        }
        else
        {
            return new List<ActivityDto>();
        }

        return await query
            .OrderByDescending(a => a.ActivityDate)
            .Take(20)
            .Select(a => new ActivityDto
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                Description = a.Description,
                ActivityDate = a.ActivityDate,
                CustomerId = a.CustomerId,
                LeadId = a.LeadId,
                AssignedTo = a.AssignedTo,
                AssignedUserName = a.AssignedUser != null ? a.AssignedUser.FullName : "Unassigned",
                Status = a.Status,
                CreatedDate = a.CreatedDate
            })
            .ToListAsync(cancellationToken);
    }
}

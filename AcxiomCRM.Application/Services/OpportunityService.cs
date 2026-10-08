using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Opportunities;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Domain.Interfaces;
using AcxiomCRM.Domain.Rules;

namespace AcxiomCRM.Application.Services;

public class OpportunityService : IOpportunityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public OpportunityService(IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    private IQueryable<Opportunity> ApplyAuthorization(IQueryable<Opportunity> query, UserContext userContext)
    {
        if (userContext.IsAdmin || userContext.IsManager)
        {
            return query;
        }

        return query.Where(o => o.AssignedTo == userContext.UserId || (o.Customer != null && o.Customer.OwnerId == userContext.UserId));
    }

    public async Task<bool> CanUserAccessOpportunityAsync(int opportunityId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (userContext.IsAdmin || userContext.IsManager) return true;
        return await _unitOfWork.Opportunities.ExistsAsync(
            o => o.OpportunityId == opportunityId && (o.AssignedTo == userContext.UserId || (o.Customer != null && o.Customer.OwnerId == userContext.UserId)),
            cancellationToken);
    }

    public async Task<PagedResult<OpportunityDto>> GetOpportunitiesAsync(OpportunityFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default)
    {
        var query = ApplyAuthorization(_unitOfWork.Opportunities.Query(), userContext);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(o => o.OpportunityName.ToLower().Contains(term)
                                  || (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)));
        }

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == filter.CustomerId.Value);
        }

        if (filter.Stage.HasValue)
        {
            query = query.Where(o => o.Stage == filter.Stage.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
        {
            query = query.Where(o => o.AssignedTo == filter.AssignedTo);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDescending ? query.OrderByDescending(o => o.OpportunityName) : query.OrderBy(o => o.OpportunityName),
            "amount" => filter.SortDescending ? query.OrderByDescending(o => o.Amount) : query.OrderBy(o => o.Amount),
            "stage" => filter.SortDescending ? query.OrderByDescending(o => o.Stage) : query.OrderBy(o => o.Stage),
            "probability" => filter.SortDescending ? query.OrderByDescending(o => o.Probability) : query.OrderBy(o => o.Probability),
            "closedate" => filter.SortDescending ? query.OrderByDescending(o => o.ExpectedCloseDate) : query.OrderBy(o => o.ExpectedCloseDate),
            _ => query.OrderByDescending(o => o.CreatedDate)
        };

        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : "Unknown",
                LeadId = o.LeadId,
                LeadName = o.Lead != null ? o.Lead.LeadName : null,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                CreatedDate = o.CreatedDate,
                AssignedTo = o.AssignedTo,
                AssignedUserName = o.AssignedUser != null ? o.AssignedUser.FullName : "Unassigned",
                Notes = o.Notes,
                WeightedPipeline = Math.Round(o.Amount * o.Probability / 100m, 2)
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<OpportunityDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<OpportunityDetailDto?> GetOpportunityDetailsAsync(int opportunityId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessOpportunityAsync(opportunityId, userContext, cancellationToken))
        {
            return null;
        }

        var opp = await _unitOfWork.Opportunities.GetWithDetailsAsync(opportunityId, cancellationToken);
        if (opp == null) return null;

        return new OpportunityDetailDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            CustomerName = opp.Customer?.CustomerName ?? "Unknown",
            LeadId = opp.LeadId,
            LeadName = opp.Lead?.LeadName,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            CreatedDate = opp.CreatedDate,
            AssignedTo = opp.AssignedTo,
            AssignedUserName = opp.AssignedUser?.FullName ?? "Unassigned",
            Notes = opp.Notes,
            WeightedPipeline = opp.WeightedPipeline,
            FollowUps = opp.FollowUps.Select(f => new DTOs.Customers.FollowUpSummaryDto
            {
                FollowUpId = f.FollowUpId,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status
            }).ToList()
        };
    }

    public async Task<OpportunityDto?> GetByIdAsync(int opportunityId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessOpportunityAsync(opportunityId, userContext, cancellationToken))
        {
            return null;
        }

        var opp = await _unitOfWork.Opportunities.GetWithDetailsAsync(opportunityId, cancellationToken);
        if (opp == null) return null;

        return new OpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            CustomerName = opp.Customer?.CustomerName ?? "Unknown",
            LeadId = opp.LeadId,
            LeadName = opp.Lead?.LeadName,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            CreatedDate = opp.CreatedDate,
            AssignedTo = opp.AssignedTo,
            Notes = opp.Notes,
            WeightedPipeline = opp.WeightedPipeline
        };
    }

    public async Task<ApiResponse<OpportunityDto>> CreateOpportunityAsync(CreateOpportunityDto dto, UserContext userContext, CancellationToken cancellationToken = default)
    {
        // 1. Mandatory field checks
        if (string.IsNullOrWhiteSpace(dto.OpportunityName))
        {
            return ApiResponse<OpportunityDto>.Fail("Opportunity Name is required.");
        }

        var customerExists = await _unitOfWork.Customers.ExistsAsync(c => c.CustomerId == dto.CustomerId, cancellationToken);
        if (!customerExists)
        {
            return ApiResponse<OpportunityDto>.Fail("Selected customer does not exist.");
        }

        // 2. Business Rules
        var status = dto.Stage == OpportunityStage.Won ? OpportunityStatus.Won : (dto.Stage == OpportunityStage.Lost ? OpportunityStatus.Lost : OpportunityStatus.Open);
        var (isValid, errorMessage) = OpportunityRules.Validate(dto.Amount, dto.Probability, dto.ExpectedCloseDate, status);
        if (!isValid)
        {
            return ApiResponse<OpportunityDto>.Fail(errorMessage!);
        }

        var assignedTo = userContext.IsSalesExecutive ? userContext.UserId : (dto.AssignedTo ?? userContext.UserId);

        var opp = new Opportunity
        {
            OpportunityName = dto.OpportunityName.Trim(),
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = status,
            CreatedDate = DateTime.UtcNow,
            AssignedTo = assignedTo,
            Notes = dto.Notes?.Trim()
        };

        await _unitOfWork.Opportunities.AddAsync(opp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Create",
            "Opportunity",
            opp.OpportunityId.ToString(),
            null,
            $"Created Opportunity '{opp.OpportunityName}' for ${opp.Amount} (Stage: {opp.Stage})",
            userContext.IpAddress,
            cancellationToken);

        var result = new OpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            CreatedDate = opp.CreatedDate,
            AssignedTo = opp.AssignedTo,
            WeightedPipeline = opp.WeightedPipeline
        };

        return ApiResponse<OpportunityDto>.Ok(result, "Opportunity created successfully.");
    }

    public async Task<ApiResponse<OpportunityDto>> UpdateOpportunityAsync(UpdateOpportunityDto dto, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessOpportunityAsync(dto.OpportunityId, userContext, cancellationToken))
        {
            return ApiResponse<OpportunityDto>.Fail("Unauthorized access to opportunity record.");
        }

        var opp = await _unitOfWork.Opportunities.GetByIdAsync(dto.OpportunityId, cancellationToken);
        if (opp == null)
        {
            return ApiResponse<OpportunityDto>.Fail("Opportunity not found.");
        }

        if (string.IsNullOrWhiteSpace(dto.OpportunityName))
        {
            return ApiResponse<OpportunityDto>.Fail("Opportunity Name is required.");
        }

        var status = dto.Stage == OpportunityStage.Won ? OpportunityStatus.Won : (dto.Stage == OpportunityStage.Lost ? OpportunityStatus.Lost : OpportunityStatus.Open);
        var (isValid, errorMessage) = OpportunityRules.Validate(dto.Amount, dto.Probability, dto.ExpectedCloseDate, status);
        if (!isValid)
        {
            return ApiResponse<OpportunityDto>.Fail(errorMessage!);
        }

        var oldSummary = $"Name: {opp.OpportunityName}, Amount: {opp.Amount}, Stage: {opp.Stage}, Prob: {opp.Probability}";

        opp.OpportunityName = dto.OpportunityName.Trim();
        opp.CustomerId = dto.CustomerId;
        opp.LeadId = dto.LeadId;
        opp.Amount = dto.Amount;
        opp.Stage = dto.Stage;
        opp.Probability = dto.Probability;
        opp.ExpectedCloseDate = dto.ExpectedCloseDate;
        opp.Status = status;
        opp.Notes = dto.Notes?.Trim();
        opp.ModifiedDate = DateTime.UtcNow;

        if (!userContext.IsSalesExecutive && !string.IsNullOrWhiteSpace(dto.AssignedTo))
        {
            opp.AssignedTo = dto.AssignedTo;
        }

        await _unitOfWork.Opportunities.UpdateAsync(opp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var newSummary = $"Name: {opp.OpportunityName}, Amount: {opp.Amount}, Stage: {opp.Stage}, Prob: {opp.Probability}";

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Update",
            "Opportunity",
            opp.OpportunityId.ToString(),
            oldSummary,
            newSummary,
            userContext.IpAddress,
            cancellationToken);

        var result = new OpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            CreatedDate = opp.CreatedDate,
            AssignedTo = opp.AssignedTo,
            WeightedPipeline = opp.WeightedPipeline
        };

        return ApiResponse<OpportunityDto>.Ok(result, "Opportunity updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteOpportunityAsync(int opportunityId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessOpportunityAsync(opportunityId, userContext, cancellationToken))
        {
            return ApiResponse<bool>.Fail("Unauthorized access to opportunity record.");
        }

        var opp = await _unitOfWork.Opportunities.GetByIdAsync(opportunityId, cancellationToken);
        if (opp == null)
        {
            return ApiResponse<bool>.Fail("Opportunity not found.");
        }

        opp.Status = OpportunityStatus.Lost;
        opp.Stage = OpportunityStage.Lost;
        opp.ModifiedDate = DateTime.UtcNow;

        await _unitOfWork.Opportunities.UpdateAsync(opp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "StatusChange",
            "Opportunity",
            opportunityId.ToString(),
            $"Opportunity '{opp.OpportunityName}' active",
            $"Opportunity marked as Lost",
            userContext.IpAddress,
            cancellationToken);

        return ApiResponse<bool>.Ok(true, "Opportunity marked as Lost successfully.");
    }
}

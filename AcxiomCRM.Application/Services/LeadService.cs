using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Leads;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Domain.Interfaces;
using AcxiomCRM.Domain.Rules;

namespace AcxiomCRM.Application.Services;

public class LeadService : ILeadService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public LeadService(IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    private IQueryable<Lead> ApplyAuthorization(IQueryable<Lead> query, UserContext userContext)
    {
        if (userContext.IsAdmin || userContext.IsManager)
        {
            return query;
        }

        return query.Where(l => l.AssignedTo == userContext.UserId);
    }

    public async Task<bool> CanUserAccessLeadAsync(int leadId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (userContext.IsAdmin || userContext.IsManager) return true;
        return await _unitOfWork.Leads.ExistsAsync(l => l.LeadId == leadId && l.AssignedTo == userContext.UserId, cancellationToken);
    }

    public async Task<PagedResult<LeadDto>> GetLeadsAsync(LeadFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default)
    {
        var query = ApplyAuthorization(_unitOfWork.Leads.Query(), userContext);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(l => l.LeadName.ToLower().Contains(term)
                                  || l.Email.ToLower().Contains(term)
                                  || l.Phone.ToLower().Contains(term)
                                  || (l.CompanyName != null && l.CompanyName.ToLower().Contains(term))
                                  || l.LeadCode.ToLower().Contains(term));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(l => l.Status == filter.Status.Value);
        }

        if (filter.Source.HasValue)
        {
            query = query.Where(l => l.Source == filter.Source.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(l => l.Priority == filter.Priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
        {
            query = query.Where(l => l.AssignedTo == filter.AssignedTo);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDescending ? query.OrderByDescending(l => l.LeadName) : query.OrderBy(l => l.LeadName),
            "company" => filter.SortDescending ? query.OrderByDescending(l => l.CompanyName) : query.OrderBy(l => l.CompanyName),
            "status" => filter.SortDescending ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status),
            "value" => filter.SortDescending ? query.OrderByDescending(l => l.ExpectedValue) : query.OrderBy(l => l.ExpectedValue),
            "createddate" => filter.SortDescending ? query.OrderByDescending(l => l.CreatedDate) : query.OrderBy(l => l.CreatedDate),
            _ => query.OrderByDescending(l => l.CreatedDate)
        };

        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedTo = l.AssignedTo,
                AssignedUserName = l.AssignedUser != null ? l.AssignedUser.FullName : "Unassigned",
                ConvertedCustomerId = l.ConvertedCustomerId,
                ConvertedCustomerName = l.ConvertedCustomer != null ? l.ConvertedCustomer.CustomerName : null,
                ConvertedDate = l.ConvertedDate,
                Notes = l.Notes
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LeadDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<LeadDetailDto?> GetLeadDetailsAsync(int leadId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessLeadAsync(leadId, userContext, cancellationToken))
        {
            return null;
        }

        var lead = await _unitOfWork.Leads.GetWithDetailsAsync(leadId, cancellationToken);
        if (lead == null) return null;

        return new LeadDetailDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            CreatedDate = lead.CreatedDate,
            AssignedTo = lead.AssignedTo,
            AssignedUserName = lead.AssignedUser?.FullName ?? "Unassigned",
            ConvertedCustomerId = lead.ConvertedCustomerId,
            ConvertedCustomerName = lead.ConvertedCustomer?.CustomerName,
            ConvertedDate = lead.ConvertedDate,
            Notes = lead.Notes,
            Opportunities = lead.Opportunities.Select(o => new DTOs.Customers.OpportunitySummaryDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status
            }).ToList(),
            FollowUps = lead.FollowUps.Select(f => new DTOs.Customers.FollowUpSummaryDto
            {
                FollowUpId = f.FollowUpId,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status
            }).ToList(),
            Activities = lead.Activities.Select(a => new DTOs.Customers.ActivitySummaryDto
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                ActivityDate = a.ActivityDate,
                Status = a.Status
            }).ToList()
        };
    }

    public async Task<LeadDto?> GetByIdAsync(int leadId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessLeadAsync(leadId, userContext, cancellationToken))
        {
            return null;
        }

        var lead = await _unitOfWork.Leads.GetByIdAsync(leadId, cancellationToken);
        if (lead == null) return null;

        return new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            CreatedDate = lead.CreatedDate,
            AssignedTo = lead.AssignedTo,
            ConvertedCustomerId = lead.ConvertedCustomerId,
            ConvertedDate = lead.ConvertedDate,
            Notes = lead.Notes
        };
    }

    public async Task<ApiResponse<LeadDto>> CreateLeadAsync(CreateLeadDto dto, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.LeadName))
        {
            return ApiResponse<LeadDto>.Fail("Lead Name is required.");
        }

        if (!CustomerRules.IsValidEmail(dto.Email))
        {
            return ApiResponse<LeadDto>.Fail(CustomerRules.InvalidEmailError);
        }

        if (!CustomerRules.IsValidPhone(dto.Phone))
        {
            return ApiResponse<LeadDto>.Fail(CustomerRules.InvalidPhoneError);
        }

        if (dto.ExpectedValue < 0)
        {
            return ApiResponse<LeadDto>.Fail("Expected value must be a positive number.");
        }

        var assignedTo = userContext.IsSalesExecutive ? userContext.UserId : (dto.AssignedTo ?? userContext.UserId);
        var code = await _unitOfWork.Leads.GenerateLeadCodeAsync(cancellationToken);

        var lead = new Lead
        {
            LeadCode = code,
            LeadName = dto.LeadName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Source = dto.Source,
            Status = dto.Status,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            CreatedDate = DateTime.UtcNow,
            AssignedTo = assignedTo,
            Notes = dto.Notes?.Trim()
        };

        await _unitOfWork.Leads.AddAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Create",
            "Lead",
            lead.LeadId.ToString(),
            null,
            $"Created Lead '{lead.LeadName}' ({lead.LeadCode})",
            userContext.IpAddress,
            cancellationToken);

        var result = new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            CreatedDate = lead.CreatedDate,
            AssignedTo = lead.AssignedTo,
            Notes = lead.Notes
        };

        return ApiResponse<LeadDto>.Ok(result, "Lead created successfully.");
    }

    public async Task<ApiResponse<LeadDto>> UpdateLeadAsync(UpdateLeadDto dto, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessLeadAsync(dto.LeadId, userContext, cancellationToken))
        {
            return ApiResponse<LeadDto>.Fail("Unauthorized access to lead record.");
        }

        var lead = await _unitOfWork.Leads.GetByIdAsync(dto.LeadId, cancellationToken);
        if (lead == null)
        {
            return ApiResponse<LeadDto>.Fail("Lead not found.");
        }

        if (lead.Status == LeadStatus.Converted && dto.Status != LeadStatus.Converted)
        {
            return ApiResponse<LeadDto>.Fail("Converted leads cannot change status.");
        }

        if (string.IsNullOrWhiteSpace(dto.LeadName))
        {
            return ApiResponse<LeadDto>.Fail("Lead Name is required.");
        }

        if (!CustomerRules.IsValidEmail(dto.Email))
        {
            return ApiResponse<LeadDto>.Fail(CustomerRules.InvalidEmailError);
        }

        if (!CustomerRules.IsValidPhone(dto.Phone))
        {
            return ApiResponse<LeadDto>.Fail(CustomerRules.InvalidPhoneError);
        }

        if (dto.ExpectedValue < 0)
        {
            return ApiResponse<LeadDto>.Fail("Expected value must be a positive number.");
        }

        // Validate status transition
        var (isValidTransition, transitionError) = LeadStatusRules.ValidateTransition(lead.Status, dto.Status);
        if (!isValidTransition)
        {
            return ApiResponse<LeadDto>.Fail(transitionError!);
        }

        var oldSummary = $"Name: {lead.LeadName}, Status: {lead.Status}, Value: {lead.ExpectedValue}";

        lead.LeadName = dto.LeadName.Trim();
        lead.Email = dto.Email.Trim().ToLower();
        lead.Phone = dto.Phone.Trim();
        lead.CompanyName = dto.CompanyName?.Trim();
        lead.Source = dto.Source;
        lead.Status = dto.Status;
        lead.Priority = dto.Priority;
        lead.ExpectedValue = dto.ExpectedValue;
        lead.Notes = dto.Notes?.Trim();
        lead.ModifiedDate = DateTime.UtcNow;

        if (!userContext.IsSalesExecutive && !string.IsNullOrWhiteSpace(dto.AssignedTo))
        {
            lead.AssignedTo = dto.AssignedTo;
        }

        await _unitOfWork.Leads.UpdateAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var newSummary = $"Name: {lead.LeadName}, Status: {lead.Status}, Value: {lead.ExpectedValue}";

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Update",
            "Lead",
            lead.LeadId.ToString(),
            oldSummary,
            newSummary,
            userContext.IpAddress,
            cancellationToken);

        var result = new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            CreatedDate = lead.CreatedDate,
            AssignedTo = lead.AssignedTo,
            Notes = lead.Notes
        };

        return ApiResponse<LeadDto>.Ok(result, "Lead updated successfully.");
    }

    public async Task<ApiResponse<LeadDto>> UpdateStatusAsync(int leadId, LeadStatus newStatus, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessLeadAsync(leadId, userContext, cancellationToken))
        {
            return ApiResponse<LeadDto>.Fail("Unauthorized access to lead record.");
        }

        var lead = await _unitOfWork.Leads.GetByIdAsync(leadId, cancellationToken);
        if (lead == null)
        {
            return ApiResponse<LeadDto>.Fail("Lead not found.");
        }

        var (isValidTransition, transitionError) = LeadStatusRules.ValidateTransition(lead.Status, newStatus);
        if (!isValidTransition)
        {
            return ApiResponse<LeadDto>.Fail(transitionError!);
        }

        var oldStatus = lead.Status;
        lead.Status = newStatus;
        lead.ModifiedDate = DateTime.UtcNow;

        await _unitOfWork.Leads.UpdateAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "StatusChange",
            "Lead",
            lead.LeadId.ToString(),
            $"Status: {oldStatus}",
            $"Status: {newStatus}",
            userContext.IpAddress,
            cancellationToken);

        var result = new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            CreatedDate = lead.CreatedDate,
            AssignedTo = lead.AssignedTo
        };

        return ApiResponse<LeadDto>.Ok(result, $"Lead status updated to {newStatus}.");
    }

    public async Task<ApiResponse<int>> ConvertLeadAsync(ConvertLeadDto dto, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessLeadAsync(dto.LeadId, userContext, cancellationToken))
        {
            return ApiResponse<int>.Fail("Unauthorized access to lead record.");
        }

        var lead = await _unitOfWork.Leads.GetByIdAsync(dto.LeadId, cancellationToken);
        if (lead == null)
        {
            return ApiResponse<int>.Fail("Lead not found.");
        }

        if (lead.Status == LeadStatus.Converted)
        {
            return ApiResponse<int>.Fail("Lead is already converted.");
        }

        // 1. Check or create Customer
        Customer? customer = null;
        var existingCustomers = await _unitOfWork.Customers.FindAsync(c => c.Email.ToLower() == lead.Email.ToLower(), cancellationToken);
        if (existingCustomers.Any())
        {
            customer = existingCustomers.First();
        }
        else
        {
            var custCode = await _unitOfWork.Customers.GenerateCustomerCodeAsync(cancellationToken);
            customer = new Customer
            {
                CustomerCode = custCode,
                CustomerName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userContext.UserName,
                OwnerId = lead.AssignedTo ?? userContext.UserId
            };

            await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                userContext.UserId,
                userContext.UserName,
                "Create",
                "Customer",
                customer.CustomerId.ToString(),
                null,
                $"Created Customer '{customer.CustomerName}' from Lead Conversion ({lead.LeadCode})",
                userContext.IpAddress,
                cancellationToken);
        }

        // 2. Optional Opportunity creation
        if (dto.CreateOpportunity)
        {
            var oppName = !string.IsNullOrWhiteSpace(dto.OpportunityName) ? dto.OpportunityName.Trim() : $"{customer.CustomerName} - New Business";
            var oppAmount = dto.OpportunityAmount ?? (lead.ExpectedValue > 0 ? lead.ExpectedValue : 1000m);
            var prob = dto.Probability ?? 20;
            var closeDate = dto.ExpectedCloseDate ?? DateTime.UtcNow.AddDays(30);

            var (oppValid, oppError) = OpportunityRules.Validate(oppAmount, prob, closeDate, OpportunityStatus.Open);
            if (!oppValid)
            {
                return ApiResponse<int>.Fail(oppError!);
            }

            var opp = new Opportunity
            {
                OpportunityName = oppName,
                CustomerId = customer.CustomerId,
                LeadId = lead.LeadId,
                Amount = oppAmount,
                Stage = dto.OpportunityStage ?? OpportunityStage.Qualification,
                Probability = prob,
                ExpectedCloseDate = closeDate,
                Status = OpportunityStatus.Open,
                CreatedDate = DateTime.UtcNow,
                AssignedTo = lead.AssignedTo ?? userContext.UserId,
                Notes = $"Generated from conversion of Lead {lead.LeadCode}"
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
                $"Created Opportunity '{opp.OpportunityName}' with amount ${opp.Amount} from Lead {lead.LeadCode}",
                userContext.IpAddress,
                cancellationToken);
        }

        // 3. Mark Lead as Converted
        lead.Status = LeadStatus.Converted;
        lead.ConvertedCustomerId = customer.CustomerId;
        lead.ConvertedDate = DateTime.UtcNow;
        lead.ModifiedDate = DateTime.UtcNow;

        await _unitOfWork.Leads.UpdateAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "LeadConversion",
            "Lead",
            lead.LeadId.ToString(),
            $"Status: Qualified/Active",
            $"Lead {lead.LeadCode} converted into Customer {customer.CustomerCode}",
            userContext.IpAddress,
            cancellationToken);

        return ApiResponse<int>.Ok(customer.CustomerId, "Lead converted to Customer successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteLeadAsync(int leadId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessLeadAsync(leadId, userContext, cancellationToken))
        {
            return ApiResponse<bool>.Fail("Unauthorized access to lead record.");
        }

        var lead = await _unitOfWork.Leads.GetByIdAsync(leadId, cancellationToken);
        if (lead == null)
        {
            return ApiResponse<bool>.Fail("Lead not found.");
        }

        lead.Status = LeadStatus.Lost;
        lead.ModifiedDate = DateTime.UtcNow;

        await _unitOfWork.Leads.UpdateAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Deactivate",
            "Lead",
            leadId.ToString(),
            $"Lead '{lead.LeadName}'",
            $"Lead marked as Lost (Deactivated)",
            userContext.IpAddress,
            cancellationToken);

        return ApiResponse<bool>.Ok(true, "Lead marked as Lost successfully.");
    }
}

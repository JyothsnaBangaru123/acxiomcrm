using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Domain.Interfaces;
using AcxiomCRM.Domain.Rules;

namespace AcxiomCRM.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public CustomerService(IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    private IQueryable<Customer> ApplyAuthorization(IQueryable<Customer> query, UserContext userContext)
    {
        if (userContext.IsAdmin || userContext.IsManager)
        {
            return query;
        }

        // SalesExecutive: only their own assigned records
        return query.Where(c => c.OwnerId == userContext.UserId);
    }

    public async Task<bool> CanUserAccessCustomerAsync(int customerId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (userContext.IsAdmin || userContext.IsManager) return true;
        return await _unitOfWork.Customers.ExistsAsync(c => c.CustomerId == customerId && c.OwnerId == userContext.UserId, cancellationToken);
    }

    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(CustomerFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default)
    {
        var query = ApplyAuthorization(_unitOfWork.Customers.Query(), userContext);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(c => c.CustomerName.ToLower().Contains(term)
                                  || c.Email.ToLower().Contains(term)
                                  || c.Phone.ToLower().Contains(term)
                                  || (c.CompanyName != null && c.CompanyName.ToLower().Contains(term))
                                  || c.CustomerCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            query = query.Where(c => c.City != null && c.City.ToLower() == filter.City.Trim().ToLower());
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(c => c.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.OwnerId))
        {
            query = query.Where(c => c.OwnerId == filter.OwnerId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDescending ? query.OrderByDescending(c => c.CustomerName) : query.OrderBy(c => c.CustomerName),
            "company" => filter.SortDescending ? query.OrderByDescending(c => c.CompanyName) : query.OrderBy(c => c.CompanyName),
            "email" => filter.SortDescending ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            "createddate" => filter.SortDescending ? query.OrderByDescending(c => c.CreatedDate) : query.OrderBy(c => c.CreatedDate),
            _ => query.OrderByDescending(c => c.CreatedDate)
        };

        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                CreatedBy = c.CreatedBy,
                OwnerId = c.OwnerId,
                OwnerName = c.Owner != null ? c.Owner.FullName : "Unassigned",
                OpportunitiesCount = c.Opportunities.Count,
                TotalPipelineAmount = c.Opportunities.Where(o => o.Status == OpportunityStatus.Open).Sum(o => o.Amount)
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<CustomerDetailDto?> GetCustomerDetailsAsync(int customerId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessCustomerAsync(customerId, userContext, cancellationToken))
        {
            return null;
        }

        var customer = await _unitOfWork.Customers.GetWithDetailsAsync(customerId, cancellationToken);
        if (customer == null) return null;

        return new CustomerDetailDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate,
            CreatedBy = customer.CreatedBy,
            OwnerId = customer.OwnerId,
            OwnerName = customer.Owner?.FullName ?? "Unassigned",
            OpportunitiesCount = customer.Opportunities.Count,
            TotalPipelineAmount = customer.Opportunities.Where(o => o.Status == OpportunityStatus.Open).Sum(o => o.Amount),
            Leads = customer.Leads.Select(l => new LeadSummaryDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Status = l.Status,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate
            }).ToList(),
            Opportunities = customer.Opportunities.Select(o => new OpportunitySummaryDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status
            }).ToList(),
            FollowUps = customer.FollowUps.Select(f => new FollowUpSummaryDto
            {
                FollowUpId = f.FollowUpId,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status
            }).ToList(),
            Activities = customer.Activities.Select(a => new ActivitySummaryDto
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                ActivityDate = a.ActivityDate,
                Status = a.Status
            }).ToList()
        };
    }

    public async Task<CustomerDto?> GetByIdAsync(int customerId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessCustomerAsync(customerId, userContext, cancellationToken))
        {
            return null;
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken);
        if (customer == null) return null;

        return new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate,
            CreatedBy = customer.CreatedBy,
            OwnerId = customer.OwnerId
        };
    }

    public async Task<ApiResponse<CustomerDto>> CreateCustomerAsync(CreateCustomerDto dto, UserContext userContext, CancellationToken cancellationToken = default)
    {
        // 1. Validate mandatory fields & formats
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            return ApiResponse<CustomerDto>.Fail("Customer Name is required.");
        }

        if (!CustomerRules.IsValidEmail(dto.Email))
        {
            return ApiResponse<CustomerDto>.Fail(CustomerRules.InvalidEmailError);
        }

        if (!CustomerRules.IsValidPhone(dto.Phone))
        {
            return ApiResponse<CustomerDto>.Fail(CustomerRules.InvalidPhoneError);
        }

        // 2. Validate email uniqueness
        if (!await _unitOfWork.Customers.IsEmailUniqueAsync(dto.Email, null, cancellationToken))
        {
            return ApiResponse<CustomerDto>.Fail(CustomerRules.DuplicateEmailError);
        }

        // 3. Validate phone uniqueness
        if (!await _unitOfWork.Customers.IsPhoneUniqueAsync(dto.Phone, null, cancellationToken))
        {
            return ApiResponse<CustomerDto>.Fail(CustomerRules.DuplicatePhoneError);
        }

        // 4. Duplicate business checking (same company name and email)
        if (!string.IsNullOrWhiteSpace(dto.CompanyName))
        {
            var exists = await _unitOfWork.Customers.ExistsAsync(c => c.CompanyName != null && c.CompanyName.ToLower() == dto.CompanyName.Trim().ToLower() && c.Email.ToLower() == dto.Email.Trim().ToLower(), cancellationToken);
            if (exists)
            {
                return ApiResponse<CustomerDto>.Fail(CustomerRules.DuplicateCustomerError);
            }
        }

        // If SalesExecutive is creating, owner is automatically themselves unless overridden by Admin/Manager
        var ownerId = userContext.IsSalesExecutive ? userContext.UserId : (dto.OwnerId ?? userContext.UserId);
        var code = await _unitOfWork.Customers.GenerateCustomerCodeAsync(cancellationToken);

        var customer = new Customer
        {
            CustomerCode = code,
            CustomerName = dto.CustomerName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Address = dto.Address?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            Status = dto.Status,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = userContext.UserName,
            OwnerId = ownerId
        };

        await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Audit Log
        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Create",
            "Customer",
            customer.CustomerId.ToString(),
            null,
            $"Created Customer '{customer.CustomerName}' ({customer.CustomerCode})",
            userContext.IpAddress,
            cancellationToken);

        var resultDto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate,
            CreatedBy = customer.CreatedBy,
            OwnerId = customer.OwnerId
        };

        return ApiResponse<CustomerDto>.Ok(resultDto, "Customer created successfully.");
    }

    public async Task<ApiResponse<CustomerDto>> UpdateCustomerAsync(UpdateCustomerDto dto, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessCustomerAsync(dto.CustomerId, userContext, cancellationToken))
        {
            return ApiResponse<CustomerDto>.Fail("Unauthorized access to customer record.");
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(dto.CustomerId, cancellationToken);
        if (customer == null)
        {
            return ApiResponse<CustomerDto>.Fail("Customer not found.");
        }

        if (string.IsNullOrWhiteSpace(dto.CustomerName))
        {
            return ApiResponse<CustomerDto>.Fail("Customer Name is required.");
        }

        if (!CustomerRules.IsValidEmail(dto.Email))
        {
            return ApiResponse<CustomerDto>.Fail(CustomerRules.InvalidEmailError);
        }

        if (!CustomerRules.IsValidPhone(dto.Phone))
        {
            return ApiResponse<CustomerDto>.Fail(CustomerRules.InvalidPhoneError);
        }

        if (!await _unitOfWork.Customers.IsEmailUniqueAsync(dto.Email, dto.CustomerId, cancellationToken))
        {
            return ApiResponse<CustomerDto>.Fail(CustomerRules.DuplicateEmailError);
        }

        if (!await _unitOfWork.Customers.IsPhoneUniqueAsync(dto.Phone, dto.CustomerId, cancellationToken))
        {
            return ApiResponse<CustomerDto>.Fail(CustomerRules.DuplicatePhoneError);
        }

        var oldSummary = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";

        customer.CustomerName = dto.CustomerName.Trim();
        customer.Email = dto.Email.Trim().ToLower();
        customer.Phone = dto.Phone.Trim();
        customer.CompanyName = dto.CompanyName?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.City = dto.City?.Trim();
        customer.State = dto.State?.Trim();
        customer.Status = dto.Status;
        customer.ModifiedDate = DateTime.UtcNow;

        if (!userContext.IsSalesExecutive && !string.IsNullOrWhiteSpace(dto.OwnerId))
        {
            customer.OwnerId = dto.OwnerId;
        }

        await _unitOfWork.Customers.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var newSummary = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Update",
            "Customer",
            customer.CustomerId.ToString(),
            oldSummary,
            newSummary,
            userContext.IpAddress,
            cancellationToken);

        var resultDto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate,
            CreatedBy = customer.CreatedBy,
            OwnerId = customer.OwnerId
        };

        return ApiResponse<CustomerDto>.Ok(resultDto, "Customer updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteCustomerAsync(int customerId, UserContext userContext, CancellationToken cancellationToken = default)
    {
        if (!await CanUserAccessCustomerAsync(customerId, userContext, cancellationToken))
        {
            return ApiResponse<bool>.Fail("Unauthorized access to customer record.");
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
        {
            return ApiResponse<bool>.Fail("Customer not found.");
        }

        // Deactivate or remove
        customer.Status = CustomerStatus.Inactive;
        customer.ModifiedDate = DateTime.UtcNow;

        await _unitOfWork.Customers.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            userContext.UserId,
            userContext.UserName,
            "Deactivate",
            "Customer",
            customerId.ToString(),
            $"Customer '{customer.CustomerName}' status Active",
            $"Customer '{customer.CustomerName}' status Inactive (Deactivated)",
            userContext.IpAddress,
            cancellationToken);

        return ApiResponse<bool>.Ok(true, "Customer deactivated successfully.");
    }
}

using AcxiomCRM.Application.DTOs.Auth;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.DTOs.Leads;
using AcxiomCRM.Application.DTOs.Opportunities;
using AcxiomCRM.Application.DTOs.FollowUps;
using AcxiomCRM.Application.DTOs.Activities;
using AcxiomCRM.Application.DTOs.Audit;
using AcxiomCRM.Application.DTOs.Dashboard;
using AcxiomCRM.Application.DTOs.Reports;
using AcxiomCRM.Application.DTOs.Common;

namespace AcxiomCRM.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string? userId, string? userName, string action, string entityName, string? recordId, string? oldValue = null, string? newValue = null, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogFilterDto filter, CancellationToken cancellationToken = default);
}

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(CustomerFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default);
    Task<CustomerDetailDto?> GetCustomerDetailsAsync(int customerId, UserContext userContext, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByIdAsync(int customerId, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerDto>> CreateCustomerAsync(CreateCustomerDto dto, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerDto>> UpdateCustomerAsync(UpdateCustomerDto dto, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteCustomerAsync(int customerId, UserContext userContext, CancellationToken cancellationToken = default);
    Task<bool> CanUserAccessCustomerAsync(int customerId, UserContext userContext, CancellationToken cancellationToken = default);
}

public interface ILeadService
{
    Task<PagedResult<LeadDto>> GetLeadsAsync(LeadFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default);
    Task<LeadDetailDto?> GetLeadDetailsAsync(int leadId, UserContext userContext, CancellationToken cancellationToken = default);
    Task<LeadDto?> GetByIdAsync(int leadId, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<LeadDto>> CreateLeadAsync(CreateLeadDto dto, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<LeadDto>> UpdateLeadAsync(UpdateLeadDto dto, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<LeadDto>> UpdateStatusAsync(int leadId, Domain.Enums.LeadStatus newStatus, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> ConvertLeadAsync(ConvertLeadDto dto, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteLeadAsync(int leadId, UserContext userContext, CancellationToken cancellationToken = default);
    Task<bool> CanUserAccessLeadAsync(int leadId, UserContext userContext, CancellationToken cancellationToken = default);
}

public interface IOpportunityService
{
    Task<PagedResult<OpportunityDto>> GetOpportunitiesAsync(OpportunityFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default);
    Task<OpportunityDetailDto?> GetOpportunityDetailsAsync(int opportunityId, UserContext userContext, CancellationToken cancellationToken = default);
    Task<OpportunityDto?> GetByIdAsync(int opportunityId, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<OpportunityDto>> CreateOpportunityAsync(CreateOpportunityDto dto, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<OpportunityDto>> UpdateOpportunityAsync(UpdateOpportunityDto dto, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteOpportunityAsync(int opportunityId, UserContext userContext, CancellationToken cancellationToken = default);
    Task<bool> CanUserAccessOpportunityAsync(int opportunityId, UserContext userContext, CancellationToken cancellationToken = default);
}

public interface IFollowUpService
{
    Task<PagedResult<FollowUpDto>> GetFollowUpsAsync(FollowUpFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default);
    Task<FollowUpDto?> GetByIdAsync(int followUpId, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<FollowUpDto>> CreateFollowUpAsync(CreateFollowUpDto dto, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<FollowUpDto>> UpdateFollowUpAsync(UpdateFollowUpDto dto, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> RescheduleFollowUpAsync(RescheduleFollowUpDto dto, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> CompleteFollowUpAsync(CompleteFollowUpDto dto, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> CancelFollowUpAsync(int followUpId, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteFollowUpAsync(int followUpId, UserContext userContext, CancellationToken cancellationToken = default);
}

public interface IActivityService
{
    Task<PagedResult<ActivityDto>> GetActivitiesAsync(ActivityFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<ActivityDto>> CreateActivityAsync(CreateActivityDto dto, UserContext userContext, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityDto>> GetRecentActivitiesForRecordAsync(int? customerId, int? leadId, UserContext userContext, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardViewModelDto> GetDashboardDataAsync(UserContext userContext, string dateFilter = "All", CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<IReadOnlyList<PipelineStageReportItemDto>> GetPipelineStageReportAsync(UserContext userContext, DateTime? start, DateTime? end, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OwnerPipelineReportItemDto>> GetOwnerPipelineReportAsync(UserContext userContext, DateTime? start, DateTime? end, CancellationToken cancellationToken = default);
    Task<ConversionRateReportDto> GetConversionReportAsync(UserContext userContext, DateTime? start, DateTime? end, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserActivityReportItemDto>> GetUserActivityReportAsync(UserContext userContext, DateTime? start, DateTime? end, CancellationToken cancellationToken = default);
    Task<PagedResult<CustomerDto>> GetCustomerReportAsync(CustomerFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default);
    Task<PagedResult<LeadDto>> GetLeadReportAsync(LeadFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default);
    Task<PagedResult<FollowUpDto>> GetFollowUpReportAsync(FollowUpFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default);
    Task<PagedResult<OpportunityDto>> GetOpportunityReportAsync(OpportunityFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default);
}

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> UpdateUserRoleAsync(UpdateUserRoleDto dto, UserContext currentUserContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ToggleUserActiveStatusAsync(string userId, UserContext currentUserContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ResetUserPasswordAsync(ResetUserPasswordDto dto, UserContext currentUserContext, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> UnlockUserAccountAsync(string userId, UserContext currentUserContext, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetSalesExecutivesAsync(CancellationToken cancellationToken = default);
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.DTOs.FollowUps;
using AcxiomCRM.Application.DTOs.Leads;
using AcxiomCRM.Application.DTOs.Opportunities;
using AcxiomCRM.Application.DTOs.Reports;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Domain.Interfaces;

namespace AcxiomCRM.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly IOpportunityService _opportunityService;
    private readonly IFollowUpService _followUpService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportService(
        IUnitOfWork unitOfWork,
        ICustomerService customerService,
        ILeadService leadService,
        IOpportunityService opportunityService,
        IFollowUpService followUpService,
        UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _customerService = customerService;
        _leadService = leadService;
        _opportunityService = opportunityService;
        _followUpService = followUpService;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<PipelineStageReportItemDto>> GetPipelineStageReportAsync(UserContext userContext, DateTime? start, DateTime? end, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Opportunities.Query();

        if (userContext.IsSalesExecutive)
        {
            query = query.Where(o => o.AssignedTo == userContext.UserId);
        }

        if (start.HasValue) query = query.Where(o => o.CreatedDate >= start.Value);
        if (end.HasValue) query = query.Where(o => o.CreatedDate <= end.Value);

        var list = await query.ToListAsync(cancellationToken);

        var stages = Enum.GetValues<OpportunityStage>();
        return stages.Select(stage =>
        {
            var oppsInStage = list.Where(o => o.Stage == stage).ToList();
            return new PipelineStageReportItemDto
            {
                Stage = stage,
                OpportunityCount = oppsInStage.Count,
                TotalAmount = oppsInStage.Sum(o => o.Amount),
                WeightedAmount = Math.Round(oppsInStage.Sum(o => o.Amount * o.Probability / 100m), 2)
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<OwnerPipelineReportItemDto>> GetOwnerPipelineReportAsync(UserContext userContext, DateTime? start, DateTime? end, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Opportunities.Query();

        if (userContext.IsSalesExecutive)
        {
            query = query.Where(o => o.AssignedTo == userContext.UserId);
        }

        if (start.HasValue) query = query.Where(o => o.CreatedDate >= start.Value);
        if (end.HasValue) query = query.Where(o => o.CreatedDate <= end.Value);

        var opps = await query.Include(o => o.AssignedUser).ToListAsync(cancellationToken);

        return opps
            .GroupBy(o => o.AssignedTo ?? "unassigned")
            .Select(g =>
            {
                var user = g.FirstOrDefault()?.AssignedUser;
                var active = g.Where(o => o.Status == OpportunityStatus.Open).ToList();
                var won = g.Where(o => o.Status == OpportunityStatus.Won).ToList();

                return new OwnerPipelineReportItemDto
                {
                    OwnerId = g.Key,
                    OwnerName = user?.FullName ?? "Unassigned",
                    ActiveOpportunitiesCount = active.Count,
                    TotalPipelineValue = active.Sum(o => o.Amount),
                    WonCount = won.Count,
                    WonAmount = won.Sum(o => o.Amount)
                };
            })
            .OrderByDescending(r => r.TotalPipelineValue)
            .ToList();
    }

    public async Task<ConversionRateReportDto> GetConversionReportAsync(UserContext userContext, DateTime? start, DateTime? end, CancellationToken cancellationToken = default)
    {
        var leadQuery = _unitOfWork.Leads.Query();
        var oppQuery = _unitOfWork.Opportunities.Query();

        if (userContext.IsSalesExecutive)
        {
            leadQuery = leadQuery.Where(l => l.AssignedTo == userContext.UserId);
            oppQuery = oppQuery.Where(o => o.AssignedTo == userContext.UserId);
        }

        if (start.HasValue)
        {
            leadQuery = leadQuery.Where(l => l.CreatedDate >= start.Value);
            oppQuery = oppQuery.Where(o => o.CreatedDate >= start.Value);
        }

        if (end.HasValue)
        {
            leadQuery = leadQuery.Where(l => l.CreatedDate <= end.Value);
            oppQuery = oppQuery.Where(o => o.CreatedDate <= end.Value);
        }

        var totalLeads = await leadQuery.CountAsync(cancellationToken);
        var qualifiedLeads = await leadQuery.CountAsync(l => l.Status == LeadStatus.Qualified || l.Status == LeadStatus.Converted, cancellationToken);
        var convertedLeads = await leadQuery.CountAsync(l => l.Status == LeadStatus.Converted, cancellationToken);
        var lostLeads = await leadQuery.CountAsync(l => l.Status == LeadStatus.Lost, cancellationToken);
        var wonRevenue = await oppQuery.Where(o => o.Status == OpportunityStatus.Won).SumAsync(o => (decimal?)o.Amount, cancellationToken) ?? 0m;

        return new ConversionRateReportDto
        {
            TotalLeads = totalLeads,
            QualifiedLeads = qualifiedLeads,
            ConvertedLeads = convertedLeads,
            LostLeads = lostLeads,
            TotalWonRevenue = wonRevenue
        };
    }

    public async Task<IReadOnlyList<UserActivityReportItemDto>> GetUserActivityReportAsync(UserContext userContext, DateTime? start, DateTime? end, CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.ToListAsync(cancellationToken);

        if (userContext.IsSalesExecutive)
        {
            users = users.Where(u => u.Id == userContext.UserId).ToList();
        }

        var actQuery = _unitOfWork.Activities.Query();
        var fuQuery = _unitOfWork.FollowUps.Query();

        if (start.HasValue)
        {
            actQuery = actQuery.Where(a => a.ActivityDate >= start.Value);
            fuQuery = fuQuery.Where(f => f.FollowUpDate >= start.Value);
        }

        if (end.HasValue)
        {
            actQuery = actQuery.Where(a => a.ActivityDate <= end.Value);
            fuQuery = fuQuery.Where(f => f.FollowUpDate <= end.Value);
        }

        var activities = await actQuery.ToListAsync(cancellationToken);
        var followUps = await fuQuery.ToListAsync(cancellationToken);

        var report = new List<UserActivityReportItemDto>();

        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var userActs = activities.Where(a => a.AssignedTo == user.Id).ToList();
            var userFus = followUps.Where(f => f.AssignedTo == user.Id).ToList();

            report.Add(new UserActivityReportItemDto
            {
                UserId = user.Id,
                UserName = user.FullName,
                Role = userRoles.FirstOrDefault() ?? "None",
                TotalCalls = userActs.Count(a => a.ActivityType == ActivityType.Call),
                TotalMeetings = userActs.Count(a => a.ActivityType == ActivityType.Meeting),
                TotalEmails = userActs.Count(a => a.ActivityType == ActivityType.Email),
                TotalTasks = userActs.Count(a => a.ActivityType == ActivityType.Task),
                CompletedFollowUps = userFus.Count(f => f.Status == FollowUpStatus.Completed),
                PendingFollowUps = userFus.Count(f => f.Status == FollowUpStatus.Planned)
            });
        }

        return report.OrderByDescending(r => r.TotalActivitiesCount).ToList();
    }

    public Task<PagedResult<CustomerDto>> GetCustomerReportAsync(CustomerFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default)
    {
        return _customerService.GetCustomersAsync(filter, userContext, cancellationToken);
    }

    public Task<PagedResult<LeadDto>> GetLeadReportAsync(LeadFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default)
    {
        return _leadService.GetLeadsAsync(filter, userContext, cancellationToken);
    }

    public Task<PagedResult<FollowUpDto>> GetFollowUpReportAsync(FollowUpFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default)
    {
        return _followUpService.GetFollowUpsAsync(filter, userContext, cancellationToken);
    }

    public Task<PagedResult<OpportunityDto>> GetOpportunityReportAsync(OpportunityFilterDto filter, UserContext userContext, CancellationToken cancellationToken = default)
    {
        return _opportunityService.GetOpportunitiesAsync(filter, userContext, cancellationToken);
    }
}

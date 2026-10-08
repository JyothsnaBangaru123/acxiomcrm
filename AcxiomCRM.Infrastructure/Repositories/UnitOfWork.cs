using AcxiomCRM.Domain.Interfaces;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AcxiomCrmDbContext _context;
    private ICustomerRepository? _customers;
    private ILeadRepository? _leads;
    private IOpportunityRepository? _opportunities;
    private IFollowUpRepository? _followUps;
    private IActivityRepository? _activities;
    private IAuditLogRepository? _auditLogs;

    public UnitOfWork(AcxiomCrmDbContext context)
    {
        _context = context;
    }

    public ICustomerRepository Customers => _customers ??= new CustomerRepository(_context);
    public ILeadRepository Leads => _leads ??= new LeadRepository(_context);
    public IOpportunityRepository Opportunities => _opportunities ??= new OpportunityRepository(_context);
    public IFollowUpRepository FollowUps => _followUps ??= new FollowUpRepository(_context);
    public IActivityRepository Activities => _activities ??= new ActivityRepository(_context);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}

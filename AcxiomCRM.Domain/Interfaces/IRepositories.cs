using System.Linq.Expressions;
using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    IQueryable<T> Query();
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
}

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetWithDetailsAsync(int customerId, CancellationToken cancellationToken = default);
    Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null, CancellationToken cancellationToken = default);
    Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null, CancellationToken cancellationToken = default);
    Task<string> GenerateCustomerCodeAsync(CancellationToken cancellationToken = default);
}

public interface ILeadRepository : IRepository<Lead>
{
    Task<Lead?> GetWithDetailsAsync(int leadId, CancellationToken cancellationToken = default);
    Task<string> GenerateLeadCodeAsync(CancellationToken cancellationToken = default);
}

public interface IOpportunityRepository : IRepository<Opportunity>
{
    Task<Opportunity?> GetWithDetailsAsync(int opportunityId, CancellationToken cancellationToken = default);
}

public interface IFollowUpRepository : IRepository<FollowUp>
{
    Task<FollowUp?> GetWithDetailsAsync(int followUpId, CancellationToken cancellationToken = default);
}

public interface IActivityRepository : IRepository<Activity>
{
    Task<Activity?> GetWithDetailsAsync(int activityId, CancellationToken cancellationToken = default);
}

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> GetRecentLogsAsync(int count = 50, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork : IDisposable
{
    ICustomerRepository Customers { get; }
    ILeadRepository Leads { get; }
    IOpportunityRepository Opportunities { get; }
    IFollowUpRepository FollowUps { get; }
    IActivityRepository Activities { get; }
    IAuditLogRepository AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

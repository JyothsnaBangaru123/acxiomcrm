using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Interfaces;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Infrastructure.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AcxiomCrmDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentLogsAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .OrderByDescending(a => a.CreatedDate)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}

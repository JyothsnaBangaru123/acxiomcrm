using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Interfaces;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Infrastructure.Repositories;

public class FollowUpRepository : Repository<FollowUp>, IFollowUpRepository
{
    public FollowUpRepository(AcxiomCrmDbContext context) : base(context)
    {
    }

    public async Task<FollowUp?> GetWithDetailsAsync(int followUpId, CancellationToken cancellationToken = default)
    {
        return await _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedUser)
            .FirstOrDefaultAsync(f => f.FollowUpId == followUpId, cancellationToken);
    }
}

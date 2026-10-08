using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Interfaces;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Infrastructure.Repositories;

public class OpportunityRepository : Repository<Opportunity>, IOpportunityRepository
{
    public OpportunityRepository(AcxiomCrmDbContext context) : base(context)
    {
    }

    public async Task<Opportunity?> GetWithDetailsAsync(int opportunityId, CancellationToken cancellationToken = default)
    {
        return await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedUser)
            .Include(o => o.FollowUps)
            .FirstOrDefaultAsync(o => o.OpportunityId == opportunityId, cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Interfaces;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Infrastructure.Repositories;

public class LeadRepository : Repository<Lead>, ILeadRepository
{
    public LeadRepository(AcxiomCrmDbContext context) : base(context)
    {
    }

    public async Task<Lead?> GetWithDetailsAsync(int leadId, CancellationToken cancellationToken = default)
    {
        return await _context.Leads
            .Include(l => l.AssignedUser)
            .Include(l => l.ConvertedCustomer)
            .Include(l => l.Opportunities)
            .Include(l => l.FollowUps)
            .Include(l => l.Activities)
            .FirstOrDefaultAsync(l => l.LeadId == leadId, cancellationToken);
    }

    public async Task<string> GenerateLeadCodeAsync(CancellationToken cancellationToken = default)
    {
        var count = await _context.Leads.CountAsync(cancellationToken);
        return $"LEAD-{(count + 1):D4}";
    }
}

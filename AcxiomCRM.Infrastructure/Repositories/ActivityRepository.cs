using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Interfaces;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Infrastructure.Repositories;

public class ActivityRepository : Repository<Activity>, IActivityRepository
{
    public ActivityRepository(AcxiomCrmDbContext context) : base(context)
    {
    }

    public async Task<Activity?> GetWithDetailsAsync(int activityId, CancellationToken cancellationToken = default)
    {
        return await _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedUser)
            .FirstOrDefaultAsync(a => a.ActivityId == activityId, cancellationToken);
    }
}

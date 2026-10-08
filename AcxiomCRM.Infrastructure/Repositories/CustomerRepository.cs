using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Interfaces;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Infrastructure.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(AcxiomCrmDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetWithDetailsAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .Include(c => c.Owner)
            .Include(c => c.Leads)
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsQueryable();
        if (excludeCustomerId.HasValue)
        {
            query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
        }
        return !await query.AnyAsync(c => c.Email.ToLower() == email.Trim().ToLower(), cancellationToken);
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null, CancellationToken cancellationToken = default)
    {
        var cleanedPhone = new string(phone.Where(char.IsDigit).ToArray());
        var query = _context.Customers.AsQueryable();
        if (excludeCustomerId.HasValue)
        {
            query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
        }
        
        var customers = await query.Select(c => new { c.CustomerId, c.Phone }).ToListAsync(cancellationToken);
        return !customers.Any(c => new string(c.Phone.Where(char.IsDigit).ToArray()) == cleanedPhone);
    }

    public async Task<string> GenerateCustomerCodeAsync(CancellationToken cancellationToken = default)
    {
        var count = await _context.Customers.CountAsync(cancellationToken);
        return $"CUST-{(count + 1):D4}";
    }
}

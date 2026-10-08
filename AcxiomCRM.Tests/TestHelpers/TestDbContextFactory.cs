using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Tests.TestHelpers;

public static class TestDbContextFactory
{
    public static AcxiomCrmDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AcxiomCrmDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new AcxiomCrmDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}

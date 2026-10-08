using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.DTOs.Opportunities;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Repositories;
using AcxiomCRM.Tests.TestHelpers;
using Xunit;

namespace AcxiomCRM.Tests.IntegrationTests;

public class OpportunityServiceTests
{
    private readonly UserContext _salesUser = new()
    {
        UserId = "sales-1",
        UserName = "sales@acxiomcrm.com",
        Role = AppRoles.SalesExecutive
    };

    [Fact]
    public async Task CreateOpportunity_AmountZero_FailsBusinessValidation()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var uow = new UnitOfWork(context);
        var auditService = new AuditService(uow);
        var customerService = new CustomerService(uow, auditService);
        var oppService = new OpportunityService(uow, auditService);

        var custResult = await customerService.CreateCustomerAsync(new CreateCustomerDto
        {
            CustomerName = "Big Buyer",
            Email = "buyer@big.com",
            Phone = "+1-555-098877"
        }, _salesUser);

        var dto = new CreateOpportunityDto
        {
            OpportunityName = "Bad Deal",
            CustomerId = custResult.Data!.CustomerId,
            Amount = 0m,
            Probability = 50,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(10)
        };

        // Act
        var result = await oppService.CreateOpportunityAsync(dto, _salesUser);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("must be greater than 0", result.Message);
    }

    [Fact]
    public async Task CreateOpportunity_PastCloseDate_FailsBusinessValidation()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var uow = new UnitOfWork(context);
        var auditService = new AuditService(uow);
        var customerService = new CustomerService(uow, auditService);
        var oppService = new OpportunityService(uow, auditService);

        var custResult = await customerService.CreateCustomerAsync(new CreateCustomerDto
        {
            CustomerName = "Big Buyer 2",
            Email = "buyer2@big.com",
            Phone = "+1-555-098876"
        }, _salesUser);

        var dto = new CreateOpportunityDto
        {
            OpportunityName = "Past Deal",
            CustomerId = custResult.Data!.CustomerId,
            Amount = 50000m,
            Probability = 50,
            ExpectedCloseDate = DateTime.UtcNow.Date.AddDays(-2)
        };

        // Act
        var result = await oppService.CreateOpportunityAsync(dto, _salesUser);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be in the past", result.Message);
    }
}

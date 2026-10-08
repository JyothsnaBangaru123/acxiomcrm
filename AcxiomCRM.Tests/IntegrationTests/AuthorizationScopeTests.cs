using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.DTOs.Leads;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Repositories;
using AcxiomCRM.Tests.TestHelpers;
using Xunit;

namespace AcxiomCRM.Tests.IntegrationTests;

public class AuthorizationScopeTests
{
    private readonly UserContext _rep1 = new()
    {
        UserId = "rep-1",
        UserName = "rep1@acxiomcrm.com",
        Role = AppRoles.SalesExecutive
    };

    private readonly UserContext _rep2 = new()
    {
        UserId = "rep-2",
        UserName = "rep2@acxiomcrm.com",
        Role = AppRoles.SalesExecutive
    };

    private readonly UserContext _admin = new()
    {
        UserId = "admin-1",
        UserName = "admin@acxiomcrm.com",
        Role = AppRoles.Admin
    };

    [Fact]
    public async Task SalesExecutive_CannotAccessOtherRepsCustomerDetails()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var uow = new UnitOfWork(context);
        var auditService = new AuditService(uow);
        var customerService = new CustomerService(uow, auditService);

        // Rep 1 creates a customer
        var createResult = await customerService.CreateCustomerAsync(new CreateCustomerDto
        {
            CustomerName = "Rep 1 Exclusive Account",
            Email = "exclusive@rep1.com",
            Phone = "+1-555-081122"
        }, _rep1);
        var customerId = createResult.Data!.CustomerId;

        // Act - Rep 2 attempts to fetch details of Rep 1's customer
        var rep2Access = await customerService.GetCustomerDetailsAsync(customerId, _rep2);

        // Act - Admin attempts to fetch details
        var adminAccess = await customerService.GetCustomerDetailsAsync(customerId, _admin);

        // Assert
        Assert.Null(rep2Access); // Rep 2 is BLOCKED
        Assert.NotNull(adminAccess); // Admin has full access
    }

    [Fact]
    public async Task SalesExecutive_CannotUpdateOtherRepsLead()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var uow = new UnitOfWork(context);
        var auditService = new AuditService(uow);
        var leadService = new LeadService(uow, auditService);

        // Rep 1 creates a lead
        var createResult = await leadService.CreateLeadAsync(new CreateLeadDto
        {
            LeadName = "High Value Lead",
            Email = "highval@lead.com",
            Phone = "+1-555-072233",
            ExpectedValue = 100000m
        }, _rep1);
        var leadId = createResult.Data!.LeadId;

        // Act - Rep 2 tries to update Rep 1's lead
        var updateResult = await leadService.UpdateLeadAsync(new UpdateLeadDto
        {
            LeadId = leadId,
            LeadName = "Hijacked Lead Name",
            Email = "highval@lead.com",
            Phone = "+1-555-072233",
            Status = LeadStatus.Qualified
        }, _rep2);

        // Assert
        Assert.False(updateResult.Success);
        Assert.Contains("Unauthorized", updateResult.Message);
    }
}

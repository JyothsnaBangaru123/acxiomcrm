using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Leads;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Repositories;
using AcxiomCRM.Tests.TestHelpers;
using Xunit;

namespace AcxiomCRM.Tests.IntegrationTests;

public class LeadServiceTests
{
    private readonly UserContext _salesUser = new()
    {
        UserId = "sales-1",
        UserName = "sales@acxiomcrm.com",
        Role = AppRoles.SalesExecutive
    };

    [Fact]
    public async Task ConvertLead_ValidLead_CreatesCustomerAndOpportunityAndMarksConverted()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var uow = new UnitOfWork(context);
        var auditService = new AuditService(uow);
        var leadService = new LeadService(uow, auditService);

        var leadDto = new CreateLeadDto
        {
            LeadName = "Tech Corp Prospect",
            Email = "lead@techcorp.com",
            Phone = "+1-555-029988",
            CompanyName = "Tech Corp Inc",
            Status = LeadStatus.Qualified,
            ExpectedValue = 75000m
        };
        var createResult = await leadService.CreateLeadAsync(leadDto, _salesUser);
        var leadId = createResult.Data!.LeadId;

        var convertDto = new ConvertLeadDto
        {
            LeadId = leadId,
            CreateOpportunity = true,
            OpportunityName = "Tech Corp - Cloud Migration",
            OpportunityAmount = 75000m,
            OpportunityStage = OpportunityStage.Qualification,
            Probability = 25,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var convertResult = await leadService.ConvertLeadAsync(convertDto, _salesUser);

        // Assert
        Assert.True(convertResult.Success);
        var customerId = convertResult.Data;
        Assert.True(customerId > 0);

        // Verify customer created
        var customer = await uow.Customers.GetByIdAsync(customerId);
        Assert.NotNull(customer);
        Assert.Equal("Tech Corp Prospect", customer.CustomerName);

        // Verify lead is marked converted
        var lead = await uow.Leads.GetByIdAsync(leadId);
        Assert.NotNull(lead);
        Assert.Equal(LeadStatus.Converted, lead.Status);
        Assert.Equal(customerId, lead.ConvertedCustomerId);

        // Verify linked opportunity created
        var opps = await uow.Opportunities.FindAsync(o => o.CustomerId == customerId);
        Assert.Single(opps);
        Assert.Equal(75000m, opps.First().Amount);

        // Verify Audit Log
        var logs = await uow.AuditLogs.GetAllAsync();
        Assert.Contains(logs, l => l.Action == "LeadConversion");
    }
}

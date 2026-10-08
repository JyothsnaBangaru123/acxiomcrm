using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Repositories;
using AcxiomCRM.Tests.TestHelpers;
using Xunit;

namespace AcxiomCRM.Tests.IntegrationTests;

public class CustomerServiceTests
{
    private readonly UserContext _adminUser = new()
    {
        UserId = "admin-1",
        UserName = "admin@acxiomcrm.com",
        Role = AppRoles.Admin
    };

    private readonly UserContext _salesUser = new()
    {
        UserId = "sales-1",
        UserName = "sales@acxiomcrm.com",
        Role = AppRoles.SalesExecutive
    };

    [Fact]
    public async Task CreateCustomer_ValidData_CreatesSuccessfullyAndLogsAudit()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var uow = new UnitOfWork(context);
        var auditService = new AuditService(uow);
        var customerService = new CustomerService(uow, auditService);

        var dto = new CreateCustomerDto
        {
            CustomerName = "Enterprise Global",
            Email = "contact@enterpriseglobal.com",
            Phone = "+1-555-0199123",
            CompanyName = "Enterprise Global Inc",
            City = "Boston",
            State = "MA",
            Status = CustomerStatus.Active
        };

        // Act
        var result = await customerService.CreateCustomerAsync(dto, _adminUser);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Enterprise Global", result.Data.CustomerName);
        Assert.StartsWith("CUST-", result.Data.CustomerCode);

        // Verify database existence
        var dbCustomer = await uow.Customers.GetByIdAsync(result.Data.CustomerId);
        Assert.NotNull(dbCustomer);

        // Verify Audit Log generated
        var logs = await uow.AuditLogs.GetAllAsync();
        Assert.Contains(logs, l => l.EntityName == "Customer" && l.Action == "Create" && l.RecordId == result.Data.CustomerId.ToString());
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_FailsValidation()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var uow = new UnitOfWork(context);
        var auditService = new AuditService(uow);
        var customerService = new CustomerService(uow, auditService);

        var dto1 = new CreateCustomerDto
        {
            CustomerName = "Acme Corp",
            Email = "dup@acme.com",
            Phone = "+1-555-010111"
        };
        await customerService.CreateCustomerAsync(dto1, _adminUser);

        var dto2 = new CreateCustomerDto
        {
            CustomerName = "Duplicate Corp",
            Email = "dup@acme.com",
            Phone = "+1-555-010222"
        };

        // Act
        var result = await customerService.CreateCustomerAsync(dto2, _adminUser);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
    }
}

using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Repositories;
using AcxiomCRM.Tests.TestHelpers;
using Xunit;

namespace AcxiomCRM.Tests.IntegrationTests;

public class AuditLoggingTests
{
    private readonly UserContext _user = new()
    {
        UserId = "test-user-123",
        UserName = "tester@acxiomcrm.com",
        Role = AppRoles.Admin,
        IpAddress = "192.168.1.50"
    };

    [Fact]
    public async Task ServiceOperations_GenerateDetailedAuditRecords()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var uow = new UnitOfWork(context);
        var auditService = new AuditService(uow);
        var customerService = new CustomerService(uow, auditService);

        // Act 1: Create
        var createResult = await customerService.CreateCustomerAsync(new CreateCustomerDto
        {
            CustomerName = "Audit Target Corp",
            Email = "audit@target.com",
            Phone = "+1-555-063344"
        }, _user);
        var custId = createResult.Data!.CustomerId;

        // Act 2: Update
        await customerService.UpdateCustomerAsync(new UpdateCustomerDto
        {
            CustomerId = custId,
            CustomerName = "Audit Target Corp Updated",
            Email = "audit@target.com",
            Phone = "+1-555-063344"
        }, _user);

        // Act 3: Deactivate
        await customerService.DeleteCustomerAsync(custId, _user);

        // Assert
        var logs = await uow.AuditLogs.GetAllAsync();
        Assert.True(logs.Count >= 3);

        var createLog = logs.FirstOrDefault(l => l.Action == "Create" && l.RecordId == custId.ToString());
        Assert.NotNull(createLog);
        Assert.Equal(_user.UserId, createLog.UserId);
        Assert.Equal(_user.UserName, createLog.UserName);
        Assert.Equal(_user.IpAddress, createLog.IpAddress);

        var updateLog = logs.FirstOrDefault(l => l.Action == "Update" && l.RecordId == custId.ToString());
        Assert.NotNull(updateLog);

        var deactLog = logs.FirstOrDefault(l => l.Action == "Deactivate" && l.RecordId == custId.ToString());
        Assert.NotNull(deactLog);
    }
}

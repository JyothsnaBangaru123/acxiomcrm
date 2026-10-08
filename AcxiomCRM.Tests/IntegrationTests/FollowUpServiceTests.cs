using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.FollowUps;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Repositories;
using AcxiomCRM.Tests.TestHelpers;
using Xunit;

namespace AcxiomCRM.Tests.IntegrationTests;

public class FollowUpServiceTests
{
    private readonly UserContext _salesUser = new()
    {
        UserId = "sales-1",
        UserName = "sales@acxiomcrm.com",
        Role = AppRoles.SalesExecutive
    };

    [Fact]
    public async Task CreateFollowUp_PastDate_FailsValidation()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var uow = new UnitOfWork(context);
        var auditService = new AuditService(uow);
        var fuService = new FollowUpService(uow, auditService);

        var dto = new CreateFollowUpDto
        {
            FollowUpDate = DateTime.UtcNow.Date.AddDays(-3),
            FollowUpType = FollowUpType.Call,
            Remarks = "Trying to schedule in past",
            Status = FollowUpStatus.Planned
        };

        // Act
        var result = await fuService.CreateFollowUpAsync(dto, _salesUser);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be earlier than today", result.Message);
    }

    [Fact]
    public async Task CompleteFollowUp_UpdatesStatusAndLogsAudit()
    {
        // Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var uow = new UnitOfWork(context);
        var auditService = new AuditService(uow);
        var fuService = new FollowUpService(uow, auditService);

        var createResult = await fuService.CreateFollowUpAsync(new CreateFollowUpDto
        {
            FollowUpDate = DateTime.UtcNow.Date.AddDays(1),
            FollowUpType = FollowUpType.Meeting,
            Remarks = "Quarterly check-in"
        }, _salesUser);

        var fuId = createResult.Data!.FollowUpId;

        // Act
        var completeResult = await fuService.CompleteFollowUpAsync(new CompleteFollowUpDto
        {
            FollowUpId = fuId,
            CompletionRemarks = "Client signed renewal!"
        }, _salesUser);

        // Assert
        Assert.True(completeResult.Success);

        var updatedFu = await uow.FollowUps.GetByIdAsync(fuId);
        Assert.NotNull(updatedFu);
        Assert.Equal(FollowUpStatus.Completed, updatedFu.Status);
        Assert.NotNull(updatedFu.CompletedDate);

        // Verify activity created
        var activities = await uow.Activities.GetAllAsync();
        Assert.Contains(activities, a => a.Description == "Client signed renewal!");
    }
}

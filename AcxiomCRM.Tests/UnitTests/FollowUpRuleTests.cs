using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Domain.Rules;
using Xunit;

namespace AcxiomCRM.Tests.UnitTests;

public class FollowUpRuleTests
{
    [Fact]
    public void ValidateDate_PlannedDateInPast_ReturnsError()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.Date.AddDays(-1);

        // Act
        var (isValid, errorMessage) = FollowUpRules.ValidateDate(pastDate, FollowUpStatus.Planned);

        // Assert
        Assert.False(isValid);
        Assert.Equal(FollowUpRules.PastDateError, errorMessage);
    }

    [Fact]
    public void ValidateDate_PlannedDateTodayOrFuture_ReturnsSuccess()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var futureDate = DateTime.UtcNow.Date.AddDays(5);

        // Act
        var resultToday = FollowUpRules.ValidateDate(today, FollowUpStatus.Planned);
        var resultFuture = FollowUpRules.ValidateDate(futureDate, FollowUpStatus.Planned);

        // Assert
        Assert.True(resultToday.IsValid);
        Assert.Null(resultToday.ErrorMessage);

        Assert.True(resultFuture.IsValid);
        Assert.Null(resultFuture.ErrorMessage);
    }
}

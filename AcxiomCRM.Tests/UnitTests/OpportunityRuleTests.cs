using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Domain.Rules;
using Xunit;

namespace AcxiomCRM.Tests.UnitTests;

public class OpportunityRuleTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    [InlineData(-0.01)]
    public void Validate_AmountZeroOrNegative_ReturnsError(decimal amount)
    {
        // Arrange
        var prob = 50;
        var futureDate = DateTime.UtcNow.AddDays(10);

        // Act
        var (isValid, errorMessage) = OpportunityRules.Validate(amount, prob, futureDate, OpportunityStatus.Open);

        // Assert
        Assert.False(isValid);
        Assert.Equal(OpportunityRules.AmountPositiveError, errorMessage);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validate_ProbabilityOutOfRange_ReturnsError(int probability)
    {
        // Arrange
        var amount = 10000m;
        var futureDate = DateTime.UtcNow.AddDays(10);

        // Act
        var (isValid, errorMessage) = OpportunityRules.Validate(amount, probability, futureDate, OpportunityStatus.Open);

        // Assert
        Assert.False(isValid);
        Assert.Equal(OpportunityRules.ProbabilityRangeError, errorMessage);
    }

    [Fact]
    public void Validate_ExpectedCloseDateInPast_ReturnsError()
    {
        // Arrange
        var amount = 10000m;
        var prob = 50;
        var pastDate = DateTime.UtcNow.Date.AddDays(-1);

        // Act
        var (isValid, errorMessage) = OpportunityRules.Validate(amount, prob, pastDate, OpportunityStatus.Open);

        // Assert
        Assert.False(isValid);
        Assert.Equal(OpportunityRules.PastCloseDateError, errorMessage);
    }

    [Fact]
    public void Validate_ValidActiveOpportunity_ReturnsSuccess()
    {
        // Arrange
        var amount = 50000m;
        var prob = 75;
        var futureDate = DateTime.UtcNow.Date.AddDays(30);

        // Act
        var (isValid, errorMessage) = OpportunityRules.Validate(amount, prob, futureDate, OpportunityStatus.Open);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void WeightedPipeline_CalculatesCorrectly()
    {
        // Arrange
        var opp = new Opportunity
        {
            Amount = 100000m,
            Probability = 60
        };

        // Act
        var weighted = opp.WeightedPipeline;

        // Assert
        Assert.Equal(60000m, weighted);
    }
}

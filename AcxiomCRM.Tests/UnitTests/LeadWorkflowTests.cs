using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Domain.Rules;
using Xunit;

namespace AcxiomCRM.Tests.UnitTests;

public class LeadWorkflowTests
{
    [Theory]
    [InlineData(LeadStatus.New, LeadStatus.Contacted)]
    [InlineData(LeadStatus.Contacted, LeadStatus.Qualified)]
    [InlineData(LeadStatus.Qualified, LeadStatus.Converted)]
    [InlineData(LeadStatus.Qualified, LeadStatus.Lost)]
    [InlineData(LeadStatus.New, LeadStatus.Lost)]
    public void ValidateTransition_ValidWorkflowTransitions_Succeeds(LeadStatus current, LeadStatus target)
    {
        var (isValid, errorMessage) = LeadStatusRules.ValidateTransition(current, target);
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateTransition_ConvertedLeadAttemptingChange_Fails()
    {
        var (isValid, errorMessage) = LeadStatusRules.ValidateTransition(LeadStatus.Converted, LeadStatus.Qualified);
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
    }
}

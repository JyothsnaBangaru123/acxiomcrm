using AcxiomCRM.Domain.Rules;
using Xunit;

namespace AcxiomCRM.Tests.UnitTests;

public class CustomerValidationTests
{
    [Theory]
    [InlineData("valid@example.com")]
    [InlineData("user.name+tag@sub.domain.co")]
    [InlineData("alex@acxiomcrm.com")]
    public void IsValidEmail_ValidEmails_ReturnsTrue(string email)
    {
        Assert.True(CustomerRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("notanemail")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    public void IsValidEmail_InvalidEmails_ReturnsFalse(string email)
    {
        Assert.False(CustomerRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData("+1-555-0199123")]
    [InlineData("5551234567")]
    [InlineData("+91 9876543210")]
    [InlineData("(555) 012-3456")]
    public void IsValidPhone_ValidPhones_ReturnsTrue(string phone)
    {
        Assert.True(CustomerRules.IsValidPhone(phone));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("abc-def-ghij")]
    [InlineData("555-PHONE-NOW")]
    public void IsValidPhone_InvalidPhones_ReturnsFalse(string phone)
    {
        Assert.False(CustomerRules.IsValidPhone(phone));
    }
}

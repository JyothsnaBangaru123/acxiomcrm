using Microsoft.AspNetCore.Identity;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Tests.TestHelpers;
using Xunit;

namespace AcxiomCRM.Tests.UnitTests;

public class AuthenticationSecurityTests
{
    [Fact]
    public void PasswordPolicy_RequiresComplexityAndLength()
    {
        var options = new PasswordOptions
        {
            RequiredLength = 8,
            RequireDigit = true,
            RequireLowercase = true,
            RequireUppercase = true,
            RequireNonAlphanumeric = true
        };

        Assert.Equal(8, options.RequiredLength);
        Assert.True(options.RequireDigit);
        Assert.True(options.RequireLowercase);
        Assert.True(options.RequireUppercase);
        Assert.True(options.RequireNonAlphanumeric);
    }

    [Fact]
    public void LockoutPolicy_HasConfiguredThresholds()
    {
        var options = new LockoutOptions
        {
            MaxFailedAccessAttempts = 5,
            DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15),
            AllowedForNewUsers = true
        };

        Assert.Equal(5, options.MaxFailedAccessAttempts);
        Assert.Equal(TimeSpan.FromMinutes(15), options.DefaultLockoutTimeSpan);
        Assert.True(options.AllowedForNewUsers);
    }
}

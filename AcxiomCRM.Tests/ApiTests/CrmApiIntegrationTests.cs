using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AcxiomCRM.Tests.ApiTests;

public class CrmApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CrmApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProtectedApiEndpoint_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/customers");

        // Assert
        // When unauthenticated, either 401 Unauthorized or 302 Redirect to /Account/Login
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task GetLoginEndpoint_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Account/Login");

        // Assert
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("AcxiomCRM", html);
        Assert.Contains("Sign in", html);
    }

    [Fact]
    public async Task GetSwaggerEndpoint_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Swagger failed with status {response.StatusCode}: {content}");
        Assert.Contains("AcxiomCRM", content);
    }
}

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using AcxiomCRM.Application.DTOs.Auth;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.DTOs.Leads;
using AcxiomCRM.Application.DTOs.Opportunities;
using AcxiomCRM.Domain.Enums;
using Xunit;

namespace AcxiomCRM.Tests.ApiTests;

public class CrmRestApiCrudTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CrmRestApiCrudTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        response.EnsureSuccessStatusCode();

        return client;
    }

    [Fact]
    public async Task Admin_CanPerformFullCustomerApiCrud()
    {
        // 1. Authenticate as Admin
        var client = await GetAuthenticatedClientAsync("admin@acxiomcrm.com", "Admin@12345!");

        var uniqueEmail = $"api.enterprise.{Guid.NewGuid():N}@test.com";
        var uniquePhone = $"+1-555-{Random.Shared.Next(1000000, 9999999)}";
        var createDto = new CreateCustomerDto
        {
            CustomerName = "API Test Enterprise",
            Email = uniqueEmail,
            Phone = uniquePhone,
            CompanyName = "API Enterprise Inc",
            City = "Denver",
            State = "CO",
            Status = CustomerStatus.Active
        };

        var createResponse = await client.PostAsJsonAsync("/api/customers", createDto);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.StatusCode == HttpStatusCode.Created, $"Failed to create customer ({createResponse.StatusCode}): {createContent}");

        var apiResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CustomerDto>>();
        Assert.NotNull(apiResult);
        Assert.True(apiResult.Success);
        var customerId = apiResult.Data!.CustomerId;

        // 3. GET customer by ID
        var getResponse = await client.GetAsync($"/api/customers/{customerId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // 4. PUT update customer
        var updateDto = new UpdateCustomerDto
        {
            CustomerId = customerId,
            CustomerName = "API Test Enterprise Updated",
            Email = uniqueEmail,
            Phone = uniquePhone,
            CompanyName = "API Enterprise Inc",
            City = "Denver",
            State = "CO",
            Status = CustomerStatus.Active
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/customers/{customerId}", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // 5. DELETE deactivate customer
        var deleteResponse = await client.DeleteAsync($"/api/customers/{customerId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateOpportunityApi_InvalidAmount_ReturnsBadRequest()
    {
        var client = await GetAuthenticatedClientAsync("admin@acxiomcrm.com", "Admin@12345!");

        var oppDto = new CreateOpportunityDto
        {
            OpportunityName = "Zero Dollar Deal",
            CustomerId = 1,
            Amount = 0m,
            Stage = OpportunityStage.Qualification,
            Probability = 20,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        };

        var response = await client.PostAsJsonAsync("/api/opportunities", oppDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

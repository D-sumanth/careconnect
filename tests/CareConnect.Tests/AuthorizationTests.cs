using Microsoft.AspNetCore.Mvc.Testing;

namespace CareConnect.Tests;

public sealed class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Theory]
    [InlineData("/Admin")]
    [InlineData("/Lead")]
    public async Task ProtectedPages_RedirectAnonymousUsersToLogin(string path)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(path);

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }
}

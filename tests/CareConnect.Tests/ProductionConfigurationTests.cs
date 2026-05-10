using CareConnect.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CareConnect.Tests;

public sealed class ProductionConfigurationTests
{
    [Fact]
    public void AddCareConnectInfrastructure_RequiresConnectionStringOutsideDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            })
            .Build();

        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCareConnectInfrastructure(configuration));

        Assert.Contains("DefaultConnection", exception.Message);
    }
}

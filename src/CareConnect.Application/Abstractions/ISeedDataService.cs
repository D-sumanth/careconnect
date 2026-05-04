namespace CareConnect.Application.Abstractions;

public interface ISeedDataService
{
    Task SeedDevelopmentAsync(CancellationToken cancellationToken = default);
}

namespace CareConnect.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

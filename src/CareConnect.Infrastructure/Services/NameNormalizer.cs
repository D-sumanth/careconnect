using System.Text.RegularExpressions;
using CareConnect.Application.Abstractions;

namespace CareConnect.Infrastructure.Services;

public sealed partial class NameNormalizer : INameNormalizer
{
    public string Normalize(string value)
    {
        var trimmed = value.Trim().ToUpperInvariant();
        return WhitespaceRegex().Replace(trimmed, " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

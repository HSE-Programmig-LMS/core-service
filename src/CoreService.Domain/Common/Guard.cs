namespace CoreService.Domain.Common;

/// <summary>
/// validation
/// </summary>
public static class Guard
{
    public static void NotNull(object? value, string name)
    {
        if (value is null) throw new ArgumentNullException(name);
    }

    public static void NotNullOrWhiteSpace(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", name);
    }

    public static void InRange(int value, int minInclusive, int maxInclusive, string name)
    {
        if (value < minInclusive || value > maxInclusive)
            throw new ArgumentOutOfRangeException(name, value,
                $"Value must be in range [{minInclusive}..{maxInclusive}].");
    }
}

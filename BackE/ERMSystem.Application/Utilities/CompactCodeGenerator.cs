using System.Text;

namespace ERMSystem.Application.Utilities;

public static class CompactCodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly DateTime EpochUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static string Generate(string prefix, DateTime nowUtc, int randomWidth = 3)
    {
        var normalizedPrefix = string.IsNullOrWhiteSpace(prefix)
            ? "ID"
            : prefix.Trim().ToUpperInvariant();
        var seconds = Math.Max(0, (long)(nowUtc.ToUniversalTime() - EpochUtc).TotalSeconds);
        var timePart = ToBase36(seconds).PadLeft(5, '0');
        var randomPart = ToBase36(Random.Shared.Next(0, (int)Math.Pow(36, randomWidth))).PadLeft(randomWidth, '0');
        return $"{normalizedPrefix}-{timePart}{randomPart}";
    }

    private static string ToBase36(long value)
    {
        if (value <= 0)
        {
            return "0";
        }

        var builder = new StringBuilder();
        var remaining = value;
        while (remaining > 0)
        {
            builder.Insert(0, Alphabet[(int)(remaining % 36)]);
            remaining /= 36;
        }

        return builder.ToString();
    }
}

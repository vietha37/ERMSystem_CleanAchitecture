using System.Text.RegularExpressions;

namespace ERMSystem.Infrastructure.Services;

internal static partial class SensitiveDataMasking
{
    public static string MaskAuditDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            return string.Empty;
        }

        var masked = detail.Trim();
        masked = ReplaceValue(masked, "Username", MaskUsername);
        masked = ReplaceValue(masked, "Email", MaskEmail);
        masked = ReplaceValue(masked, "Phone", MaskPhone);
        masked = ReplaceValue(masked, "MedicalRecordNumber", MaskIdentifier);
        return masked;
    }

    private static string ReplaceValue(string input, string key, Func<string, string> masker)
    {
        return ValuePattern().Replace(
            input,
            match =>
            {
                var currentKey = match.Groups["key"].Value;
                if (!string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
                {
                    return match.Value;
                }

                var value = match.Groups["value"].Value;
                return $"{currentKey}={masker(value)}";
            });
    }

    private static string MaskUsername(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length <= 2)
        {
            return new string('*', trimmed.Length);
        }

        return $"{trimmed[0]}***{trimmed[^1]}";
    }

    private static string MaskEmail(string value)
    {
        var trimmed = value.Trim();
        var atIndex = trimmed.IndexOf('@');
        if (atIndex <= 1 || atIndex == trimmed.Length - 1)
        {
            return "***";
        }

        var local = trimmed[..atIndex];
        var domain = trimmed[(atIndex + 1)..];
        var maskedLocal = local.Length <= 2
            ? $"{local[0]}*"
            : $"{local[0]}***{local[^1]}";

        return $"{maskedLocal}@{domain}";
    }

    private static string MaskPhone(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length <= 4)
        {
            return new string('*', digits.Length);
        }

        var suffix = digits[^4..];
        return $"***{suffix}";
    }

    private static string MaskIdentifier(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length <= 6)
        {
            return "***";
        }

        return $"{trimmed[..3]}***{trimmed[^3..]}";
    }

    [GeneratedRegex(@"(?<key>[A-Za-z_][A-Za-z0-9_]*)=(?<value>[^;]+)", RegexOptions.CultureInvariant)]
    private static partial Regex ValuePattern();
}

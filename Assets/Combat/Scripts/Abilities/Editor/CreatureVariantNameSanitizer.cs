using System.IO;

public static class CreatureVariantNameSanitizer
{
    public static string SanitizeAssetName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        string sanitized = raw.Trim();
        sanitized = sanitized.Replace(' ', '_');

        var result = new System.Text.StringBuilder(sanitized.Length);
        for (int i = 0; i < sanitized.Length; i++)
        {
            char c = sanitized[i];
            if (char.IsLetterOrDigit(c) || c == '_')
                result.Append(c);
        }

        return result.ToString();
    }

    public static string SanitizeUnitId(string raw)
    {
        return SanitizeAssetName(raw);
    }

    public static string SanitizeDisplayName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        return raw.Trim();
    }

    public static bool IsValidAssetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        string sanitized = SanitizeAssetName(name);
        return sanitized.Length > 0 && sanitized == name.Replace(' ', '_');
    }

    public static string GetAssetNameValidationError(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "name is empty.";

        string trimmed = name.Trim();
        if (trimmed != name)
            return "name cannot start or end with spaces.";

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (c == ' ')
                continue;
            if (!char.IsLetterOrDigit(c) && c != '_')
                return $"name contains invalid character '{c}'. Use only letters, numbers, underscores, and spaces.";
        }

        return null;
    }
}
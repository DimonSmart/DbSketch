using System.Text;

namespace DimonSmart.DbSketch.Core.Rendering;

internal static class RenderTextNormalizer
{
    public static string? NormalizeInlineComment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new StringBuilder(value.Length);
        var previousWasWhitespace = true;
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        var normalized = builder.ToString().Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    public static string? NormalizeInlineComment(string? value, int? maxLength)
    {
        var normalized = NormalizeInlineComment(value);
        if (normalized is null || maxLength is null)
        {
            return normalized;
        }

        if (maxLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Comment max length must be greater than zero.");
        }

        if (normalized.Length <= maxLength.Value)
        {
            return normalized;
        }

        return maxLength.Value == 1
            ? "…"
            : normalized[..(maxLength.Value - 1)] + "…";
    }
}

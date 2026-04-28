using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

internal static partial class MarkdownRenderer
{
    public static string Render(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return "<p></p>";

        var normalized = markdown.Replace("\r\n", "\n").Trim();
        var blocks = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var builder = new StringBuilder();

        foreach (var block in blocks)
        {
            var lines = block.Split('\n', StringSplitOptions.TrimEntries);
            if (lines.Length == 0)
                continue;

            if (lines.All(line => line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal)))
            {
                builder.Append("<ul>");
                foreach (var line in lines)
                {
                    builder.Append("<li>")
                        .Append(ApplyInlineMarkdown(line[2..]))
                        .Append("</li>");
                }

                builder.Append("</ul>");
                continue;
            }

            if (TryRenderHeading(lines, builder))
                continue;

            builder.Append("<p>")
                .Append(string.Join("<br />", lines.Select(ApplyInlineMarkdown)))
                .Append("</p>");
        }

        return builder.ToString();
    }

    private static bool TryRenderHeading(IReadOnlyList<string> lines, StringBuilder builder)
    {
        if (lines.Count != 1)
            return false;

        var line = lines[0];
        if (line.StartsWith("### ", StringComparison.Ordinal))
        {
            builder.Append("<h3>").Append(ApplyInlineMarkdown(line[4..])).Append("</h3>");
            return true;
        }

        if (line.StartsWith("## ", StringComparison.Ordinal))
        {
            builder.Append("<h2>").Append(ApplyInlineMarkdown(line[3..])).Append("</h2>");
            return true;
        }

        if (line.StartsWith("# ", StringComparison.Ordinal))
        {
            builder.Append("<h1>").Append(ApplyInlineMarkdown(line[2..])).Append("</h1>");
            return true;
        }

        return false;
    }

    private static string ApplyInlineMarkdown(string value)
    {
        var encoded = HtmlEncoder.Default.Encode(value);
        encoded = LinkRegex().Replace(encoded, match =>
        {
            var label = match.Groups[1].Value;
            var href = HtmlEncoder.Default.Encode(match.Groups[2].Value);
            return $"<a href=\"{href}\" target=\"_blank\" rel=\"noreferrer\">{label}</a>";
        });
        encoded = BoldRegex().Replace(encoded, "<strong>$1</strong>");
        encoded = ItalicRegex().Replace(encoded, "<em>$1</em>");
        encoded = CodeRegex().Replace(encoded, "<code>$1</code>");
        return encoded;
    }

    [GeneratedRegex(@"\[(.+?)\]\((.+?)\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"\*(.+?)\*")]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"`(.+?)`")]
    private static partial Regex CodeRegex();
}

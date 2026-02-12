using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ReverseMarkdown;
using YamlDotNet.Serialization;

namespace ConfluenceMd;

public static class Converter
{
    private static readonly ReverseMarkdown.Converter _markdownConverter = new ReverseMarkdown.Converter();

    public static string Slugify(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "untitled";
        }

        // Normalize Unicode to NFD and remove diacritics
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        var ascii = sb.ToString().Normalize(NormalizationForm.FormC);

        // Lowercase
        ascii = ascii.ToLowerInvariant();

        // Replace spaces and special chars with hyphens
        ascii = Regex.Replace(ascii, @"[^\w\s-]", "");
        ascii = Regex.Replace(ascii, @"[-\s]+", "-");

        // Remove leading/trailing hyphens
        ascii = ascii.Trim('-');

        // Truncate to 100 chars max
        if (ascii.Length > 100)
        {
            ascii = ascii.Substring(0, 100).TrimEnd('-');
        }

        // Fallback if empty
        if (string.IsNullOrEmpty(ascii))
        {
            ascii = "untitled";
        }

        return ascii;
    }

    public static Dictionary<string, object?> ExtractMetadata(JsonElement page, string baseUrl)
    {
        var metadata = new Dictionary<string, object?>();

        // Extract fields
        metadata["title"] = page.TryGetProperty("title", out var title) ? title.GetString() : null;
        metadata["page_id"] = page.TryGetProperty("id", out var id) ? id.GetString() : null;

        if (page.TryGetProperty("space", out var space) && space.TryGetProperty("key", out var spaceKey))
        {
            metadata["space_key"] = spaceKey.GetString();
        }
        else
        {
            metadata["space_key"] = null;
        }

        // Author
        if (page.TryGetProperty("history", out var history) && 
            history.TryGetProperty("createdBy", out var createdBy) &&
            createdBy.TryGetProperty("displayName", out var displayName))
        {
            metadata["author"] = displayName.GetString();
        }
        else
        {
            metadata["author"] = null;
        }

        // Dates
        if (page.TryGetProperty("history", out var hist) && hist.TryGetProperty("createdDate", out var created))
        {
            metadata["created"] = created.GetString();
        }
        else
        {
            metadata["created"] = null;
        }

        if (page.TryGetProperty("version", out var version) && version.TryGetProperty("when", out var when))
        {
            metadata["modified"] = when.GetString();
        }
        else
        {
            metadata["modified"] = null;
        }

        // URL
        if (page.TryGetProperty("_links", out var links) && links.TryGetProperty("webui", out var webui))
        {
            metadata["url"] = baseUrl + webui.GetString();
        }
        else
        {
            metadata["url"] = null;
        }

        // Parent ID
        if (page.TryGetProperty("ancestors", out var ancestors) && ancestors.GetArrayLength() > 0)
        {
            var lastAncestor = ancestors.EnumerateArray().Last();
            if (lastAncestor.TryGetProperty("id", out var parentId))
            {
                metadata["parent_id"] = parentId.GetString();
            }
            else
            {
                metadata["parent_id"] = null;
            }
        }
        else
        {
            metadata["parent_id"] = null;
        }

        return metadata;
    }

    public static string ConvertToMarkdown(string html, Dictionary<string, object?> metadata)
    {
        // Convert HTML to markdown
        var markdownBody = _markdownConverter.Convert(html);

        // Generate YAML frontmatter
        var serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
        var yamlStr = serializer.Serialize(metadata).TrimEnd('\n', '\r');

        // Format: ---\n{yaml}\n---\n\n{markdown}
        return $"---\n{yamlStr}\n---\n\n{markdownBody}";
    }
}

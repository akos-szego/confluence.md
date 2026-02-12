using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConfluenceMd;

public class PageExporter
{
    private readonly ConfluenceClient _client;
    private readonly string _outputPath;
    private readonly int _delayMs;
    private readonly string _baseUrl;
    private readonly bool _skipExisting;
    private readonly int _maxDepth;
    private readonly ILogger _logger;

    public PageExporter(
        ConfluenceClient client,
        string outputPath,
        int delayMs,
        string baseUrl,
        bool skipExisting = false,
        int maxDepth = 50,
        ILogger? logger = null)
    {
        _client = client;
        _outputPath = outputPath;
        _delayMs = delayMs;
        _baseUrl = baseUrl;
        _skipExisting = skipExisting;
        _maxDepth = maxDepth;
        _logger = logger ?? new ConsoleLogger();
    }

    private string GenerateUniqueFilename(string baseSlug, string directory)
    {
        var filename = $"{baseSlug}.md";
        var filepath = Path.Combine(directory, filename);

        if (!File.Exists(filepath))
        {
            return filename;
        }

        var counter = 2;
        while (true)
        {
            filename = $"{baseSlug}-{counter}.md";
            filepath = Path.Combine(directory, filename);
            if (!File.Exists(filepath))
            {
                return filename;
            }
            counter++;
        }
    }

    private async Task ApplyRateLimitAsync()
    {
        if (_delayMs > 0)
        {
            await Task.Delay(_delayMs);
        }
    }

    private async Task<string> WritePageFileAsync(JsonElement page, string parentPath, string baseUrl)
    {
        // Extract metadata and convert to markdown
        var metadata = Converter.ExtractMetadata(page, baseUrl);
        
        var html = "";
        if (page.TryGetProperty("body", out var body) &&
            body.TryGetProperty("storage", out var storage) &&
            storage.TryGetProperty("value", out var value))
        {
            html = value.GetString() ?? "";
        }

        var markdown = Converter.ConvertToMarkdown(html, metadata);

        // Generate slug and check for collision
        var title = page.TryGetProperty("title", out var t) ? t.GetString() : "untitled";
        var slug = Converter.Slugify(title ?? "untitled");
        var filename = GenerateUniqueFilename(slug, parentPath);

        // Create directory if needed
        Directory.CreateDirectory(parentPath);

        // Check if file exists and skip_existing is enabled
        var filepath = Path.Combine(parentPath, filename);
        if (_skipExisting && File.Exists(filepath))
        {
            _logger.Info($"Skipped (exists): {filepath}");
            return filename.EndsWith(".md") ? filename.Substring(0, filename.Length - 3) : filename;
        }

        // Write file
        await File.WriteAllTextAsync(filepath, markdown);
        _logger.Info($"Created: {filepath}");

        // Return actual filename without extension for child directory
        return filename.EndsWith(".md") ? filename.Substring(0, filename.Length - 3) : filename;
    }

    public async Task<(int successCount, int failureCount)> ExportTreeAsync(
        string rootPageId,
        string parentDirectory,
        int depth = 0)
    {
        // Check recursion depth limit
        if (depth >= _maxDepth)
        {
            _logger.Error($"Maximum recursion depth ({_maxDepth}) reached for page {rootPageId}");
            return (0, 1);
        }

        try
        {
            // Fetch root page
            _logger.Info($"Fetching page: {rootPageId}");
            using var page = await _client.GetPageAsync(rootPageId);
            await ApplyRateLimitAsync();

            // Write root page to disk
            var actualFilename = await WritePageFileAsync(page.RootElement, parentDirectory, _baseUrl);

            // Initialize counters
            var successCount = 1;
            var failureCount = 0;

            // Create child directory
            var childDirectory = Path.Combine(parentDirectory, actualFilename);

            // Fetch children
            var children = await _client.GetChildPagesAsync(rootPageId);
            await ApplyRateLimitAsync();

            // Recursively export each child
            foreach (var child in children)
            {
                if (child.TryGetProperty("id", out var childId))
                {
                    var id = childId.GetString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        var (childSuccess, childFailure) = await ExportTreeAsync(id, childDirectory, depth + 1);
                        successCount += childSuccess;
                        failureCount += childFailure;
                    }
                }
            }

            return (successCount, failureCount);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to export page {rootPageId}: {ex.Message}");
            return (0, 1);
        }
    }
}

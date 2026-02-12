using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using DotNetEnv;

namespace ConfluenceMd;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Load environment variables from .env file
        Env.Load();

        var rootCommand = new RootCommand("Export Confluence pages to Markdown files recursively.");

        var pageIdOption = new Option<string>(
            name: "--page-id",
            description: "Confluence page ID to export")
        {
            IsRequired = true
        };

        var outputPathOption = new Option<string>(
            name: "--output-path",
            description: "Output directory path")
        {
            IsRequired = true
        };

        var urlOption = new Option<string?>(
            name: "--url",
            description: "Confluence base URL (or set CONFLUENCE_URL env var)",
            getDefaultValue: () => null);

        var userOption = new Option<string?>(
            name: "--user",
            description: "Username/email for Cloud, omit for Server PAT (or set CONFLUENCE_USER)",
            getDefaultValue: () => null);

        var tokenOption = new Option<string?>(
            name: "--token",
            description: "API token (Cloud) or Personal Access Token (Server) (or set CONFLUENCE_TOKEN)",
            getDefaultValue: () => null);

        var delayMsOption = new Option<int>(
            name: "--delay-ms",
            description: "Delay between API calls in milliseconds",
            getDefaultValue: () => 100);

        var timeoutOption = new Option<int>(
            name: "--timeout",
            description: "HTTP request timeout in seconds",
            getDefaultValue: () => 30);

        var skipExistingOption = new Option<bool>(
            name: "--skip-existing",
            description: "Skip files that already exist (resume capability)",
            getDefaultValue: () => false);

        var maxDepthOption = new Option<int>(
            name: "--max-depth",
            description: "Maximum recursion depth",
            getDefaultValue: () => 50);

        var verboseOption = new Option<bool>(
            name: "--verbose",
            description: "Enable verbose logging",
            getDefaultValue: () => false);

        rootCommand.AddOption(pageIdOption);
        rootCommand.AddOption(outputPathOption);
        rootCommand.AddOption(urlOption);
        rootCommand.AddOption(userOption);
        rootCommand.AddOption(tokenOption);
        rootCommand.AddOption(delayMsOption);
        rootCommand.AddOption(timeoutOption);
        rootCommand.AddOption(skipExistingOption);
        rootCommand.AddOption(maxDepthOption);
        rootCommand.AddOption(verboseOption);

        rootCommand.SetHandler(async (context) =>
        {
            var pageId = context.ParseResult.GetValueForOption(pageIdOption)!;
            var outputPath = context.ParseResult.GetValueForOption(outputPathOption)!;
            var url = context.ParseResult.GetValueForOption(urlOption);
            var user = context.ParseResult.GetValueForOption(userOption);
            var token = context.ParseResult.GetValueForOption(tokenOption);
            var delayMs = context.ParseResult.GetValueForOption(delayMsOption);
            var timeout = context.ParseResult.GetValueForOption(timeoutOption);
            var skipExisting = context.ParseResult.GetValueForOption(skipExistingOption);
            var maxDepth = context.ParseResult.GetValueForOption(maxDepthOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            var logger = new ConsoleLogger(verbose);

            // Resolve credentials independently (CLI args > system env var > .env file)
            var confluenceUrl = url ?? Environment.GetEnvironmentVariable("CONFLUENCE_URL");
            var confluenceUser = user ?? Environment.GetEnvironmentVariable("CONFLUENCE_USER");
            var confluenceToken = token ?? Environment.GetEnvironmentVariable("CONFLUENCE_TOKEN");

            // Validate required credentials
            if (string.IsNullOrEmpty(confluenceUrl))
            {
                Console.Error.WriteLine("Error: CONFLUENCE_URL not provided (use --url or set environment variable)");
                context.ExitCode = 2;
                return;
            }
            if (string.IsNullOrEmpty(confluenceToken))
            {
                Console.Error.WriteLine("Error: CONFLUENCE_TOKEN not provided (use --token or set environment variable)");
                context.ExitCode = 2;
                return;
            }

            // Log authentication method
            if (!string.IsNullOrEmpty(confluenceUser))
            {
                Console.WriteLine($"Using Cloud authentication (username + token) for {confluenceUrl}");
            }
            else
            {
                Console.WriteLine($"Using Server authentication (Bearer token only) for {confluenceUrl}");
            }

            // Validate output directory is writable
            if (Directory.Exists(outputPath))
            {
                try
                {
                    var testFile = Path.Combine(outputPath, $".write-test-{Guid.NewGuid()}");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                }
                catch
                {
                    Console.Error.WriteLine($"Error: Output directory '{outputPath}' is not writable");
                    context.ExitCode = 2;
                    return;
                }
            }

            // Instantiate Confluence client
            using var client = new ConfluenceClient(confluenceUrl, confluenceToken, confluenceUser, timeout, logger);

            // Instantiate exporter
            var exporter = new PageExporter(
                client: client,
                outputPath: outputPath,
                delayMs: delayMs,
                baseUrl: confluenceUrl,
                skipExisting: skipExisting,
                maxDepth: maxDepth,
                logger: logger
            );

            // Execute export with progress reporting
            var modeMsg = skipExisting ? " (resuming, skipping existing files)" : "";
            Console.WriteLine($"Exporting page {pageId} to {outputPath}{modeMsg}...");
            Console.WriteLine($"Settings: timeout={timeout}s, delay={delayMs}ms, max_depth={maxDepth}");

            var (successCount, failureCount) = await exporter.ExportTreeAsync(pageId, outputPath);

            // Report results and exit with appropriate code
            if (failureCount == 0)
            {
                Console.WriteLine($"✓ All pages exported successfully ({successCount} pages)");
                context.ExitCode = 0;
            }
            else if (successCount > 0)
            {
                Console.Error.WriteLine($"⚠ Partial success: {successCount} succeeded, {failureCount} failed");
                context.ExitCode = 1;
            }
            else
            {
                Console.Error.WriteLine("✗ Export failed");
                context.ExitCode = 1;
            }
        });

        return await rootCommand.InvokeAsync(args);
    }
}

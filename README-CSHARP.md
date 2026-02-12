# Confluence.md - C# Version

## Overview

This is a C# implementation of the Confluence.md tool - a lightweight CLI application to recursively export Confluence pages to Markdown files with YAML frontmatter. 

The C# version provides the same functionality as the original Python version, allowing you to connect to Confluence Cloud/Server, fetch pages and their descendants, convert HTML content to Markdown, and save each page as a `.md` file with preserved metadata.

## Prerequisites

- .NET 8.0 SDK or later
- A Confluence Cloud or Server/Data Center instance
- Personal Access Token or API token for authentication

## Building the Application

1. **Clone the repository:**
   ```bash
   git clone https://github.com/akos-szego/confluence.md.git
   cd confluence.md
   ```

2. **Restore dependencies and build:**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Run the application:**
   ```bash
   dotnet run -- [options]
   ```

   Or build a release version:
   ```bash
   dotnet build -c Release
   ./bin/Release/net8.0/ConfluenceMd [options]
   ```

## Configuration

### Authentication Methods

The tool supports both **Confluence Cloud** and **Confluence Server/Data Center**:

#### Confluence Cloud
Generate an API token from your Atlassian account:
- URL: https://id.atlassian.com/manage-profile/security/api-tokens
- Required: URL + username (email) + API token

```env
CONFLUENCE_URL=https://your-domain.atlassian.net/wiki
CONFLUENCE_USER=your.email@example.com
CONFLUENCE_TOKEN=your_api_token_here
```

#### Confluence Server/Data Center
Generate a Personal Access Token from your Confluence instance:
- Location: User Settings → Personal Access Tokens
- Required: URL + PAT (no username needed)

```env
CONFLUENCE_URL=https://confluence.your-company.com
CONFLUENCE_TOKEN=your_personal_access_token_here
```

**Note:** Omit `CONFLUENCE_USER` for Server/Data Center instances to use Bearer token authentication.

### Environment Variables

Create a `.env` file in the project root (use `.env.example` as template):

```env
CONFLUENCE_URL=https://your-domain.atlassian.net/wiki
CONFLUENCE_USER=your.email@example.com
CONFLUENCE_TOKEN=your_personal_access_token
```

**Note:** Each credential resolves independently via: CLI argument → system environment variable → `.env` file.

## Usage

### Basic Usage

```bash
# Confluence Cloud (with username)
dotnet run -- --page-id 123456 --output-path ./output

# Confluence Server (without username, PAT only)
dotnet run -- --page-id 123456 --output-path ./output
```

### With Explicit Credentials

```bash
# Confluence Cloud
dotnet run -- \
  --page-id 123456 \
  --output-path ./output \
  --url https://your-domain.atlassian.net/wiki \
  --user your.email@example.com \
  --token abc123def456

# Confluence Server/Data Center (omit --user for PAT Bearer auth)
dotnet run -- \
  --page-id 123456 \
  --output-path ./output \
  --url https://confluence.your-company.com \
  --token your_personal_access_token
```

### With Verbose Logging and Rate Limiting

```bash
dotnet run -- \
  --page-id 123456 \
  --output-path ./output \
  --delay-ms 500 \
  --verbose
```

### Resume Existing Export

```bash
dotnet run -- \
  --page-id 123456 \
  --output-path ./output \
  --skip-existing
```

### With Custom Timeout and Recursion Limit

```bash
dotnet run -- \
  --page-id 123456 \
  --output-path ./output \
  --timeout 60 \
  --max-depth 100
```

## Command-Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `--page-id` | Confluence page ID to export (required) | - |
| `--output-path` | Output directory path (required) | - |
| `--url` | Confluence base URL | From env |
| `--user` | Username/email (Cloud only, omit for Server PAT) | From env |
| `--token` | API token (Cloud) or PAT (Server) | From env |
| `--delay-ms` | Delay between API calls (milliseconds) | 100 |
| `--timeout` | HTTP request timeout (seconds) | 30 |
| `--skip-existing` | Skip files that already exist (resume) | False |
| `--max-depth` | Maximum recursion depth | 50 |
| `--verbose` | Enable debug logging | False |

## Output Structure

Each exported page includes YAML frontmatter with metadata:

```markdown
---
title: "Page Title"
page_id: "123456"
space_key: "MYSPACE"
author: "John Doe"
created: "2024-01-15T10:30:00.000Z"
modified: "2024-01-20T14:45:00.000Z"
url: "https://your-domain.atlassian.net/wiki/spaces/MYSPACE/pages/123456/Page+Title"
parent_id: "789012"
---

# Page content in Markdown...
```

## Dependencies

This C# implementation uses the following NuGet packages:

- **System.CommandLine** (2.0.0-beta4.22272.1) - Command-line parsing
- **YamlDotNet** (13.7.1) - YAML serialization for frontmatter
- **ReverseMarkdown** (4.6.0) - HTML to Markdown conversion
- **DotNetEnv** (3.0.0) - .env file support

## Known Limitations

### Images and Attachments

The tool converts HTML content to Markdown but does **not** download images or file attachments. Image references in the output Markdown will point to the original Confluence URLs.

### Confluence Macros

Confluence-specific macros (status indicators, info panels, table of contents, expand sections, etc.) may not convert cleanly to Markdown. The ReverseMarkdown library will do its best to preserve content, but complex macro output may be stripped or appear as plain HTML.

**Supported formats:** Basic text formatting, headings, lists, tables, links, code blocks  
**Limited support:** Custom macros, embedded content, dynamic elements

### Recursion Limits

To prevent infinite loops from circular page references, the tool enforces a maximum recursion depth (default: 50 levels). Deep hierarchies beyond this limit will not be exported. Adjust with `--max-depth` if needed.

## Exit Codes

- `0`: All pages exported successfully
- `1`: Partial success (some pages failed)
- `2`: Invalid usage (missing credentials or page not found)

## Project Structure

```
confluence.md/
├── ConfluenceClient.cs    # Confluence API client wrapper
├── Converter.cs           # HTML to Markdown conversion
├── PageExporter.cs        # Recursive export logic
├── Logger.cs              # Logging interface
├── Program.cs             # CLI entry point
├── ConfluenceMd.csproj    # Project file
├── .env.example           # Environment template
├── .gitignore
└── README-CSHARP.md       # This file
```

## License

MIT License - See LICENSE file for details.

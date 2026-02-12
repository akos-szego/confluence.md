<div align="center">

# 📄 Confluence.md

**A lightweight CLI tool to recursively export Confluence pages to Markdown files**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/download)
[![GitHub stars](https://img.shields.io/github/stars/bzoboki/Confluence.md.svg?style=social)](https://github.com/bzoboki/Confluence.md/stargazers)

<a href="https://buymeacoffee.com/jatson" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" height="50"></a>

</div>

---

A lightweight CLI tool to recursively export Confluence pages to Markdown files with YAML frontmatter. Designed to extract content for documentation processing and analysis workflows.

**Built with .NET 8.0 for cross-platform compatibility.**

## Overview

This tool connects to Confluence Cloud/Server, fetches a page and all its descendants, converts HTML content to Markdown, and saves each page as a `.md` file with preserved metadata. The output structure mirrors the Confluence page hierarchy, making it ideal for feeding into documentation analysis tools.

## AI Agent Integration

The exported Markdown files are designed to be used as context for AI agents and LLMs (Large Language Models) to support various development activities:

- **Documentation Analysis**: Feed technical specifications, RFCs, and design documents to AI coding assistants for implementation guidance
- **Code Generation**: Use architectural decisions and API documentation as context for generating code that aligns with existing systems
- **Knowledge Retrieval**: Enable AI agents to answer questions about your team's processes, standards, and technical decisions
- **Automated Testing**: Provide test case documentation and requirements for AI-assisted test generation
- **Code Review**: Supply coding standards and best practices documentation to AI tools for automated code review

The clean Markdown format with YAML frontmatter makes it easy to ingest into AI workflows, vector databases, or RAG (Retrieval-Augmented Generation) systems.

## Prerequisites

- .NET 8.0 SDK or later
- A Confluence Cloud or Server/Data Center instance
- Personal Access Token or API token for authentication

## Installation

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

### Publishing as a Standalone Executable

You can publish the application as a self-contained executable that doesn't require .NET runtime to be installed:

**For Linux:**
```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

**For Windows:**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

**For macOS:**
```bash
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

The executable will be in `bin/Release/net8.0/{runtime}/publish/` directory.

## Configuration

### Authentication Methods

The tool supports both **Confluence Cloud** and **Confluence Server/Data Center**:

#### Confluence Cloud - **NOT TESTED YET**
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

You can set credentials using system environment variables:

```bash
export CONFLUENCE_URL=https://your-domain.atlassian.net/wiki
export CONFLUENCE_USER=your.email@example.com
export CONFLUENCE_TOKEN=your_personal_access_token
```

**Note:** Each credential resolves independently via: CLI argument → system environment variable.

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

## Environment Variables Reference

| Variable | Description | Required |
|----------|-------------|----------|
| `CONFLUENCE_URL` | Base URL of Confluence instance | Yes |
| `CONFLUENCE_USER` | Username or email address | Yes |
| `CONFLUENCE_TOKEN` | Personal Access Token | Yes |

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
Known Limitations

### Images and Attachments

The tool converts HTML content to Markdown but does **not** download images or file attachments. Image references in the output Markdown will point to the original Confluence URLs. If you need offline access to images, you'll need to download them separately and update the links.

### Confluence Macros

Confluence-specific macros (status indicators, info panels, table of contents, expand sections, etc.) may not convert cleanly to Markdown. The `markdownify` library will do its best to preserve content, but complex macro output may be stripped or appear as plain HTML.

**Supported formats:** Basic text formatting, headings, lists, tables, links, code blocks  
**Limited support:** Custom macros, embedded content, dynamic elements

### Recursion Limits

To prevent infinite loops from circular page references, the tool enforces a maximum recursion depth (default: 50 levels). Deep hierarchies beyond this limit will not be exported. Adjust with `--max-depth` if needed.

## 
Files are named using slugified titles (e.g., `my-page-title.md`). Collisions are resolved with numeric suffixes (`-2`, `-3`, etc.).

## Exit Codes

- `0`: All pages exported successfully
- `1`: Partial success (some pages failed)
- `2`: Invalid usage (missing credentials or page not found)
- `3`: Authentication failure
- `4`: Fatal error (connection failure or unexpected error)

## Troubleshooting

### Error: 401 Unauthorized

**Problem:** Invalid credentials or wrong authentication method

**Solution:**
- **For Confluence Cloud**: Ensure you're providing both `--user` (email) and `--token` (API token)
- **For Confluence Server/Data Center**: Omit `--user` or unset `CONFLUENCE_USER` - PAT uses Bearer auth only
- Verify token is valid and not expired
- Check username matches token owner (Cloud only)
- Ensure correct Confluence URL format

### Error: 403 Forbidden

**Problem:** Insufficient permissions

**Solution:**
- Verify you have read access to the page
- Check space permissions
- Confirm proper PAT scopes

### Error: 404 Not Found

**Problem:** Page ID doesn't exist

**Solution:**
- Increase timeout with `--timeout 60` (default is 30 seconds)
- Try again later if server is down
- Contact Confluence administrator

### Export Interrupted

**Problem:** Large export was interrupted mid-process

**Solution:**
- Use `--skip-existing` flag to resume export without re-fetching completed pages
- Already exported files will be skipped
- Only new/missing pages will be downloaded
- Ensure correct Confluence instance

### Error: 429 Rate Limited

**Problem:** Too many API requests

**Solution:**
- Increase `--delay-ms` parameter (try 500-1000)
- Tool auto-retries with exponential backoff (1s, 2s, 4s)
- Wait a few minutes before retrying

### Connection Timeout

**Problem:** Network or server issues

**Solution:**
- Check internet connectivity
- Verify Confluence URL is accessible
- Try again later if server is down
- Contact Confluence administrator

## Development

### Manual Testing

Test authentication and basic export:

```bash
# Test with verbose logging
dotnet run -- --page-id YOUR_PAGE_ID --output-path ./test-output --verbose

# Verify output files
ls -R ./test-output
```

### Project Structure

```
confluence.md/
├── ConfluenceClient.cs    # Confluence API client wrapper
├── Converter.cs           # HTML to Markdown conversion
├── PageExporter.cs        # Recursive export logic
├── Logger.cs              # Logging interface
├── Program.cs             # CLI entry point
├── ConfluenceMd.csproj    # Project file
├── .gitignore
└── README.md
```

## License

MIT License - See LICENSE file for details.

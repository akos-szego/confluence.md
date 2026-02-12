using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConfluenceMd;

public class ConfluenceAuthException : Exception
{
    public ConfluenceAuthException(string message) : base(message) { }
}

public class ConfluenceNotFoundException : Exception
{
    public ConfluenceNotFoundException(string message) : base(message) { }
}

public class ConfluenceConnectionException : Exception
{
    public ConfluenceConnectionException(string message, Exception? inner = null) : base(message, inner) { }
}

public class ConfluenceApiException : Exception
{
    public ConfluenceApiException(string message) : base(message) { }
}

public class ConfluenceClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ILogger _logger;

    public ConfluenceClient(string url, string token, string? user = null, int timeoutSeconds = 30, ILogger? logger = null)
    {
        _baseUrl = url.TrimEnd('/');
        _logger = logger ?? new ConsoleLogger();
        
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        // Set authentication header
        if (!string.IsNullOrEmpty(user))
        {
            // Confluence Cloud: Basic auth with username + token
            _logger.Info($"Using username+token authentication for {url}");
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{token}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
        else
        {
            // Confluence Server: Bearer token only
            _logger.Info($"Using token-only (Bearer) authentication for {url}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task<T> RetryWithBackoffAsync<T>(Func<Task<T>> func)
    {
        var delays = new[] { 1, 2, 4 };
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < 4)
        {
            try
            {
                return await func();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests || 
                                                   ex.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                lastException = ex;
                if (attempt < 3)
                {
                    var delay = delays[Math.Min(attempt, delays.Length - 1)];
                    _logger.Warning($"Rate limited (429/503), retrying in {delay}s (attempt {attempt + 1}/4)");
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                    attempt++;
                    continue;
                }
                throw;
            }
        }

        throw lastException!;
    }

    public async Task<JsonDocument> GetPageAsync(string pageId)
    {
        try
        {
            _logger.Debug($"Fetching page {pageId}");
            
            var result = await RetryWithBackoffAsync(async () =>
            {
                var url = $"{_baseUrl}/rest/api/content/{pageId}?expand=body.storage,history,version,space,ancestors";
                var response = await _httpClient.GetAsync(url);
                
                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new ConfluenceAuthException($"Authentication failed: {response.StatusCode}");
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ConfluenceNotFoundException($"Page {pageId} not found");
                }
                else if (!response.IsSuccessStatusCode)
                {
                    throw new ConfluenceApiException($"API error ({response.StatusCode}): {response.ReasonPhrase}");
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            });

            _logger.Debug($"Successfully fetched page {pageId}");
            return result;
        }
        catch (TaskCanceledException ex)
        {
            throw new ConfluenceConnectionException("Connection timeout", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ConfluenceConnectionException($"Connection failed: {ex.Message}", ex);
        }
    }

    public async Task<List<JsonElement>> GetChildPagesAsync(string pageId)
    {
        var allChildren = new List<JsonElement>();
        var start = 0;
        const int limit = 100;

        try
        {
            _logger.Debug($"Fetching child pages for {pageId}");

            while (true)
            {
                var result = await RetryWithBackoffAsync(async () =>
                {
                    var url = $"{_baseUrl}/rest/api/content/{pageId}/child/page?start={start}&limit={limit}";
                    var response = await _httpClient.GetAsync(url);

                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        throw new ConfluenceAuthException($"Authentication failed: {response.StatusCode}");
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new ConfluenceNotFoundException($"Page {pageId} not found");
                    }
                    else if (!response.IsSuccessStatusCode)
                    {
                        throw new ConfluenceApiException($"API error ({response.StatusCode}): {response.ReasonPhrase}");
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    return JsonDocument.Parse(content);
                });

                using (result)
                {
                    if (result.RootElement.TryGetProperty("results", out var results))
                    {
                        var batch = results.EnumerateArray().ToList();
                        var batchSize = batch.Count;
                        allChildren.AddRange(batch);
                        _logger.Debug($"Added {batchSize} children (total: {allChildren.Count})");

                        if (batchSize < limit)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }

                    if (result.RootElement.TryGetProperty("_links", out var links) &&
                        links.TryGetProperty("next", out _))
                    {
                        start += limit;
                        _logger.Debug($"More pages available, continuing with start={start}");
                    }
                    else
                    {
                        break;
                    }
                }
            }

            _logger.Info($"Found {allChildren.Count} child pages for page {pageId}");
            return allChildren;
        }
        catch (TaskCanceledException ex)
        {
            throw new ConfluenceConnectionException("Connection timeout", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ConfluenceConnectionException($"Connection failed: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

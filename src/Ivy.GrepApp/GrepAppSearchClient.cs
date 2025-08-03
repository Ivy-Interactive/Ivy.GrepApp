using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Ivy.GrepApp
{

public class GrepAppSearchClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly bool _disposeHttpClient;
    private const string BaseUrl = "https://grep.app/api/search";

    public GrepAppSearchClient() : this(new HttpClient(), true)
    {
    }

    public GrepAppSearchClient(HttpClient httpClient) : this(httpClient, false)
    {
    }

    private GrepAppSearchClient(HttpClient httpClient, bool disposeHttpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeHttpClient = disposeHttpClient;
        
        if (_httpClient.Timeout == TimeSpan.Zero || _httpClient.Timeout > TimeSpan.FromSeconds(30))
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        request.Validate();

        var queryParams = BuildQueryParameters(request);
        var url = $"{BaseUrl}?{queryParams}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new GrepApiRateLimitException("Rate limit exceeded. Please wait before making another request.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new SearchResponse
                {
                    Query = request.Query,
                    Summary = new SearchSummary
                    {
                        TotalResults = 0,
                        Message = "No results found for this query"
                    }
                };
            }

            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<GrepApiResponse>(jsonContent, _jsonOptions)
                ?? throw new GrepApiException("Failed to deserialize API response");

            return FormatResponse(apiResponse, request);
        }
        catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("The operation was canceled.", ex, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            throw new GrepApiTimeoutException("Request to grep.app API timed out");
        }
        catch (HttpRequestException ex)
        {
            throw new GrepApiException($"Network error while contacting grep.app API: {ex.Message}", ex);
        }
        catch (GrepApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new GrepApiException($"Unexpected error occurred: {ex.Message}", ex);
        }
    }

    private static string BuildQueryParameters(SearchRequest request)
    {
        var parameters = new Dictionary<string, string>
        {
            ["q"] = request.Query
        };

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            parameters["f.lang"] = request.Language;
        }

        if (!string.IsNullOrWhiteSpace(request.Repository))
        {
            parameters["f.repo"] = request.Repository;
        }

        if (!string.IsNullOrWhiteSpace(request.Path))
        {
            parameters["f.path"] = request.Path;
        }

        return string.Join("&", parameters.Select(kvp => 
            $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));
    }

    private SearchResponse FormatResponse(GrepApiResponse apiResponse, SearchRequest request)
    {
        var response = new SearchResponse
        {
            Query = request.Query
        };

        // Extract summary information
        var facets = apiResponse.Facets ?? new GrepApiFacets();
        response.Summary.TotalResults = facets.Count;

        // Extract language statistics
        if (facets.Lang?.Buckets != null)
        {
            response.Summary.TopLanguages = facets.Lang.Buckets
                .Take(5)
                .Select(b => new LanguageStatistic
                {
                    Language = b.Val ?? "Unknown",
                    Count = b.Count
                })
                .ToList();
        }

        // Extract repository statistics
        if (facets.Repo?.Buckets != null)
        {
            response.Summary.TopRepositories = facets.Repo.Buckets
                .Take(5)
                .Select(b => new RepositoryStatistic
                {
                    Repository = b.Val ?? "Unknown",
                    Count = b.Count
                })
                .ToList();
        }

        // Process search results
        var hits = apiResponse.Hits?.Hits ?? new List<GrepApiHit>();
        var repoGroups = new Dictionary<string, List<FileMatch>>();
        
        var resultLimit = request.ResultLimit ?? 10;
        foreach (var hit in hits.Take(resultLimit))
        {
            var repo = hit.Repo?.Raw ?? "Unknown";
            var fileMatch = new FileMatch
            {
                FilePath = hit.Path?.Raw ?? "Unknown",
                Branch = hit.Branch?.Raw ?? "main",
                TotalMatches = int.TryParse(hit.TotalMatches?.Raw, out var tm) ? tm : 0,
                LineNumbers = ExtractLineNumbers(hit.Content?.Snippet),
                Language = GetLanguageFromExtension(hit.Path?.Raw),
                CodeSnippet = FormatCodeSnippet(hit.Content?.Snippet, hit.Path?.Raw)
            };

            if (!repoGroups.ContainsKey(repo))
            {
                repoGroups[repo] = new List<FileMatch>();
            }
            repoGroups[repo].Add(fileMatch);
        }

        // Convert to result format
        response.ResultsByRepository = repoGroups
            .Select(kvp => new RepositoryResult
            {
                Repository = kvp.Key,
                MatchesCount = kvp.Value.Sum(f => f.TotalMatches),
                Files = kvp.Value
            })
            .OrderByDescending(r => r.MatchesCount)
            .ToList();

        response.Summary.ResultsShown = response.ResultsByRepository.Sum(r => r.Files.Count);
        response.Summary.RepositoriesFound = response.ResultsByRepository.Count;

        return response;
    }

    private static List<int> ExtractLineNumbers(string? htmlSnippet)
    {
        if (string.IsNullOrEmpty(htmlSnippet))
            return new List<int>();

        var lineNumbers = new List<int>();
        var matches = Regex.Matches(htmlSnippet, @"data-line=""(\d+)""");
        
        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out var lineNumber))
            {
                lineNumbers.Add(lineNumber);
            }
        }

        return lineNumbers;
    }

    private static string ExtractTextFromHtml(string? htmlSnippet)
    {
        if (string.IsNullOrEmpty(htmlSnippet))
            return string.Empty;

        // Remove HTML tags
        var text = Regex.Replace(htmlSnippet, @"<[^>]+>", "");
        
        // Replace HTML entities
        text = HttpUtility.HtmlDecode(text);
        
        return text.Trim();
    }

    private static string GetLanguageFromExtension(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !filePath.Contains('.'))
            return "text";

        var extension = filePath.Split('.').Last().ToLowerInvariant();
        
        return extension switch
        {
            "py" => "python",
            "js" => "javascript",
            "ts" => "typescript",
            "jsx" => "javascript",
            "tsx" => "typescript",
            "java" => "java",
            "c" => "c",
            "cpp" or "cc" or "cxx" => "cpp",
            "h" => "c",
            "hpp" => "cpp",
            "cs" => "csharp",
            "php" => "php",
            "rb" => "ruby",
            "go" => "go",
            "rs" => "rust",
            "swift" => "swift",
            "kt" => "kotlin",
            "scala" => "scala",
            "sh" or "bash" or "zsh" or "fish" => "bash",
            "ps1" => "powershell",
            "sql" => "sql",
            "html" or "htm" => "html",
            "xml" => "xml",
            "css" => "css",
            "scss" => "scss",
            "sass" => "sass",
            "less" => "less",
            "json" => "json",
            "yaml" or "yml" => "yaml",
            "toml" => "toml",
            "ini" or "cfg" or "conf" => "ini",
            "md" or "markdown" => "markdown",
            "tex" => "latex",
            "r" => "r",
            "matlab" or "m" => "matlab",
            "pl" => "perl",
            "lua" => "lua",
            "vim" => "vim",
            "dockerfile" => "dockerfile",
            "makefile" or "make" => "makefile",
            _ => "text"
        };
    }

    private static string FormatCodeSnippet(string? htmlSnippet, string? filePath)
    {
        if (string.IsNullOrEmpty(htmlSnippet))
            return string.Empty;

        var snippet = ExtractTextFromHtml(htmlSnippet);
        if (string.IsNullOrWhiteSpace(snippet))
            return string.Empty;

        // Limit snippet length
        const int maxLength = 400;
        if (snippet.Length > maxLength)
        {
            snippet = snippet.Substring(0, maxLength) + "...";
        }

        // Split into lines and clean up
        var lines = snippet.Split('\n')
            .Select(line => line.TrimEnd())
            .Where(line => !string.IsNullOrEmpty(line))
            .Take(8)
            .ToList();

        if (lines.Count == 8 && snippet.Split('\n').Length > 8)
        {
            lines.Add("... (truncated)");
        }

        var formattedSnippet = string.Join('\n', lines);
        var language = GetLanguageFromExtension(filePath);

        // Add language marker for syntax highlighting
        if (!string.IsNullOrEmpty(language) && language != "text")
        {
            return $"```{language}\n{formattedSnippet}\n```";
        }

        return formattedSnippet;
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}

// Internal API response models
internal class GrepApiResponse
{
    [JsonPropertyName("facets")]
    public GrepApiFacets? Facets { get; set; }

    [JsonPropertyName("hits")]
    public GrepApiHits? Hits { get; set; }
}

internal class GrepApiFacets
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("lang")]
    public GrepApiFacetBucket? Lang { get; set; }

    [JsonPropertyName("repo")]
    public GrepApiFacetBucket? Repo { get; set; }
}

internal class GrepApiFacetBucket
{
    [JsonPropertyName("buckets")]
    public List<GrepApiBucket>? Buckets { get; set; }
}

internal class GrepApiBucket
{
    [JsonPropertyName("val")]
    public string? Val { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

internal class GrepApiHits
{
    [JsonPropertyName("hits")]
    public List<GrepApiHit>? Hits { get; set; }
}

internal class GrepApiHit
{
    [JsonPropertyName("repo")]
    public GrepApiField? Repo { get; set; }

    [JsonPropertyName("path")]
    public GrepApiField? Path { get; set; }

    [JsonPropertyName("branch")]
    public GrepApiField? Branch { get; set; }

    [JsonPropertyName("total_matches")]
    public GrepApiField? TotalMatches { get; set; }

    [JsonPropertyName("content")]
    public GrepApiContent? Content { get; set; }
}

internal class GrepApiField
{
    [JsonPropertyName("raw")]
    public string? Raw { get; set; }
}

internal class GrepApiContent
{
    [JsonPropertyName("snippet")]
    public string? Snippet { get; set; }
}
}
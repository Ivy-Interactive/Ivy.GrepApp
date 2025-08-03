using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ivy.GrepApp
{

public class SearchResponse
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public SearchSummary Summary { get; set; } = new();

    [JsonPropertyName("results_by_repository")]
    public List<RepositoryResult> ResultsByRepository { get; set; } = new();
}

public class SearchSummary
{
    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }

    [JsonPropertyName("results_shown")]
    public int ResultsShown { get; set; }

    [JsonPropertyName("repositories_found")]
    public int RepositoriesFound { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("top_languages")]
    public List<LanguageStatistic> TopLanguages { get; set; } = new();

    [JsonPropertyName("top_repositories")]
    public List<RepositoryStatistic> TopRepositories { get; set; } = new();
}

public class LanguageStatistic
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class RepositoryStatistic
{
    [JsonPropertyName("repository")]
    public string Repository { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class RepositoryResult
{
    [JsonPropertyName("repository")]
    public string Repository { get; set; } = string.Empty;

    [JsonPropertyName("matches_count")]
    public int MatchesCount { get; set; }

    [JsonPropertyName("files")]
    public List<FileMatch> Files { get; set; } = new();
}

public class FileMatch
{
    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("branch")]
    public string Branch { get; set; } = string.Empty;

    [JsonPropertyName("total_matches")]
    public int TotalMatches { get; set; }

    [JsonPropertyName("line_numbers")]
    public List<int> LineNumbers { get; set; } = new();

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("code_snippet")]
    public string CodeSnippet { get; set; } = string.Empty;
}
}
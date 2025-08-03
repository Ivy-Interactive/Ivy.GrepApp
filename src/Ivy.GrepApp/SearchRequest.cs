using System;
using System.Linq;

namespace Ivy.GrepApp
{

public class SearchRequest
{
    public SearchRequest(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or whitespace.", nameof(query));
        if (query.Length > 1000)
        {
            throw new ArgumentException("Query is too long (max 1000 characters)", nameof(query));
        }
        
        Query = query;
    }

    public string Query { get; }

    public string? Language { get; set; }

    public string? Repository { get; set; }

    public string? Path { get; set; }

    public int? ResultLimit { get; set; } = 10;

    internal void Validate()
    {
        if (Language is not null)
        {
            if (string.IsNullOrWhiteSpace(Language))
            {
                throw new ArgumentException("Language parameter must be a non-empty string when provided", nameof(Language));
            }
            if (Language.Length > 50)
            {
                throw new ArgumentException("Language parameter is too long (max 50 characters)", nameof(Language));
            }
        }

        if (Repository is not null)
        {
            if (string.IsNullOrWhiteSpace(Repository))
            {
                throw new ArgumentException("Repository parameter must be a non-empty string when provided", nameof(Repository));
            }
            if (!Repository.Contains('/') || Repository.Count(c => c == '/') != 1)
            {
                throw new ArgumentException("Repository parameter must be in format 'owner/repository' (e.g., 'fastapi/fastapi')", nameof(Repository));
            }
            if (Repository.Length > 100)
            {
                throw new ArgumentException("Repository parameter is too long (max 100 characters)", nameof(Repository));
            }
        }

        if (Path is not null)
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                throw new ArgumentException("Path parameter must be a non-empty string when provided", nameof(Path));
            }
            if (Path.Length > 200)
            {
                throw new ArgumentException("Path parameter is too long (max 200 characters)", nameof(Path));
            }
        }

        if (ResultLimit is not null && (ResultLimit < 1 || ResultLimit > 100))
        {
            throw new ArgumentException("ResultLimit must be between 1 and 100", nameof(ResultLimit));
        }
    }
}
}
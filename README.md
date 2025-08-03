# Ivy.GrepApp

A .NET Standard 2.1 client library for searching GitHub code using the [grep.app](https://grep.app) API.

Created by the team at [Ivy](https://ivy.app) - The ultimate framework for creating internal business app in pure C#.

## Features

- Simple, clean API for GitHub code search
- Built-in rate limiting and error handling
- Comprehensive exception handling
- Strongly-typed request and response models
- Cross-platform compatibility (.NET Standard 2.1 - works with .NET Core 3.0+, .NET Framework 4.8+, .NET 5+)

## Installation

Install the package from NuGet:

```bash
dotnet add package Ivy.GrepApp
```

## Quick Start

```csharp
using Ivy.GrepApp;

// Create a client
using var client = new GrepAppSearchClient();

// Create a search request
var request = new SearchRequest("async function")
{
    Language = "JavaScript",
    Repository = "nodejs/node",
    Path = "lib/",
    ResultLimit = 10
};

// Execute the search
var response = await client.SearchAsync(request);

// Access results
Console.WriteLine($"Found {response.Summary.TotalResults} results");
foreach (var repo in response.ResultsByRepository)
{
    Console.WriteLine($"Repository: {repo.Repository}");
    foreach (var file in repo.Files)
    {
        Console.WriteLine($"  File: {file.FilePath} ({file.TotalMatches} matches)");
    }
}
```

## API Reference

### GrepAppSearchClient

The main client class for interacting with the grep.app API.

#### Constructors

- `GrepAppSearchClient()` - Creates a new client with default HttpClient
- `GrepAppSearchClient(HttpClient httpClient)` - Creates a client with provided HttpClient

#### Methods

- `SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)` - Executes a search and returns results

### SearchRequest

Represents a search query with optional filters.

#### Properties

- `Query` (string, required) - The search query (max 1000 characters)
- `Language` (string?, optional) - Filter by programming language (max 50 characters)
- `Repository` (string?, optional) - Filter by repository in format "owner/repo" (max 100 characters)
- `Path` (string?, optional) - Filter by file path (max 200 characters)
- `ResultLimit` (int?, optional) - Limit number of results (1-100, default: 10)

### SearchResponse

Contains the search results and metadata.

#### Properties

- `Query` - The original search query
- `Summary` - Summary information including total results count
- `ResultsByRepository` - Array of results grouped by repository

## Exception Handling

The library provides specific exception types:

- `GrepApiException` - Base exception for API errors
- `GrepApiTimeoutException` - Thrown when requests timeout
- `GrepApiRateLimitException` - Thrown when rate limits are exceeded

```csharp
try
{
    var response = await client.SearchAsync(request);
}
catch (GrepApiRateLimitException)
{
    // Handle rate limiting
    await Task.Delay(TimeSpan.FromMinutes(1));
}
catch (GrepApiTimeoutException)
{
    // Handle timeout
    Console.WriteLine("Request timed out, try again later");
}
catch (GrepApiException ex)
{
    // Handle other API errors
    Console.WriteLine($"API error: {ex.Message}");
}
```

## Legal Notice and Terms of Use

**Important**: By using this library, you acknowledge and agree to the following:

- You must be at least 13 years old to use this service
- You agree to comply with [grep.app's Terms of Service](https://grep.app/terms)
- This library accesses a third-party service (grep.app) operated by Vercel
- The service is provided "AS IS" with no warranties
- You are responsible for evaluating all search results for correctness
- Automated or scripted usage may be restricted by the service provider
- You must comply with all applicable trade laws and export controls

**Disclaimer**: This is an unofficial third-party client library. The authors are not affiliated with Vercel or the grep.app service. Users are responsible for ensuring their usage complies with all applicable terms of service and legal requirements.

## License

This library is licensed under the MIT License. See the LICENSE file for details.

## Contributing

Contributions are welcome! Please ensure all tests pass and follow the existing code style.

## Support

For issues with this library, please open an issue on GitHub. For issues with the grep.app service itself, please contact Vercel directly.
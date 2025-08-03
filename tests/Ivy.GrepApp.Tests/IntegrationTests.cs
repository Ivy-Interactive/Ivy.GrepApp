using FluentAssertions;
using Xunit;

namespace Ivy.GrepApp.Tests;

[Trait("Category", "Integration")]
public class IntegrationTests
{
    // These tests hit the real grep.app API and should be run sparingly
    // They can be excluded from CI builds using: dotnet test --filter "Category!=Integration"

    [Fact(Skip = "Integration test - uncomment to run against real API")]
    public async Task SearchAsync_WithRealApi_ShouldReturnResults()
    {
        // Arrange
        using var client = new GrepAppSearchClient();
        var request = new SearchRequest("console.log")
        {
            Language = "JavaScript",
            ResultLimit = 5
        };

        // Act
        var result = await client.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Query.Should().Be("console.log");
        result.Summary.Should().NotBeNull();
        result.Summary.TotalResults.Should().BeGreaterThan(0);
        result.ResultsByRepository.Should().NotBeEmpty();
    }

    [Fact(Skip = "Integration test - uncomment to run against real API")]
    public async Task SearchAsync_WithSpecificRepository_ShouldFilterResults()
    {
        // Arrange
        using var client = new GrepAppSearchClient();
        var request = new SearchRequest("useState")
        {
            Repository = "facebook/react",
            ResultLimit = 3
        };

        // Act
        var result = await client.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ResultsByRepository.Should().NotBeEmpty();
        result.ResultsByRepository.Should().OnlyContain(r => r.Repository == "facebook/react");
    }

    [Fact(Skip = "Integration test - uncomment to run against real API")]
    public async Task SearchAsync_WithPathFilter_ShouldFilterByPath()
    {
        // Arrange
        using var client = new GrepAppSearchClient();
        var request = new SearchRequest("import")
        {
            Language = "Python",
            Path = "test",
            ResultLimit = 5
        };

        // Act
        var result = await client.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        if (result.ResultsByRepository.Any())
        {
            result.ResultsByRepository.SelectMany(r => r.Files)
                .Should().OnlyContain(f => f.FilePath.Contains("test", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact(Skip = "Integration test - uncomment to run against real API")]
    public async Task SearchAsync_WithNoResults_ShouldReturnEmptyResults()
    {
        // Arrange
        using var client = new GrepAppSearchClient();
        var request = new SearchRequest("extremely_unlikely_string_to_find_in_any_code_base_12345")
        {
            ResultLimit = 1
        };

        // Act
        var result = await client.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Query.Should().Be("extremely_unlikely_string_to_find_in_any_code_base_12345");
        result.Summary.TotalResults.Should().Be(0);
        result.ResultsByRepository.Should().BeEmpty();
    }

    [Fact(Skip = "Integration test - uncomment to run against real API")]
    public async Task SearchAsync_WithCancellation_ShouldSupportCancellation()
    {
        // Arrange
        using var client = new GrepAppSearchClient();
        var request = new SearchRequest("async function");
        using var cts = new CancellationTokenSource();
        
        // Start the request and immediately cancel
        var searchTask = client.SearchAsync(request, cts.Token);
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => searchTask);
    }

    [Fact(Skip = "Integration test - uncomment to run against real API")]
    public async Task SearchAsync_WithMultipleLanguages_ShouldReturnVariedResults()
    {
        // Arrange
        using var client = new GrepAppSearchClient();
        var request = new SearchRequest("function")
        {
            ResultLimit = 10
        };

        // Act
        var result = await client.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Summary.TopLanguages.Should().NotBeEmpty();
        result.Summary.TopLanguages.Should().HaveCountGreaterThan(1);
    }

    [Fact(Skip = "Integration test - uncomment to run against real API")]
    public async Task SearchAsync_VerifyResponseStructure_ShouldHaveExpectedFields()
    {
        // Arrange
        using var client = new GrepAppSearchClient();
        var request = new SearchRequest("TODO")
        {
            Language = "Go",
            ResultLimit = 2
        };

        // Act
        var result = await client.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        
        // Verify summary structure
        result.Summary.Should().NotBeNull();
        result.Summary.TotalResults.Should().BeGreaterOrEqualTo(0);
        result.Summary.ResultsShown.Should().BeGreaterOrEqualTo(0);
        result.Summary.RepositoriesFound.Should().BeGreaterOrEqualTo(0);
        
        // Verify repository results structure
        if (result.ResultsByRepository.Any())
        {
            var firstRepo = result.ResultsByRepository.First();
            firstRepo.Repository.Should().NotBeNullOrEmpty();
            firstRepo.MatchesCount.Should().BeGreaterThan(0);
            firstRepo.Files.Should().NotBeEmpty();
            
            var firstFile = firstRepo.Files.First();
            firstFile.FilePath.Should().NotBeNullOrEmpty();
            firstFile.Branch.Should().NotBeNullOrEmpty();
            firstFile.Language.Should().NotBeNullOrEmpty();
            firstFile.CodeSnippet.Should().NotBeNullOrEmpty();
        }
    }

    [Fact(Skip = "Integration test - uncomment to run against real API")]
    public async Task SearchAsync_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        using var client = new GrepAppSearchClient();
        var request = new SearchRequest("async/await")
        {
            Language = "JavaScript",
            ResultLimit = 3
        };

        // Act
        var result = await client.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Query.Should().Be("async/await");
        // API should handle special characters in search query
    }
}
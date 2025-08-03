using FluentAssertions;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Ivy.GrepApp.Tests;

public class GrepAppSearchClientTests
{
    [Fact]
    public void Constructor_WithDefaultConstructor_ShouldCreateHttpClient()
    {
        // Act
        using var client = new GrepAppSearchClient();

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new GrepAppSearchClient(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("httpClient");
    }

    [Fact]
    public void Constructor_WithProvidedHttpClient_ShouldUseIt()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        using var client = new GrepAppSearchClient(httpClient);

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldSetTimeoutTo30Seconds_WhenNotSet()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        using var client = new GrepAppSearchClient(httpClient);

        // Assert
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Constructor_ShouldSetTimeoutTo30Seconds_WhenTimeoutExceeds30Seconds()
    {
        // Arrange
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

        // Act
        using var client = new GrepAppSearchClient(httpClient);

        // Assert
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Constructor_ShouldKeepExistingTimeout_WhenLessThan30Seconds()
    {
        // Arrange
        var originalTimeout = TimeSpan.FromSeconds(15);
        using var httpClient = new HttpClient { Timeout = originalTimeout };

        // Act
        using var client = new GrepAppSearchClient(httpClient);

        // Assert
        httpClient.Timeout.Should().Be(originalTimeout);
    }

    [Fact]
    public async Task SearchAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var client = new GrepAppSearchClient();

        // Act
        var act = async () => await client.SearchAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SearchAsync_WithInvalidRequest_ShouldThrowArgumentException()
    {
        // Arrange
        using var client = new GrepAppSearchClient();
        var request = new SearchRequest("test") { Repository = "invalid-format" };

        // Act
        var act = async () => await client.SearchAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Repository parameter must be in format*");
    }

    [Fact]
    public async Task SearchAsync_WithValidRequest_ShouldReturnResults()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var apiResponse = CreateMockApiResponse();
        
        mockHttp.When("https://grep.app/api/search?q=test+query")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        using var httpClient = new HttpClient(mockHttp);
        using var client = new GrepAppSearchClient(httpClient);
        var request = new SearchRequest("test query");

        // Act
        var result = await client.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Query.Should().Be("test query");
        result.Summary.TotalResults.Should().Be(100);
        result.ResultsByRepository.Should().HaveCount(1);
        result.ResultsByRepository[0].Repository.Should().Be("example/repo");
    }

    [Fact]
    public async Task SearchAsync_WithAllParameters_ShouldBuildCorrectUrl()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var expectedUrl = "https://grep.app/api/search?q=async+function&f.lang=JavaScript&f.repo=nodejs%2Fnode&f.path=lib%2F";
        
        var mockedRequest = mockHttp.When(expectedUrl)
            .Respond("application/json", JsonSerializer.Serialize(CreateMockApiResponse()));

        using var httpClient = new HttpClient(mockHttp);
        using var client = new GrepAppSearchClient(httpClient);
        
        var request = new SearchRequest("async function")
        {
            Language = "JavaScript",
            Repository = "nodejs/node",
            Path = "lib/"
        };

        // Act
        var result = await client.SearchAsync(request);

        // Assert
        mockHttp.GetMatchCount(mockedRequest).Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_WithRateLimitError_ShouldThrowGrepApiRateLimitException()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://grep.app/api/search?q=test")
            .Respond(HttpStatusCode.TooManyRequests);

        using var httpClient = new HttpClient(mockHttp);
        using var client = new GrepAppSearchClient(httpClient);
        var request = new SearchRequest("test");

        // Act
        var act = async () => await client.SearchAsync(request);

        // Assert
        await act.Should().ThrowAsync<GrepApiRateLimitException>()
            .WithMessage("Rate limit exceeded*");
    }

    [Fact]
    public async Task SearchAsync_WithNotFoundResponse_ShouldReturnEmptyResults()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://grep.app/api/search?q=test")
            .Respond(HttpStatusCode.NotFound);

        using var httpClient = new HttpClient(mockHttp);
        using var client = new GrepAppSearchClient(httpClient);
        var request = new SearchRequest("test");

        // Act
        var result = await client.SearchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Query.Should().Be("test");
        result.Summary.TotalResults.Should().Be(0);
        result.Summary.Message.Should().Be("No results found for this query");
        result.ResultsByRepository.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithTimeout_ShouldThrowGrepApiTimeoutException()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://grep.app/api/search?q=test")
            .Throw(new TaskCanceledException());

        using var httpClient = new HttpClient(mockHttp);
        using var client = new GrepAppSearchClient(httpClient);
        var request = new SearchRequest("test");

        // Act
        var act = async () => await client.SearchAsync(request);

        // Assert
        await act.Should().ThrowAsync<GrepApiTimeoutException>()
            .WithMessage("Request to grep.app API timed out");
    }

    [Fact]
    public async Task SearchAsync_WithNetworkError_ShouldThrowGrepApiException()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://grep.app/api/search?q=test")
            .Throw(new HttpRequestException("Network error"));

        using var httpClient = new HttpClient(mockHttp);
        using var client = new GrepAppSearchClient(httpClient);
        var request = new SearchRequest("test");

        // Act
        var act = async () => await client.SearchAsync(request);

        // Assert
        await act.Should().ThrowAsync<GrepApiException>()
            .WithMessage("Network error while contacting grep.app API*");
    }

    [Fact]
    public async Task SearchAsync_WithCancellationToken_ShouldSupportCancellation()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockHttp.When("https://grep.app/api/search?q=test")
            .Respond("application/json", "{}");

        using var httpClient = new HttpClient(mockHttp);
        using var client = new GrepAppSearchClient(httpClient);
        var request = new SearchRequest("test");

        // Act
        var act = async () => await client.SearchAsync(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Dispose_WhenUsingDefaultConstructor_ShouldDisposeHttpClient()
    {
        // Arrange
        var client = new GrepAppSearchClient();

        // Act & Assert (should not throw)
        client.Dispose();
        client.Dispose(); // Double dispose should not throw
    }

    [Fact]
    public async Task Dispose_WhenUsingProvidedHttpClient_ShouldNotDisposeHttpClient()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var client = new GrepAppSearchClient(httpClient);

        // Act
        client.Dispose();

        // Assert - HttpClient should still be usable
        var act = async () => await httpClient.GetAsync("https://example.com");
        await act.Should().NotThrowAsync();
    }

    private static object CreateMockApiResponse()
    {
        return new
        {
            facets = new
            {
                count = 100,
                lang = new
                {
                    buckets = new[]
                    {
                        new { val = "JavaScript", count = 50 },
                        new { val = "TypeScript", count = 30 }
                    }
                },
                repo = new
                {
                    buckets = new[]
                    {
                        new { val = "example/repo", count = 10 }
                    }
                }
            },
            hits = new
            {
                hits = new[]
                {
                    new
                    {
                        repo = new { raw = "example/repo" },
                        path = new { raw = "src/index.js" },
                        branch = new { raw = "main" },
                        total_matches = new { raw = "5" },
                        content = new
                        {
                            snippet = @"<div data-line=""10"">function test() {</div>
                                      <div data-line=""11"">  console.log('test');</div>
                                      <div data-line=""12"">}</div>"
                        }
                    }
                }
            }
        };
    }
}
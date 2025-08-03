using FluentAssertions;
using Xunit;

namespace Ivy.GrepApp.Tests;

public class SearchRequestTests
{
    [Fact]
    public void Constructor_WithValidQuery_ShouldSetQuery()
    {
        // Arrange & Act
        var request = new SearchRequest("test query");

        // Assert
        request.Query.Should().Be("test query");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyQuery_ShouldThrowArgumentException(string? query)
    {
        // Act
        var act = () => new SearchRequest(query!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithQueryExceeding1000Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var longQuery = new string('a', 1001);

        // Act
        var act = () => new SearchRequest(longQuery);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Query is too long (max 1000 characters)*")
            .And.ParamName.Should().Be("query");
    }

    [Fact]
    public void Constructor_WithExactly1000Characters_ShouldNotThrow()
    {
        // Arrange
        var maxQuery = new string('a', 1000);

        // Act
        var act = () => new SearchRequest(maxQuery);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithValidLanguage_ShouldNotThrow()
    {
        // Arrange
        var request = new SearchRequest("test") { Language = "Python" };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyLanguage_ShouldThrowArgumentException(string language)
    {
        // Arrange
        var request = new SearchRequest("test") { Language = language };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Language parameter must be a non-empty string when provided*")
            .And.ParamName.Should().Be("Language");
    }

    [Fact]
    public void Validate_WithLanguageExceeding50Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new SearchRequest("test") { Language = new string('a', 51) };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Language parameter is too long (max 50 characters)*")
            .And.ParamName.Should().Be("Language");
    }

    [Theory]
    [InlineData("owner/repo")]
    [InlineData("facebook/react")]
    [InlineData("microsoft/vscode")]
    public void Validate_WithValidRepository_ShouldNotThrow(string repository)
    {
        // Arrange
        var request = new SearchRequest("test") { Repository = repository };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyRepository_ShouldThrowArgumentException(string repository)
    {
        // Arrange
        var request = new SearchRequest("test") { Repository = repository };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Repository parameter must be a non-empty string when provided*")
            .And.ParamName.Should().Be("Repository");
    }

    [Theory]
    [InlineData("noSlash")]
    [InlineData("too/many/slashes")]
    [InlineData("owner/repo/extra")]
    public void Validate_WithInvalidRepositoryFormat_ShouldThrowArgumentException(string repository)
    {
        // Arrange
        var request = new SearchRequest("test") { Repository = repository };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Repository parameter must be in format 'owner/repository' (e.g., 'fastapi/fastapi')*")
            .And.ParamName.Should().Be("Repository");
    }

    [Fact]
    public void Validate_WithRepositoryExceeding100Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var longOwner = new string('a', 50);
        var longRepo = new string('b', 51);
        var request = new SearchRequest("test") { Repository = $"{longOwner}/{longRepo}" };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Repository parameter is too long (max 100 characters)*")
            .And.ParamName.Should().Be("Repository");
    }

    [Theory]
    [InlineData("src/")]
    [InlineData("path/to/file.js")]
    [InlineData("tests/unit")]
    public void Validate_WithValidPath_ShouldNotThrow(string path)
    {
        // Arrange
        var request = new SearchRequest("test") { Path = path };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPath_ShouldThrowArgumentException(string path)
    {
        // Arrange
        var request = new SearchRequest("test") { Path = path };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Path parameter must be a non-empty string when provided*")
            .And.ParamName.Should().Be("Path");
    }

    [Fact]
    public void Validate_WithPathExceeding200Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new SearchRequest("test") { Path = new string('a', 201) };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Path parameter is too long (max 200 characters)*")
            .And.ParamName.Should().Be("Path");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_WithValidResultLimit_ShouldNotThrow(int limit)
    {
        // Arrange
        var request = new SearchRequest("test") { ResultLimit = limit };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(1000)]
    public void Validate_WithInvalidResultLimit_ShouldThrowArgumentException(int limit)
    {
        // Arrange
        var request = new SearchRequest("test") { ResultLimit = limit };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("ResultLimit must be between 1 and 100*")
            .And.ParamName.Should().Be("ResultLimit");
    }

    [Fact]
    public void DefaultResultLimit_ShouldBe10()
    {
        // Arrange & Act
        var request = new SearchRequest("test");

        // Assert
        request.ResultLimit.Should().Be(10);
    }

    [Fact]
    public void Validate_WithAllValidOptionalParameters_ShouldNotThrow()
    {
        // Arrange
        var request = new SearchRequest("test query")
        {
            Language = "JavaScript",
            Repository = "nodejs/node",
            Path = "lib/",
            ResultLimit = 25
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNullOptionalParameters_ShouldNotThrow()
    {
        // Arrange
        var request = new SearchRequest("test query")
        {
            Language = null,
            Repository = null,
            Path = null,
            ResultLimit = null
        };

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
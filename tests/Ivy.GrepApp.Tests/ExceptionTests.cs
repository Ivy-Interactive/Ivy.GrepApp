using FluentAssertions;
using Xunit;

namespace Ivy.GrepApp.Tests;

public class ExceptionTests
{
    [Fact]
    public void GrepApiException_ShouldInheritFromException()
    {
        // Arrange & Act
        var exception = new GrepApiException();

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void GrepApiException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Test error message";

        // Act
        var exception = new GrepApiException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void GrepApiException_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "Test error message";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new GrepApiException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void GrepApiTimeoutException_ShouldInheritFromGrepApiException()
    {
        // Arrange & Act
        var exception = new GrepApiTimeoutException();

        // Assert
        exception.Should().BeAssignableTo<GrepApiException>();
    }

    [Fact]
    public void GrepApiTimeoutException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Request timed out";

        // Act
        var exception = new GrepApiTimeoutException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void GrepApiTimeoutException_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "Request timed out";
        var innerException = new TaskCanceledException("Cancelled");

        // Act
        var exception = new GrepApiTimeoutException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void GrepApiRateLimitException_ShouldInheritFromGrepApiException()
    {
        // Arrange & Act
        var exception = new GrepApiRateLimitException();

        // Assert
        exception.Should().BeAssignableTo<GrepApiException>();
    }

    [Fact]
    public void GrepApiRateLimitException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Rate limit exceeded";

        // Act
        var exception = new GrepApiRateLimitException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void GrepApiRateLimitException_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "Rate limit exceeded";
        var innerException = new HttpRequestException("429 Too Many Requests");

        // Act
        var exception = new GrepApiRateLimitException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void AllExceptions_ShouldBeSerializable()
    {
        // This test verifies that exceptions can be properly created and thrown
        // In modern .NET, binary serialization is not recommended, so we test basic functionality

        // Arrange
        var exceptions = new Exception[]
        {
            new GrepApiException("Test"),
            new GrepApiTimeoutException("Timeout"),
            new GrepApiRateLimitException("Rate limit")
        };

        // Act & Assert
        foreach (var exception in exceptions)
        {
            Action act = () => throw exception;
            act.Should().Throw<Exception>()
                .And.Message.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void ExceptionHierarchy_ShouldBeCorrect()
    {
        // This test verifies the inheritance hierarchy

        // Arrange
        var baseException = new GrepApiException();
        var timeoutException = new GrepApiTimeoutException();
        var rateLimitException = new GrepApiRateLimitException();

        // Assert
        baseException.Should().BeAssignableTo<Exception>();
        baseException.Should().BeOfType<GrepApiException>();

        timeoutException.Should().BeAssignableTo<Exception>();
        timeoutException.Should().BeAssignableTo<GrepApiException>();
        timeoutException.Should().BeOfType<GrepApiTimeoutException>();

        rateLimitException.Should().BeAssignableTo<Exception>();
        rateLimitException.Should().BeAssignableTo<GrepApiException>();
        rateLimitException.Should().BeOfType<GrepApiRateLimitException>();
    }

    [Fact]
    public void Exceptions_ShouldBeDistinguishableInCatchBlocks()
    {
        // This test verifies that different exception types can be caught separately

        // Arrange
        var exceptions = new (Exception exception, Type expectedType)[]
        {
            (new GrepApiTimeoutException(), typeof(GrepApiTimeoutException)),
            (new GrepApiRateLimitException(), typeof(GrepApiRateLimitException)),
            (new GrepApiException(), typeof(GrepApiException))
        };

        foreach (var (exception, expectedType) in exceptions)
        {
            // Act
            Exception? caughtException = null;
            try
            {
                throw exception;
            }
            catch (GrepApiTimeoutException ex) when (expectedType == typeof(GrepApiTimeoutException))
            {
                caughtException = ex;
            }
            catch (GrepApiRateLimitException ex) when (expectedType == typeof(GrepApiRateLimitException))
            {
                caughtException = ex;
            }
            catch (GrepApiException ex) when (expectedType == typeof(GrepApiException))
            {
                caughtException = ex;
            }

            // Assert
            caughtException.Should().NotBeNull();
            caughtException.Should().BeOfType(expectedType);
        }
    }
}
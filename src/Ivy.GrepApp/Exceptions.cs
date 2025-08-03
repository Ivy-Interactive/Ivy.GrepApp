using System;

namespace Ivy.GrepApp
{

public class GrepApiException : Exception
{
    public GrepApiException() : base()
    {
    }

    public GrepApiException(string message) : base(message)
    {
    }

    public GrepApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class GrepApiTimeoutException : GrepApiException
{
    public GrepApiTimeoutException() : base()
    {
    }

    public GrepApiTimeoutException(string message) : base(message)
    {
    }

    public GrepApiTimeoutException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class GrepApiRateLimitException : GrepApiException
{
    public GrepApiRateLimitException() : base()
    {
    }

    public GrepApiRateLimitException(string message) : base(message)
    {
    }

    public GrepApiRateLimitException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
}
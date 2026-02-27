namespace WebApp.FunctionInvoker.Apis.Fetch;

public class CountLimitException : Exception
{
}

public class FetchSecurityException(string message) : Exception(message)
{
}

public class FetchSizeLimitException(string message) : Exception(message)
{
}

public class FetchTimeoutException(string message) : Exception(message)
{
}

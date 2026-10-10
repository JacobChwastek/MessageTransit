namespace MessageTransit.Configuration
{
    public delegate IRetryPolicy RetryPolicyFactory(IExceptionFilter filter);
}

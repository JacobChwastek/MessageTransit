namespace MessageTransit.Transports
{
    public delegate bool AllowTransportHeader(HeaderValue<string> headerValue);
}

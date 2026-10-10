namespace MessageTransit
{
    using StackExchange.Redis;


    public delegate IDatabase SelectDatabase(IConnectionMultiplexer multiplexer);
}

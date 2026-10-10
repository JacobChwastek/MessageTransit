namespace MessageTransit;

using System;
using StackExchange.Redis;


public delegate IConnectionMultiplexer RedisConnectionFactory(IServiceProvider provider);

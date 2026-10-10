namespace MessageTransit
{
    using System;


    public interface ITimeoutConfigurator
    {
        TimeSpan Timeout { set; }
    }
}

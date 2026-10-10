namespace MessageTransit
{
    using System;


    public interface ReceiveTransportFaulted :
        ReceiveTransportEvent
    {
        Exception? Exception { get; }
    }
}

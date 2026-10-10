namespace MessageTransit
{
    using System;


    public delegate Guid PendingFutureIdProvider<in T>(T message)
        where T : class;
}

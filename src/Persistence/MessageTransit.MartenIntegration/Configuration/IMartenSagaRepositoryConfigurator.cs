namespace MessageTransit
{
    using System;
    using Marten;


    public interface IMartenSagaRepositoryConfigurator
    {
        [Obsolete("Use AddMarten to configure the connection.")]
        void Connection(string connectionString, Action<StoreOptions> configure = null);
    }


    public interface IMartenSagaRepositoryConfigurator<TSaga> :
        IMartenSagaRepositoryConfigurator
        where TSaga : class, ISaga
    {
    }
}

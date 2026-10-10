namespace MessageTransit
{
    public interface ICosmosCollectionIdFormatter
    {
        string Saga<TSaga>()
            where TSaga : ISaga;
    }
}

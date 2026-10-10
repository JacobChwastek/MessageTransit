namespace MessageTransit
{
    public delegate byte[] PartitionKeyProvider<in TContext>(TContext context);
}

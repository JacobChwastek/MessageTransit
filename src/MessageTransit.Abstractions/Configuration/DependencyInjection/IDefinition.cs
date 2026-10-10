namespace MessageTransit
{
    public interface IDefinition
    {
        int? ConcurrentMessageLimit { get; }
    }
}

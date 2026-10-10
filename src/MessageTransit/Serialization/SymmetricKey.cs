namespace MessageTransit.Serialization
{
    public interface SymmetricKey
    {
        byte[] Key { get; }

        byte[] IV { get; }
    }
}

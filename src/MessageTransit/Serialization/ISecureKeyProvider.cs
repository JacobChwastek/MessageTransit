namespace MessageTransit.Serialization
{
    public interface ISecureKeyProvider :
        IProbeSite
    {
        byte[] GetKey(Headers headers);
    }
}

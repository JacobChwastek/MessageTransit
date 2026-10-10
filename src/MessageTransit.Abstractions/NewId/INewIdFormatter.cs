namespace MessageTransit
{
    public interface INewIdFormatter
    {
        string Format(in byte[] bytes);
    }
}

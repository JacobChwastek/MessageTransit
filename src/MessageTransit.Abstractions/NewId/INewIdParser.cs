namespace MessageTransit
{
    public interface INewIdParser
    {
        NewId Parse(in string text);
    }
}

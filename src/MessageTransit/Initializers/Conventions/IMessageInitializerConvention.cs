namespace MessageTransit.Initializers.Conventions
{
    public interface IMessageInputInitializerConvention<in TMessage>
        where TMessage : class
    {
    }


    public interface IMessageInitializerConvention
    {
    }
}

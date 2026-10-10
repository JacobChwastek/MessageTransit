namespace MessageTransit
{
    using Middleware;


    public interface IPipeConnectorSpecification :
        ISpecification
    {
        void Connect(IPipeConnector connector);
    }
}

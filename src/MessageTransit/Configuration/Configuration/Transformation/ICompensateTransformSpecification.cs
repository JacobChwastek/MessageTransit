namespace MessageTransit.Configuration
{
    public interface ICompensateTransformSpecification<TLog> :
        IPipeSpecification<CompensateContext<TLog>>
        where TLog : class
    {
    }
}

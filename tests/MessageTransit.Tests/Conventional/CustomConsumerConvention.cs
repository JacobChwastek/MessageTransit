namespace MessageTransit.Tests.Conventional
{
    using MessageTransit.Configuration;


    class CustomConsumerConvention :
        IConsumerConvention
    {
        IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<T>()
        {
            return new CustomConsumerMessageConvention<T>();
        }
    }
}

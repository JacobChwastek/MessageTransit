namespace MessageTransit.SqlTransport.Configuration
{
    using System.Collections.Generic;
    using MessageTransit.Configuration;
    using Middleware;


    public class SqlMessageSchedulerSpecification :
        IPipeSpecification<ConsumeContext>
    {
        public void Apply(IPipeBuilder<ConsumeContext> builder)
        {
            builder.AddFilter(new SqlMessageSchedulerFilter());
        }

        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }
    }
}

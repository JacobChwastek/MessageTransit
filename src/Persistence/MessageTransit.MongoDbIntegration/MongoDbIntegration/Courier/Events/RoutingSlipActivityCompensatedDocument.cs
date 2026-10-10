namespace MessageTransit.MongoDbIntegration.Courier.Events
{
    using System;
    using System.Collections.Generic;
    using MessageTransit.Courier.Contracts;


    public class RoutingSlipActivityCompensatedDocument :
        RoutingSlipEventDocument
    {
        public RoutingSlipActivityCompensatedDocument(RoutingSlipActivityCompensated message)
            : base(message.Timestamp, message.Duration, message.Host)
        {
            ActivityName = message.ActivityName;
            ExecutionId = message.ExecutionId;
            Data = message.Data;
        }

        public string ActivityName { get; private set; }
        public Guid ExecutionId { get; private set; }
        public IDictionary<string, object> Data { get; private set; }
    }
}

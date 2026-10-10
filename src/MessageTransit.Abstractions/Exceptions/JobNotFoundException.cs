namespace MessageTransit
{
    using System;


    [Serializable]
    public class JobNotFoundException :
        MessageTransitException
    {
        public JobNotFoundException()
        {
        }

        public JobNotFoundException(string message)
            : base(message)
        {
        }
    }
}

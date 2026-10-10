namespace MessageTransit
{
    using System;


    [Serializable]
    public class ShutDownException :
        MessageTransitException
    {
        public ShutDownException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

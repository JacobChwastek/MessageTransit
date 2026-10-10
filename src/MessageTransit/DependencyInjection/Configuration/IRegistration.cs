namespace MessageTransit.Configuration
{
    using System;


    public interface IRegistration
    {
        Type Type { get; }

        bool IncludeInConfigureEndpoints { get; set; }
    }
}

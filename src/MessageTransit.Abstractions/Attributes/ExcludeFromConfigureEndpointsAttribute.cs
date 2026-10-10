namespace MessageTransit
{
    using System;


    /// <summary>
    /// When added to a consuming type (consumer, saga, activity, etc), prevents
    /// MessageTransit from configuring endpoint for it when ConfigureEndpoints called
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class ExcludeFromConfigureEndpointsAttribute :
        Attribute
    {
    }
}

#nullable enable
namespace MessageTransit
{
    using System;
    using Configuration;
    using Microsoft.Extensions.DependencyInjection;


    public static class MessageTransitHealthCheckOptionsExtensions
    {
        /// <summary>
        /// Configure the health check options for this bus
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static IBusRegistrationConfigurator ConfigureHealthCheckOptions(this IBusRegistrationConfigurator configurator,
            Action<IHealthCheckOptionsConfigurator>? callback)
        {
            configurator.AddOptions<MessageTransitHealthCheckOptions<IBus>>()
                .Configure(options =>
                {
                    callback?.Invoke(options);
                });

            return configurator;
        }

        /// <summary>
        /// Configure the health check options for this bus
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static IBusRegistrationConfigurator<T> ConfigureHealthCheckOptions<T>(this IBusRegistrationConfigurator<T> configurator,
            Action<IHealthCheckOptionsConfigurator>? callback)
            where T : class, IBus
        {
            configurator.AddOptions<MessageTransitHealthCheckOptions<T>>()
                .Configure(options =>
                {
                    callback?.Invoke(options);
                });

            return configurator;
        }
    }
}

namespace MessageTransit.DependencyInjection
{
    using Microsoft.Extensions.Options;


    public class ValidateMessageTransitHostOptions :
        IValidateOptions<MessageTransitHostOptions>
    {
        public ValidateOptionsResult Validate(string name, MessageTransitHostOptions options)
        {
            if (options.StopTimeout < options.ConsumerStopTimeout)
                return ValidateOptionsResult.Fail($"{nameof(options.ConsumerStopTimeout)} should be less than or equals to ${nameof(options.StopTimeout)}");

            return ValidateOptionsResult.Success;
        }
    }
}

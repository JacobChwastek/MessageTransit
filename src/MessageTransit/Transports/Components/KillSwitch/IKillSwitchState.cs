namespace MessageTransit.Transports.Components
{
    public interface IKillSwitchState :
        IConsumeObserver,
        IProbeSite
    {
    }
}

namespace MessageTransit
{
    public delegate TPayload PayloadFactory<out TPayload>()
        where TPayload : class;
}

namespace MessageTransit
{
    public delegate IMessageScheduler MessageSchedulerFactory(ConsumeContext context);
}

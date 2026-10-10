namespace MessageTransit
{
    public interface IWorkerIdProvider
    {
        byte[] GetWorkerId(int index);
    }
}

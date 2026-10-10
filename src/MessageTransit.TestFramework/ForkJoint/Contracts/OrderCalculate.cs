namespace MessageTransit.TestFramework.ForkJoint.Contracts
{
    public interface OrderCalculate :
        OrderLine
    {
        int Number { get; }
    }
}

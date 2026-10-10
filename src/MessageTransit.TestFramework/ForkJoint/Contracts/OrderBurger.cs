namespace MessageTransit.TestFramework.ForkJoint.Contracts
{
    public interface OrderBurger :
        OrderLine
    {
        Burger Burger { get; }
    }
}

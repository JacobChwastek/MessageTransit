namespace MessageTransit.TestFramework.ForkJoint.Contracts
{
    public interface BurgerCompleted :
        OrderLineCompleted
    {
        Burger Burger { get; }
    }
}

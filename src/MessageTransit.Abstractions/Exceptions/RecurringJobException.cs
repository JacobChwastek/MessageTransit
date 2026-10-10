namespace MessageTransit;

using System;


[Serializable]
public class RecurringJobException :
    MessageTransitException
{
    public RecurringJobException()
    {
    }

    public RecurringJobException(string message)
        : base(message)
    {
    }
}

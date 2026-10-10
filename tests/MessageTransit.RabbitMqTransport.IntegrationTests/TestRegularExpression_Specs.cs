namespace MessageTransit.RabbitMqTransport.Tests
{
    using System.Text.RegularExpressions;
    using NUnit.Framework;


    [TestFixture]
    public class TestRegularExpression_Specs
    {
        [Test]
        public void Verify_regex()
        {
            const string stackTrack =
                @"   at MessageTransit.RabbitMqTransport.Tests.A_serialization_exception.<>c.<<ConfigureInputQueueEndpoint>b__13_0>d.MoveNext() in E:\Home\MessageTransit\src\MessageTransit.RabbitMqTransport.Tests\ErrorQueue_Specs.cs:line 129
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.GetResult()
   at MessageTransit.TestFramework.BusTestFixture.<>c__DisplayClass6_0`1.<<Handler>b__0>d.MoveNext() in E:\Home\MessageTransit\src\MessageTransit.TestFramework\BusTestFixture.cs:line 143
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.GetResult()
   at MessageTransit.Pipeline.Filters.HandlerMessageFilter`1.<MessageTransit-Pipeline-IFilter<MessageTransit-ConsumeContext<TMessage>>-Send>d__5.MoveNext() in E:\Home\MessageTransit\src\MessageTransit\Pipeline\Filters\HandlerMessageFilter.cs:line 56
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   at MessageTransit.Pipeline.Filters.HandlerMessageFilter`1.<MessageTransit-Pipeline-IFilter<MessageTransit-ConsumeContext<TMessage>>-Send>d__5.MoveNext() in E:\Home\MessageTransit\src\MessageTransit\Pipeline\Filters\HandlerMessageFilter.cs:line 69
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.GetResult()
   at MessageTransit.Pipeline.Filters.TeeConsumeFilter`1.<>c__DisplayClass7_0.<<Send>b__0>d.MoveNext() in E:\Home\MessageTransit\src\MessageTransit\Pipeline\Filters\TeeConsumeFilter.cs:line 59
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.GetResult()
   at MessageTransit.Pipeline.Filters.TeeConsumeFilter`1.<Send>d__7.MoveNext() in E:\Home\MessageTransit\src\MessageTransit\Pipeline\Filters\TeeConsumeFilter.cs:line 59
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.GetResult()
   at MessageTransit.Pipeline.Filters.MessageConsumeFilter`1.<MessageTransit-Pipeline-IFilter<MessageTransit-ConsumeContext>-Send>d__7.MoveNext() in E:\Home\MessageTransit\src\MessageTransit\Pipeline\Filters\MessageConsumeFilter.cs:line 80
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
   at MessageTransit.Pipeline.Filters.MessageConsumeFilter`1.<MessageTransit-Pipeline-IFilter<MessageTransit-ConsumeContext>-Send>d__7.MoveNext() in E:\Home\MessageTransit\src\MessageTransit\Pipeline\Filters\MessageConsumeFilter.cs:line 95
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.GetResult()
   at MessageTransit.Pipeline.Filters.DeserializeFilter.<Send>d__4.MoveNext() in E:\Home\MessageTransit\src\MessageTransit\Pipeline\Filters\DeserializeFilter.cs:line 48
--- End of stack trace from previous location where exception was thrown ---
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
   at System.Runtime.CompilerServices.TaskAwaiter.GetResult()
   at MessageTransit.Pipeline.Filters.RescueReceiveContextFilter`1.<MessageTransit-Pipeline-IFilter<MessageTransit-ReceiveContext>-Send>d__5.MoveNext() in E:\Home\MessageTransit\src\MessageTransit\Pipeline\Filters\RescueReceiveContextFilter.cs:line 55";

            var cleanup =
                new Regex(
                    @"--- End of stack trace.* ---.*\n\s+(at System\.Runtime\.CompilerServices\.TaskAwaiter.*\s*|at System\.Runtime\.ExceptionServices\.ExceptionDispatchInfo.*\s*)+",
                    RegexOptions.Multiline | RegexOptions.Compiled);

            var result = cleanup.Replace(stackTrack, "");
        }
    }
}

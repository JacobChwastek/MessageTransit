# Moving tests to NUnit 5

`MessageTransit.TestFramework` depends on NUnit 5. Test projects that reference it move to NUnit 5 as well, together with `NUnit3TestAdapter` 6.3.1 or later and a .NET 8 or later target. The tests still run through VSTest (`dotnet test`).

NUnit's [summary of breaking changes](https://docs.nunit.org/articles/nunit/V5BreakingChanges.html) covers every change. These are the ones test projects are most likely to meet:

| Change in NUnit 5 | Effect on tests | What to do |
| --- | --- | --- |
| `Assert.ThrowsAsync`, `Assert.CatchAsync` and `Assert.DoesNotThrowAsync` return a `Task`. | A call that is not awaited never evaluates the assertion, so the test passes whatever the code under test does. | Await each call and make the test `async Task`. NUnit.Analyzers 4.14 or later reports unawaited calls as error NUnit2059 and can fix them. |
| `TestDelegate`, `AsyncTestDelegate` and `ActualValueDelegate<T>` are removed. | Code that names these types no longer compiles. Code that only passes a lambda is unaffected. | Use `Action`, `Func<Task>` and `Func<T>`. |
| A `Func<T>` passed to `Assert.That` as the actual value is called. | The constraint applies to the delegate's result instead of the delegate itself. | Cast the delegate to `object` to assert on the delegate. |
| `Is.SameAs` accepts only reference types. | Comparing value types no longer compiles; it always failed at runtime before. | Use `Is.EqualTo` for value types. |
| `[Platform("NET")]` and `[Platform("DotNET")]` mean modern .NET. | Tests limited to .NET Framework with these names now run on .NET 10 instead. | Use `NETFramework` or `DotNETFramework` for .NET Framework. |
| Async tests run under NUnit's `SafeSynchronizationContext`, which NUnit 4.5 introduced; earlier 4.x releases had none. | An `await` in a test resumes through the context on another thread, not inline on the thread that completed the task. Tests that assert on a background process at the moment an observer fires can see a different state, or never see the state they wait for. | Wait for the condition the assertion needs rather than relying on where the test resumes. Where a test depends on resuming inline, clear the context with `SynchronizationContext.SetSynchronizationContext(null)` at its start. |
| `StringAssert`, `CollectionAssert`, `FileAssert` and `DirectoryAssert` are in the `NUnit.Framework` namespace again. | Aliases that name `NUnit.Framework.Legacy.CollectionAssert` and similar no longer compile. | Remove `using NUnit.Framework.Legacy;` where it was only needed for these classes. |

`MessageTransit.TestFramework` itself has no API changes. Its base fixtures, such as `InMemoryTestFixture`, work unchanged under NUnit 5.

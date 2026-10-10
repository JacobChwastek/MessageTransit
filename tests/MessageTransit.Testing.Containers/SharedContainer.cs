namespace MessageTransit.Testing.Containers;

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;


/// <summary>
/// A container that starts on first use and is shared by every test in the run
/// </summary>
public sealed class SharedContainer<TContainer>
    where TContainer : IContainer
{
    readonly Lazy<Task<TContainer>> _container;

    public SharedContainer(Func<TContainer> build)
    {
        _container = new Lazy<Task<TContainer>>(async () =>
        {
            var container = build();
            SharedContainers.Track(container);
            await container.StartAsync().ConfigureAwait(false);
            return container;
        });
    }

    /// <summary>
    /// The started container; the first access starts it
    /// </summary>
    public TContainer Instance => _container.Value.GetAwaiter().GetResult();

    public string Hostname => Instance.Hostname;

    /// <summary>
    /// Starts the container now, for suites that connect to fixed host ports without reading the container
    /// </summary>
    public void Start()
    {
        _ = Instance;
    }

    public ushort Port(int containerPort)
    {
        return Instance.GetMappedPublicPort(containerPort);
    }
}


/// <summary>
/// Removes every container and network created during the test run, newest first
/// </summary>
public static class SharedContainers
{
    static readonly ConcurrentStack<IAsyncDisposable> Created = new();

    public static void Track(IAsyncDisposable resource)
    {
        Created.Push(resource);
    }

    public static async Task DisposeAsync()
    {
        while (Created.TryPop(out var resource))
            await resource.DisposeAsync().ConfigureAwait(false);
    }
}

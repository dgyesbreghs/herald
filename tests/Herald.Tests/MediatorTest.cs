using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Herald.Tests;

[TestFixture]
public class MediatorTest
{
    private static ServiceProvider BuildProvider() => new ServiceCollection()
        .AddHeraldFromAssemblyContaining<MediatorTest>()
        .BuildServiceProvider();

    [Test]
    public async Task Send_DispatchesToRegisteredHandler()
    {
        await using var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new PingRequest("hello"));

        Assert.That(response, Is.EqualTo("hello pong"));
    }

    [Test]
    public async Task Send_SupportsUnitResponse()
    {
        await using var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new VoidRequest());

        Assert.That(response, Is.EqualTo(Unit.Value));
    }

    [Test]
    public async Task Send_PassesCancellationToken()
    {
        await using var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Assert.ThrowsAsync<TaskCanceledException>(() => mediator.Send(new DelayRequest(), cts.Token));
    }

    [Test]
    public void Send_WithoutRegisteredHandler_Throws()
    {
        using var provider = new ServiceCollection().AddHerald().BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new PingRequest("hello")));
    }

    [Test]
    public void Send_WithNullRequest_Throws()
    {
        using var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Send<string>(null!));
    }

    [Test]
    public void AddHerald_SkipsAbstractAndOpenGenericHandlers()
    {
        var services = new ServiceCollection().AddHeraldFromAssemblyContaining<MediatorTest>();

        Assert.That(services.Any(d => d.ImplementationType == typeof(AbstractHandler)), Is.False);
        Assert.That(services.Any(d => d.ImplementationType == typeof(PingHandler)), Is.True);
    }

    [Test]
    public void AddHerald_CalledTwice_DoesNotDuplicateRegistrations()
    {
        var services = new ServiceCollection()
            .AddHeraldFromAssemblyContaining<MediatorTest>()
            .AddHeraldFromAssemblyContaining<MediatorTest>();

        Assert.That(services.Count(d => d.ServiceType == typeof(IMediator)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ImplementationType == typeof(PingHandler)), Is.EqualTo(1));
    }

    public sealed record PingRequest(string Message) : IRequest<string>;

    public sealed record VoidRequest : IRequest<Unit>;

    public sealed record DelayRequest : IRequest<Unit>;

    public sealed class PingHandler : IRequestHandler<PingRequest, string>
    {
        public Task<string> Handle(PingRequest request, CancellationToken cancellationToken)
            => Task.FromResult($"{request.Message} pong");
    }

    public sealed class VoidHandler : IRequestHandler<VoidRequest, Unit>
    {
        public Task<Unit> Handle(VoidRequest request, CancellationToken cancellationToken) => Unit.Task;
    }

    public sealed class DelayHandler : IRequestHandler<DelayRequest, Unit>
    {
        public async Task<Unit> Handle(DelayRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return Unit.Value;
        }
    }

    public abstract class AbstractHandler : IRequestHandler<PingRequest, string>
    {
        public abstract Task<string> Handle(PingRequest request, CancellationToken cancellationToken);
    }
}

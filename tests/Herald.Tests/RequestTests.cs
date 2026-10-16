using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Herald.Tests;

[TestFixture]
public class RequestTests
{
    [Test]
    public async Task Send_DispatchesToRegisteredHandler()
    {
        await using var provider = TestServices.Build();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new PingRequest("hello"));

        Assert.That(response, Is.EqualTo("hello pong"));
    }

    [Test]
    public async Task Send_SupportsUnitResponse()
    {
        await using var provider = TestServices.Build();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new UnitRequest());

        Assert.That(response, Is.EqualTo(Unit.Value));
    }

    [Test]
    public async Task Send_PassesCancellationToken()
    {
        await using var provider = TestServices.Build();
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
        using var provider = TestServices.Build();
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Send<string>(null!));
    }

    [Test]
    public async Task Send_ThroughISender_DispatchesToRegisteredHandler()
    {
        await using var provider = TestServices.Build();
        var sender = provider.GetRequiredService<ISender>();

        var response = await sender.Send(new PingRequest("hi"));

        Assert.That(response, Is.EqualTo("hi pong"));
    }

    public sealed record PingRequest(string Message) : IRequest<string>;

    public sealed record UnitRequest : IRequest<Unit>;

    public sealed record DelayRequest : IRequest<Unit>;

    public sealed class PingHandler : IRequestHandler<PingRequest, string>
    {
        public Task<string> Handle(PingRequest request, CancellationToken cancellationToken)
            => Task.FromResult($"{request.Message} pong");
    }

    public sealed class UnitHandler : IRequestHandler<UnitRequest, Unit>
    {
        public Task<Unit> Handle(UnitRequest request, CancellationToken cancellationToken) => Unit.Task;
    }

    public sealed class DelayHandler : IRequestHandler<DelayRequest, Unit>
    {
        public async Task<Unit> Handle(DelayRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return Unit.Value;
        }
    }
}

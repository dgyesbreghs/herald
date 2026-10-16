using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Herald.Tests;

[TestFixture]
public class VoidRequestTests
{
    [Test]
    public async Task Send_DispatchesToVoidHandler()
    {
        await using var provider = TestServices.Build();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new DeleteRequest(42));

        Assert.That(provider.GetRequiredService<CallLog>().Entries, Is.EqualTo(new[] { "delete 42" }));
    }

    [Test]
    public void Send_PropagatesHandlerException()
    {
        using var provider = TestServices.Build();
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new FailingRequest()));
    }

    [Test]
    public void Send_WithoutRegisteredHandler_Throws()
    {
        using var provider = new ServiceCollection().AddHerald().BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new DeleteRequest(1)));
    }

    [Test]
    public void Send_WithNullRequest_Throws()
    {
        using var provider = TestServices.Build();
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Send((IRequest)null!));
    }

    public sealed record DeleteRequest(int Id) : IRequest;

    public sealed record FailingRequest : IRequest;

    public sealed class DeleteHandler(CallLog log) : IRequestHandler<DeleteRequest>
    {
        public Task Handle(DeleteRequest request, CancellationToken cancellationToken)
        {
            log.Add($"delete {request.Id}");
            return Task.CompletedTask;
        }
    }

    public sealed class FailingHandler : IRequestHandler<FailingRequest>
    {
        public Task Handle(FailingRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("boom");
    }
}

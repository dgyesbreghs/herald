using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Herald.Tests;

[TestFixture]
public class NotificationTests
{
    [Test]
    public async Task Publish_InvokesAllHandlersInRegistrationOrder()
    {
        await using var provider = TestServices.Build();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new OrderPlaced(7));

        Assert.That(provider.GetRequiredService<CallLog>().Entries, Is.EqualTo(new[] { "email 7", "audit 7" }));
    }

    [Test]
    public async Task Publish_WithoutHandlers_DoesNothing()
    {
        await using var provider = TestServices.Build();
        var publisher = provider.GetRequiredService<IPublisher>();

        await publisher.Publish(new Unhandled());

        Assert.That(provider.GetRequiredService<CallLog>().Entries, Is.Empty);
    }

    [Test]
    public async Task Publish_ResolvesHandlersForRuntimeType()
    {
        await using var provider = TestServices.Build();
        var mediator = provider.GetRequiredService<IMediator>();
        INotification notification = new OrderPlaced(3);

        await mediator.Publish(notification);

        Assert.That(provider.GetRequiredService<CallLog>().Entries, Is.EqualTo(new[] { "email 3", "audit 3" }));
    }

    [Test]
    public async Task Publish_StopsAtFirstFailingHandler()
    {
        await using var provider = TestServices.Build();
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Publish(new Exploded()));
        Assert.That(provider.GetRequiredService<CallLog>().Entries, Is.EqualTo(new[] { "explode" }));
    }

    [Test]
    public void Publish_WithNullNotification_Throws()
    {
        using var provider = TestServices.Build();
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Publish<OrderPlaced>(null!));
    }

    public sealed record OrderPlaced(int OrderId) : INotification;

    public sealed record Unhandled : INotification;

    public sealed record Exploded : INotification;

    public sealed class SendEmailHandler(CallLog log) : INotificationHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
        {
            log.Add($"email {notification.OrderId}");
            return Task.CompletedTask;
        }
    }

    public sealed class WriteAuditHandler(CallLog log) : INotificationHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
        {
            log.Add($"audit {notification.OrderId}");
            return Task.CompletedTask;
        }
    }

    public sealed class ExplodingHandler(CallLog log) : INotificationHandler<Exploded>
    {
        public Task Handle(Exploded notification, CancellationToken cancellationToken)
        {
            log.Add("explode");
            throw new InvalidOperationException("boom");
        }
    }

    public sealed class NeverReachedHandler(CallLog log) : INotificationHandler<Exploded>
    {
        public Task Handle(Exploded notification, CancellationToken cancellationToken)
        {
            log.Add("never");
            return Task.CompletedTask;
        }
    }
}

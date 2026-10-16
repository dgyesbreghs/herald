using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Herald.Tests;

[TestFixture]
public class RegistrationTests
{
    [Test]
    public void AddHerald_RegistersMediatorSenderAndPublisher()
    {
        using var provider = TestServices.Build();

        Assert.That(provider.GetRequiredService<IMediator>(), Is.TypeOf<Mediator>());
        Assert.That(provider.GetRequiredService<ISender>(), Is.TypeOf<Mediator>());
        Assert.That(provider.GetRequiredService<IPublisher>(), Is.TypeOf<Mediator>());
    }

    [Test]
    public void AddHerald_SkipsAbstractHandlers()
    {
        var services = new ServiceCollection().AddHeraldFromAssemblyContaining<RegistrationTests>();

        Assert.That(services.Any(d => d.ImplementationType == typeof(AbstractHandler)), Is.False);
        Assert.That(services.Any(d => d.ImplementationType == typeof(RequestTests.PingHandler)), Is.True);
    }

    [Test]
    public void AddHerald_CalledTwice_DoesNotDuplicateRegistrations()
    {
        var services = new ServiceCollection()
            .AddHerald(c => c.RegisterServicesFromAssemblyContaining<RegistrationTests>().AddOpenBehavior(typeof(PipelineBehaviorTests.OuterBehavior<,>)))
            .AddHerald(c => c.RegisterServicesFromAssemblyContaining<RegistrationTests>().AddOpenBehavior(typeof(PipelineBehaviorTests.OuterBehavior<,>)));

        Assert.That(services.Count(d => d.ServiceType == typeof(IMediator)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ImplementationType == typeof(RequestTests.PingHandler)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ImplementationType == typeof(NotificationTests.SendEmailHandler)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ImplementationType == typeof(PipelineBehaviorTests.OuterBehavior<,>)), Is.EqualTo(1));
    }

    [Test]
    public void AddHerald_RegistersAllNotificationHandlers()
    {
        var services = new ServiceCollection().AddHeraldFromAssemblyContaining<RegistrationTests>();

        Assert.That(
            services.Count(d => d.ServiceType == typeof(INotificationHandler<NotificationTests.OrderPlaced>)),
            Is.EqualTo(2));
    }

    public abstract class AbstractHandler : IRequestHandler<RequestTests.PingRequest, string>
    {
        public abstract Task<string> Handle(RequestTests.PingRequest request, CancellationToken cancellationToken);
    }
}

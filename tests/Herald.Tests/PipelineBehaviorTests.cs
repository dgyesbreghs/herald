using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Herald.Tests;

[TestFixture]
public class PipelineBehaviorTests
{
    [Test]
    public async Task Send_RunsBehaviorsInRegistrationOrderAroundHandler()
    {
        await using var provider = TestServices.Build(c => c
            .AddOpenBehavior(typeof(OuterBehavior<,>))
            .AddOpenBehavior(typeof(InnerBehavior<,>)));
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new EchoRequest("x"));

        Assert.That(response, Is.EqualTo("x"));
        Assert.That(provider.GetRequiredService<CallLog>().Entries, Is.EqualTo(new[]
        {
            "outer before", "inner before", "handler", "inner after", "outer after",
        }));
    }

    [Test]
    public async Task Send_VoidRequest_RunsBehaviorsWithUnitResponse()
    {
        await using var provider = TestServices.Build(c => c.AddOpenBehavior(typeof(OuterBehavior<,>)));
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new FireRequest());

        Assert.That(provider.GetRequiredService<CallLog>().Entries, Is.EqualTo(new[] { "outer before", "fire", "outer after" }));
    }

    [Test]
    public async Task Send_BehaviorCanShortCircuit()
    {
        await using var provider = TestServices.Build(c => c.AddBehavior<CachedEchoBehavior>());
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new EchoRequest("x"));

        Assert.That(response, Is.EqualTo("cached"));
        Assert.That(provider.GetRequiredService<CallLog>().Entries, Is.Empty);
    }

    [Test]
    public async Task Send_ClosedBehavior_OnlyAppliesToItsRequest()
    {
        await using var provider = TestServices.Build(c => c.AddBehavior<CachedEchoBehavior>());
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new FireRequest());

        Assert.That(provider.GetRequiredService<CallLog>().Entries, Is.EqualTo(new[] { "fire" }));
    }

    [Test]
    public async Task Send_ConstrainedOpenBehavior_IsSkippedForNonMatchingRequests()
    {
        await using var provider = TestServices.Build(c => c.AddOpenBehavior(typeof(AuditedOnlyBehavior<,>)));
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new EchoRequest("plain"));
        await mediator.Send(new AuditedRequest());

        Assert.That(provider.GetRequiredService<CallLog>().Entries, Is.EqualTo(new[] { "handler", "audited", "audited handler" }));
    }

    [Test]
    public void AddOpenBehavior_WithNonBehaviorType_Throws()
    {
        var configuration = new HeraldConfiguration();

        Assert.Throws<ArgumentException>(() => configuration.AddOpenBehavior(typeof(List<>)));
        Assert.Throws<ArgumentException>(() => configuration.AddOpenBehavior(typeof(CachedEchoBehavior)));
    }

    [Test]
    public void AddBehavior_WithNonBehaviorType_Throws()
    {
        var configuration = new HeraldConfiguration();

        Assert.Throws<ArgumentException>(() => configuration.AddBehavior<CallLog>());
    }

    public interface IAudited;

    public sealed record EchoRequest(string Value) : IRequest<string>;

    public sealed record FireRequest : IRequest;

    public sealed record AuditedRequest : IRequest<Unit>, IAudited;

    public sealed class EchoHandler(CallLog log) : IRequestHandler<EchoRequest, string>
    {
        public Task<string> Handle(EchoRequest request, CancellationToken cancellationToken)
        {
            log.Add("handler");
            return Task.FromResult(request.Value);
        }
    }

    public sealed class FireHandler(CallLog log) : IRequestHandler<FireRequest>
    {
        public Task Handle(FireRequest request, CancellationToken cancellationToken)
        {
            log.Add("fire");
            return Task.CompletedTask;
        }
    }

    public sealed class AuditedHandler(CallLog log) : IRequestHandler<AuditedRequest, Unit>
    {
        public Task<Unit> Handle(AuditedRequest request, CancellationToken cancellationToken)
        {
            log.Add("audited handler");
            return Unit.Task;
        }
    }

    public sealed class OuterBehavior<TRequest, TResponse>(CallLog log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            log.Add("outer before");
            var response = await next();
            log.Add("outer after");
            return response;
        }
    }

    public sealed class InnerBehavior<TRequest, TResponse>(CallLog log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            log.Add("inner before");
            var response = await next();
            log.Add("inner after");
            return response;
        }
    }

    public sealed class AuditedOnlyBehavior<TRequest, TResponse>(CallLog log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IAudited
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            log.Add("audited");
            return next();
        }
    }

    public sealed class CachedEchoBehavior : IPipelineBehavior<EchoRequest, string>
    {
        public Task<string> Handle(EchoRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            => Task.FromResult("cached");
    }
}

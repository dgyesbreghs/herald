# Herald

[![CI](https://github.com/dgyesbreghs/herald/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/dgyesbreghs/herald/actions/workflows/ci.yml)
[![Publish](https://github.com/dgyesbreghs/herald/actions/workflows/publish.yml/badge.svg)](https://github.com/dgyesbreghs/herald/actions/workflows/publish.yml)
[![NuGet](https://img.shields.io/nuget/v/Herald.svg)](https://www.nuget.org/packages/Herald)
[![NuGet downloads](https://img.shields.io/nuget/dt/Herald.svg)](https://www.nuget.org/packages/Herald)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Minimal in-process mediator for .NET: requests, notifications and pipeline behaviors, resolved via DI.

- **Requests** with or without a response, dispatched to exactly one handler via `ISender.Send`
- **Notifications** published to any number of handlers via `IPublisher.Publish`
- **Pipeline behaviors** to wrap requests in logging, validation, transactions, …
- Handlers resolved from `Microsoft.Extensions.DependencyInjection`
- Familiar API if you're coming from MediatR
- MIT licensed, single dependency (`Microsoft.Extensions.DependencyInjection.Abstractions`)

## Install

Herald is available on [NuGet](https://www.nuget.org/packages/Herald) and targets .NET 8 and later.

Using the .NET CLI:

```bash
dotnet add package Herald
```

Or add a package reference to your project file:

```xml
<PackageReference Include="Herald" Version="2.*" />
```

Or with [central package management](https://learn.microsoft.com/nuget/consume-packages/central-package-management) in `Directory.Packages.props`:

```xml
<PackageVersion Include="Herald" Version="2.*" />
```

## Getting started

Register Herald and every handler in an assembly:

```csharp
builder.Services.AddHerald(herald => herald
    .RegisterServicesFromAssemblyContaining<Program>()
    .AddOpenBehavior(typeof(LoggingBehavior<,>)));

// Or, without behaviors:
builder.Services.AddHeraldFromAssemblyContaining<Program>();
```

Then inject `ISender`, `IPublisher` or `IMediator` (both) where you need them.

A runnable example lives in [`samples/Herald.Samples.WebApi`](samples/Herald.Samples.WebApi): a small todo API using every feature below.

```bash
dotnet run --project samples/Herald.Samples.WebApi
```

## Requests

A request with a response, and its handler:

```csharp
public sealed record GetUserQuery(int Id) : IRequest<UserDto>;

public sealed class GetUserHandler(IUserRepository users) : IRequestHandler<GetUserQuery, UserDto>
{
    public Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
        => users.GetAsync(request.Id, cancellationToken);
}
```

A request without a response:

```csharp
public sealed record DeleteUserCommand(int Id) : IRequest;

public sealed class DeleteUserHandler(IUserRepository users) : IRequestHandler<DeleteUserCommand>
{
    public Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        => users.DeleteAsync(request.Id, cancellationToken);
}
```

Send them with `ISender`:

```csharp
app.MapGet("/users/{id:int}", (int id, ISender sender, CancellationToken ct)
    => sender.Send(new GetUserQuery(id), ct));

app.MapDelete("/users/{id:int}", async (int id, ISender sender, CancellationToken ct) =>
{
    await sender.Send(new DeleteUserCommand(id), ct);
    return Results.NoContent();
});
```

`IRequest<Unit>` with `return Unit.Value;` keeps working too, for code migrated from MediatR.

## Notifications

A notification can have zero or more handlers:

```csharp
public sealed record UserDeleted(int Id) : INotification;

public sealed class SendGoodbyeEmail(IEmailSender email) : INotificationHandler<UserDeleted>
{
    public Task Handle(UserDeleted notification, CancellationToken cancellationToken) => /* ... */;
}

public sealed class WriteAuditLog(IAuditLog audit) : INotificationHandler<UserDeleted>
{
    public Task Handle(UserDeleted notification, CancellationToken cancellationToken) => /* ... */;
}
```

Publish it with `IPublisher`:

```csharp
await publisher.Publish(new UserDeleted(id), cancellationToken);
```

## Pipeline behaviors

Behaviors wrap the handling of a request. Call `next()` to continue to the next behavior or the handler, or return early to short-circuit:

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
        return response;
    }
}
```

Register behaviors in the order they should run; the first one is the outermost:

```csharp
builder.Services.AddHerald(herald => herald
    .RegisterServicesFromAssemblyContaining<Program>()
    .AddOpenBehavior(typeof(LoggingBehavior<,>))     // applies to every request
    .AddOpenBehavior(typeof(ValidationBehavior<,>))  // applies where its generic constraints match
    .AddBehavior<CacheUserBehavior>());              // closed: applies to one request type only
```

An open behavior with a constraint such as `where TRequest : IValidatable` only runs for requests that satisfy it. Requests without a response flow through behaviors with `Unit` as `TResponse`.

## Behaviour

- `IMediator`, `ISender`, `IPublisher`, handlers and behaviors are registered as **transient**.
- Each request type has exactly **one** handler; the first registration wins. Sending a request without a handler throws `InvalidOperationException`.
- Notification handlers run **one after another**, in registration order. Handlers are resolved for the notification's runtime type. The first handler that throws stops the rest. Publishing without handlers is a no-op.
- Abstract and open generic handler types are skipped during scanning. Calling `AddHerald` more than once does not duplicate registrations.

## Upgrading from 1.x

2.0 adds requests without a response, notifications and pipeline behaviors. Code that sends requests or registers Herald needs no changes. The only breaking change: `IMediator` now extends `ISender` and `IPublisher`, so a class that implements `IMediator` itself (e.g. a hand-written test fake) must also implement `Send(IRequest, CancellationToken)` and `Publish`. Mocks created with Moq or NSubstitute are unaffected.

## Migrating from MediatR

1. Replace the `MediatR` package with `Herald`.
2. Replace `using MediatR;` with `using Herald;`.
3. Replace `AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<T>())` with `AddHerald(cfg => cfg.RegisterServicesFromAssemblyContaining<T>())`. `AddOpenBehavior` and `AddBehavior` work the same way.
4. In behaviors, call `next()` instead of MediatR 13's `next(cancellationToken)`; the token is passed to the handler for you.

Not supported: streaming requests, pre/post processors, exception handlers and custom notification publishers.

## Releasing

Publishing a GitHub release (e.g. tag `v2.1.0`) triggers the `Publish` workflow, which tests, packs with the version from the tag and pushes to NuGet via Trusted Publishing.

```bash
gh release create v2.1.0 --generate-notes
```

## License

[MIT](LICENSE)

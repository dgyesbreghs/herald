# Herald

Minimal in-process mediator for .NET. Send requests, resolve handlers via DI, nothing more.

- Request/response dispatch through `IMediator.Send`
- Handlers resolved from `Microsoft.Extensions.DependencyInjection`
- No pipelines, notifications or streaming — just the part most apps actually use
- Familiar API if you're coming from MediatR
- MIT licensed, single dependency (`Microsoft.Extensions.DependencyInjection.Abstractions`)

## Install

```bash
dotnet add package Herald
```

## Usage

Define a request and its handler:

```csharp
using Herald;

public sealed record GetUserRequest(int Id) : IRequest<UserDto>;

public sealed class GetUserHandler(IUserRepository users) : IRequestHandler<GetUserRequest, UserDto>
{
    public async Task<UserDto> Handle(GetUserRequest request, CancellationToken cancellationToken)
        => await users.GetAsync(request.Id, cancellationToken);
}
```

Register Herald and every handler in an assembly:

```csharp
builder.Services.AddHeraldFromAssemblyContaining<GetUserHandler>();
// or: builder.Services.AddHerald(typeof(GetUserHandler).Assembly, typeof(Other).Assembly);
```

Send requests:

```csharp
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:int}")]
    public Task<UserDto> Get(int id, CancellationToken cancellationToken)
        => mediator.Send(new GetUserRequest(id), cancellationToken);
}
```

For requests without a result, return `Unit`:

```csharp
public sealed record DeleteUserRequest(int Id) : IRequest<Unit>;

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserRequest, Unit>
{
    public Task<Unit> Handle(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        // ...
        return Unit.Task;
    }
}
```

## Behaviour

- `IMediator` and handlers are registered as **transient**.
- Each request type has exactly **one** handler; the first registration wins.
- Abstract and open generic handler types are skipped during scanning.
- Sending a request without a registered handler throws `InvalidOperationException`.

## Migrating from MediatR

1. Replace the `MediatR` package with `Herald`.
2. Replace `using MediatR;` with `using Herald;`.
3. Replace `AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<T>())` with `AddHeraldFromAssemblyContaining<T>()`.

Only `IRequest<T>`, `IRequestHandler<TRequest, TResponse>`, `IMediator.Send` and `Unit` are supported.

## Releasing

Publishing a GitHub release (e.g. tag `v1.1.0`) triggers the `Publish` workflow, which tests, packs with the version from the tag and pushes to NuGet via Trusted Publishing.

```bash
gh release create v1.1.0 --generate-notes
```

## License

[MIT](LICENSE)

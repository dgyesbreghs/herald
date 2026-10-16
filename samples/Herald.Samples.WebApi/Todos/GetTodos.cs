namespace Herald.Samples.WebApi.Todos;

/// <summary>A query: a request with a response.</summary>
public sealed record GetTodosQuery : IRequest<IReadOnlyCollection<Todo>>;

public sealed class GetTodosHandler(TodoStore store) : IRequestHandler<GetTodosQuery, IReadOnlyCollection<Todo>>
{
    public Task<IReadOnlyCollection<Todo>> Handle(GetTodosQuery request, CancellationToken cancellationToken)
        => Task.FromResult(store.All);
}

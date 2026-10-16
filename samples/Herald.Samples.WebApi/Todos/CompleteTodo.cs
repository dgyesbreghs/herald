namespace Herald.Samples.WebApi.Todos;

/// <summary>A command without a response.</summary>
public sealed record CompleteTodoCommand(Guid Id) : IRequest;

public sealed class CompleteTodoHandler(TodoStore store, IPublisher publisher) : IRequestHandler<CompleteTodoCommand>
{
    public async Task Handle(CompleteTodoCommand request, CancellationToken cancellationToken)
    {
        var todo = store.Find(request.Id) ?? throw new KeyNotFoundException($"Todo {request.Id} was not found.");

        if (todo.IsCompleted)
        {
            return;
        }

        store.Save(todo with { IsCompleted = true });

        // Let any interested handlers react, without this handler knowing who they are.
        await publisher.Publish(new TodoCompleted(todo.Id, todo.Title), cancellationToken);
    }
}

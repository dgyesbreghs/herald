using Herald.Samples.WebApi.Behaviors;

namespace Herald.Samples.WebApi.Todos;

/// <summary>A command with a response. Implements <see cref="IValidatable"/>, so the validation behavior runs for it.</summary>
public sealed record CreateTodoCommand(string Title) : IRequest<Todo>, IValidatable
{
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return "Title is required.";
        }
        else if (Title.Length > 100)
        {
            yield return "Title must be 100 characters or fewer.";
        }
    }
}

public sealed class CreateTodoHandler(TodoStore store) : IRequestHandler<CreateTodoCommand, Todo>
{
    public Task<Todo> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        var todo = new Todo(Guid.NewGuid(), request.Title.Trim(), IsCompleted: false);
        store.Save(todo);
        return Task.FromResult(todo);
    }
}

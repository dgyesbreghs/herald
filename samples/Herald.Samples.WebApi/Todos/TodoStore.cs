using System.Collections.Concurrent;

namespace Herald.Samples.WebApi.Todos;

/// <summary>An in-memory store standing in for a database.</summary>
public sealed class TodoStore
{
    private readonly ConcurrentDictionary<Guid, Todo> _todos = new();

    public IReadOnlyCollection<Todo> All => _todos.Values.OrderBy(t => t.Title, StringComparer.Ordinal).ToList();

    public Todo? Find(Guid id) => _todos.GetValueOrDefault(id);

    public void Save(Todo todo) => _todos[todo.Id] = todo;
}

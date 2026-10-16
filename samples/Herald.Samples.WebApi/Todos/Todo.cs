namespace Herald.Samples.WebApi.Todos;

public sealed record Todo(Guid Id, string Title, bool IsCompleted);

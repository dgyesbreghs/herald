namespace Herald.Samples.WebApi.Todos;

/// <summary>A notification: published once, handled by every <see cref="INotificationHandler{TNotification}"/>.</summary>
public sealed record TodoCompleted(Guid Id, string Title) : INotification;

public sealed class LogTodoCompletedHandler(ILogger<LogTodoCompletedHandler> logger) : INotificationHandler<TodoCompleted>
{
    public Task Handle(TodoCompleted notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Todo '{Title}' was completed", notification.Title);
        return Task.CompletedTask;
    }
}

public sealed class TodoStatistics
{
    private int _completed;

    public int Completed => _completed;

    public void IncrementCompleted() => Interlocked.Increment(ref _completed);
}

public sealed class UpdateStatisticsHandler(TodoStatistics statistics) : INotificationHandler<TodoCompleted>
{
    public Task Handle(TodoCompleted notification, CancellationToken cancellationToken)
    {
        statistics.IncrementCompleted();
        return Task.CompletedTask;
    }
}

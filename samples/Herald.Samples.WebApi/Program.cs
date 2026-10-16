using Herald;
using Herald.Samples.WebApi;
using Herald.Samples.WebApi.Behaviors;
using Herald.Samples.WebApi.Todos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TodoStore>();
builder.Services.AddSingleton<TodoStatistics>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ExceptionHandler>();

// Scan this assembly for handlers and wrap every request in logging, then validation.
builder.Services.AddHerald(herald => herald
    .RegisterServicesFromAssemblyContaining<Program>()
    .AddOpenBehavior(typeof(LoggingBehavior<,>))
    .AddOpenBehavior(typeof(ValidationBehavior<,>)));

var app = builder.Build();

app.UseExceptionHandler();

var todos = app.MapGroup("/todos");

todos.MapGet("/", (ISender sender, CancellationToken cancellationToken)
    => sender.Send(new GetTodosQuery(), cancellationToken));

todos.MapPost("/", async (CreateTodoCommand command, ISender sender, CancellationToken cancellationToken) =>
{
    var todo = await sender.Send(command, cancellationToken);
    return Results.Created($"/todos/{todo.Id}", todo);
});

todos.MapPost("/{id:guid}/complete", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
{
    await sender.Send(new CompleteTodoCommand(id), cancellationToken);
    return Results.NoContent();
});

app.MapGet("/stats", (TodoStatistics statistics) => new { statistics.Completed });

app.Run();

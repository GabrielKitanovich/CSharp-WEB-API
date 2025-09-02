using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

var app = builder.Build();


app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path} at {DateTime.Now} Started");
    await next(context);
    Console.WriteLine($"Response: {context.Response.StatusCode} at {DateTime.Now} Finished");
});

var todos = new List<Todo>
{
    new(1, "Learn ASP.NET Core", DateTime.Now.AddDays(7), false),
    new(2, "Build a web app", DateTime.Now.AddDays(14), false)
};

app.MapGet("/todos", (ITaskService taskService) => taskService.GetTodos());

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService taskService) =>
{
    var todo = taskService.GetTodoById(id);
    return todo is not null
    ? TypedResults.Ok(todo)
    : TypedResults.NotFound();
});

app.MapPost("/todos", (Todo todo, ITaskService taskService) =>
{
    taskService.AddTodo(todo);
    return TypedResults.Created($"/todos/{todo.Id}", todo);
}).AddEndpointFilter(async (context, next) => {
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();
    if (taskArgument.DueDate < DateTime.UtcNow)
    {
        errors.Add(nameof(Todo.DueDate), ["Cannot have due date in the past."]);
    }
    if (taskArgument.IsCompleted)
    {
        errors.Add(nameof(Todo.IsCompleted), ["Cannot add completed todo."]);
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapDelete("/todos/{id}", Results<NoContent, NotFound> (int id, ITaskService taskService) =>
{
    var todo = taskService.GetTodoById(id);
    if (todo is null)
    {
        return TypedResults.NotFound();
    }
    taskService.DeleteTodoById(id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);

interface ITaskService
{
    Todo? GetTodoById(int id);
    List<Todo> GetTodos();
    void DeleteTodoById(int id);
    Todo AddTodo(Todo todo);
}

class InMemoryTaskService : ITaskService
{
    private readonly List<Todo> _todos = [];

    public Todo AddTodo(Todo task)
    {
        _todos.Add(task);
        return task;
    }

    public void DeleteTodoById(int id)
    {
        _todos.RemoveAll(task => id == task.Id);
    }

    public Todo? GetTodoById(int id)
    {
        return _todos.SingleOrDefault(t => id == t.Id);
    }

    public List<Todo> GetTodos()
    {
        return _todos;
    }
}
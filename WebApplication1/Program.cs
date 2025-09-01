using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var todos = new List<Todo>
{
    new(1, "Learn ASP.NET Core", DateTime.Now.AddDays(7), false),
    new(2, "Build a web app", DateTime.Now.AddDays(14), false)
};

app.MapGet("/todos", () => todos);

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    return todo is not null
    ? TypedResults.Ok(todo)
    : TypedResults.NotFound();
});

app.MapPost("/todos", (Todo todo) =>
{
    todos.Add(todo);
    return TypedResults.Created($"/todos/{todo.Id}", todo);
});

app.MapDelete("/todos/{id}", Results<NoContent, NotFound> (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    if (todo is null)
    {
        return TypedResults.NotFound();
    }
    todos.Remove(todo);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted);
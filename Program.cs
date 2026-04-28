using Npgsql;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var notes = new ConcurrentDictionary<int, Note>();
var nextId = 0;

app.MapGet("/", () => Results.Json(new
{
    message = "IsLabApp is running",
    endpoints = new[]
    {
        "/health",
        "/version",
        "/api/notes",
        "/db/ping"
    }
}));

app.MapGet("/health", () => Results.Json(new
{
    status = "ok",
    time = DateTimeOffset.UtcNow
}));

app.MapGet("/version", (IConfiguration config) => Results.Json(new
{
    app = config["App:Name"] ?? "IsLabApp",
    version = config["App:Version"] ?? "0.0.0"
}));

app.MapPost("/api/notes", (CreateNoteRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new
        {
            error = "Title is required"
        });
    }

    if (request.Title.Length > 100)
    {
        return Results.BadRequest(new
        {
            error = "Title must be 100 characters or less"
        });
    }

    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new
        {
            error = "Text is required"
        });
    }

    var id = Interlocked.Increment(ref nextId);

    var note = new Note(
        Id: id,
        Title: request.Title.Trim(),
        Text: request.Text.Trim(),
        CreatedAt: DateTimeOffset.UtcNow
    );

    notes[id] = note;

    return Results.Created($"/api/notes/{id}", note);
});

app.MapGet("/api/notes", () =>
{
    var result = notes.Values
        .OrderBy(note => note.Id)
        .ToList();

    return Results.Json(result);
});

app.MapGet("/api/notes/{id:int}", (int id) =>
{
    if (!notes.TryGetValue(id, out var note))
    {
        return Results.NotFound(new
        {
            error = $"Note with id {id} was not found"
        });
    }

    return Results.Json(note);
});

app.MapDelete("/api/notes/{id:int}", (int id) =>
{
    if (!notes.TryRemove(id, out _))
    {
        return Results.NotFound(new
        {
            error = $"Note with id {id} was not found"
        });
    }

    return Results.NoContent();
});

app.MapGet("/db/ping", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("Postgresql");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Connection string 'Postgresql' is not configured.");
    }

    try
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        return Results.Json(new
        {
            status = "ok",
            database = "postgresql"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "error",
            database = "postgresql",
            message = ex.Message
        });
    }
});

app.Run();

record Note(int Id, string Title, string Text, DateTimeOffset CreatedAt);

record CreateNoteRequest(string Title, string Text);
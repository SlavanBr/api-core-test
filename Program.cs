using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация: appsettings.json -> переменные окружения (ConnectionStrings__Default и т.д.)
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(5)));

builder.Services.AddOpenApi();

var app = builder.Build();

// Swagger включается в Development или флагом Swagger__Enabled=true
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.MapOpenApi();                       // /openapi/v1.json
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "TodoApi v1");
        o.RoutePrefix = "swagger";          // /swagger
    });
}

// Создаём схему БД при старте (для простоты; для продакшена лучше миграции, см. README)
if (app.Configuration.GetValue("Database:AutoCreate", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Liveness — приложение живо; Readiness — есть связь с БД
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/ready", async (AppDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

var api = app.MapGroup("/api/todos").WithTags("Todos");

api.MapGet("/", async (AppDbContext db) =>
    await db.Todos.AsNoTracking().OrderBy(t => t.Id).ToListAsync());

api.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.Todos.FindAsync(id) is { } todo ? Results.Ok(todo) : Results.NotFound());

api.MapPost("/", async (CreateTodo input, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200)
        return Results.BadRequest(new { error = "Title is required (max 200 chars)" });

    var todo = new Todo { Title = input.Title.Trim() };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/api/todos/{todo.Id}", todo);
});

api.MapPut("/{id:int}", async (int id, UpdateTodo input, AppDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();

    if (!string.IsNullOrWhiteSpace(input.Title)) todo.Title = input.Title.Trim();
    todo.IsDone = input.IsDone;
    await db.SaveChangesAsync();
    return Results.Ok(todo);
});

api.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();

    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

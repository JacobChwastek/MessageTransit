using MessageTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReleaseSmoke;


await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<SmokeDbContext>().UseSqlite(connection).Options;
await using var context = new SmokeDbContext(options);
await context.Database.EnsureCreatedAsync();
var id = NewId.NextGuid();
context.Add(new PersistenceState { CorrelationId = id, CurrentState = "Accepted", Description = "persisted package saga" });
await context.SaveChangesAsync();
context.ChangeTracker.Clear();
var state = await context.Set<PersistenceState>().SingleAsync(x => x.CorrelationId == id);
var model = context.Model.FindEntityType(typeof(PersistenceState))!;
var checks = new Dictionary<string, bool>
{
    ["EF Core SQLite saga round trip"] = state.CorrelationId == id && state.CurrentState == "Accepted" && state.Description == "persisted package saga",
    ["EF Core application assigned correlation key"] = model.FindProperty(nameof(PersistenceState.CorrelationId))!.ValueGenerated == ValueGenerated.Never,
};
foreach (var (name, passed) in checks)
    Console.WriteLine($"{(passed ? "pass" : "FAIL")}\t{name}");
return checks.Values.All(value => value) ? 0 : 1;

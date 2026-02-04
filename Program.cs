using FiscalisationService.Models;
using FiscalisationService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5000");
builder.Host.UseWindowsService();

builder.Services.AddSingleton<ConfigStore>();
builder.Services.AddSingleton<SqlRepository>();
builder.Services.AddSingleton<ConfigPageRenderer>();
builder.Services.AddSingleton<EmailNotifier>();
builder.Services.AddSingleton<ServiceStats>();
builder.Services.AddHttpClient<FiscalApiClient>();
builder.Services.AddHostedService<FiscalWorker>();

var app = builder.Build();

app.MapGet("/", (ConfigStore store, ServiceStats stats) =>
{
    var config = store.Current;
    var snapshot = stats.Snapshot();
    var html = $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Fiscalisation Service</title>
  <style>
    body { font-family: 'Segoe UI', Tahoma, sans-serif; margin: 32px; background: #f7f4ef; }
    .card { background: #fff; padding: 20px; border-radius: 12px; box-shadow: 0 8px 24px rgba(0,0,0,0.08); max-width: 720px; }
    a { color: #1d6b5f; }
  </style>
</head>
<body>
  <div class="card">
    <h1>Fiscalisation Service</h1>
    <p>Table: <strong>{{System.Net.WebUtility.HtmlEncode(config.TableName)}}</strong></p>
    <p>API URL: <strong>{{System.Net.WebUtility.HtmlEncode(config.ApiUrl)}}</strong></p>
    <p>Polling every <strong>{{config.PollIntervalSeconds}}</strong> seconds</p>
    <p>Last batch: <strong>{{snapshot.LastBatchCount}}</strong></p>
    <p>Last run (UTC): <strong>{{snapshot.LastRunUtc}}</strong></p>
    <p>Last success (UTC): <strong>{{snapshot.LastSuccessUtc}}</strong></p>
    <p>Last error (UTC): <strong>{{snapshot.LastErrorUtc}}</strong></p>
    <p>Last error: <strong>{{System.Net.WebUtility.HtmlEncode(snapshot.LastErrorMessage ?? "")}}</strong></p>
    <p>Total processed: <strong>{{snapshot.TotalProcessed}}</strong></p>
    <p>Total success: <strong>{{snapshot.TotalSuccess}}</strong></p>
    <p>Total timeouts: <strong>{{snapshot.TotalTimeout}}</strong></p>
    <p>Total failed: <strong>{{snapshot.TotalFailed}}</strong></p>
    <p><a href="/config">Open configuration</a></p>
  </div>
</body>
</html>
""";
    return Results.Content(html, "text/html");
});

app.MapGet("/health", () => Results.Ok(new { status = "ok", timeUtc = DateTimeOffset.UtcNow }));

app.MapGet("/metrics", (ServiceStats stats) =>
{
    var snapshot = stats.Snapshot();
    var lines = new[]
    {
        $"last_run_utc {snapshot.LastRunUtc?.ToUnixTimeSeconds() ?? 0}",
        $"last_success_utc {snapshot.LastSuccessUtc?.ToUnixTimeSeconds() ?? 0}",
        $"last_error_utc {snapshot.LastErrorUtc?.ToUnixTimeSeconds() ?? 0}",
        $"last_batch_count {snapshot.LastBatchCount}",
        $"total_processed {snapshot.TotalProcessed}",
        $"total_success {snapshot.TotalSuccess}",
        $"total_timeouts {snapshot.TotalTimeout}",
        $"total_failed {snapshot.TotalFailed}"
    };
    return Results.Text(string.Join(Environment.NewLine, lines), "text/plain");
});

app.MapGet("/config", (HttpRequest request, ConfigStore store, ConfigPageRenderer renderer) =>
{
    var saved = request.Query.ContainsKey("saved");
    var html = renderer.Render(store.Current, saved);
    return Results.Content(html, "text/html");
});

app.MapPost("/config", async (HttpRequest request, ConfigStore store) =>
{
    var form = await request.ReadFormAsync();
    var updated = ServiceConfig.FromForm(form, store.Current);
    store.Save(updated);
    return Results.Redirect("/config?saved=1");
});

app.Run();

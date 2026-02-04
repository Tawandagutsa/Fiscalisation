using FiscalisationService.Models;
using FiscalisationService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5000");
builder.Host.UseWindowsService();

builder.Services.AddSingleton<ConfigStore>();
builder.Services.AddSingleton<SqlRepository>();
builder.Services.AddSingleton<ConfigPageRenderer>();
builder.Services.AddSingleton<EmailNotifier>();
builder.Services.AddHttpClient<FiscalApiClient>();
builder.Services.AddHostedService<FiscalWorker>();

var app = builder.Build();

app.MapGet("/", (ConfigStore store) =>
{
    var config = store.Current;
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
    <p><a href="/config">Open configuration</a></p>
  </div>
</body>
</html>
""";
    return Results.Content(html, "text/html");
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

using System.Text.Json;
using FiscalisationService.Models;

namespace FiscalisationService.Services;

public sealed class ConfigStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _serializerOptions;
    private ServiceConfig _current;
    private readonly object _lock = new();

    public ConfigStore(IHostEnvironment environment)
    {
        _filePath = Path.Combine(environment.ContentRootPath, "config.json");
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        _current = LoadInternal() ?? new ServiceConfig();
    }

    public ServiceConfig Current
    {
        get
        {
            lock (_lock)
            {
                return _current.Clone();
            }
        }
    }

    public void Save(ServiceConfig config)
    {
        lock (_lock)
        {
            _current = config.Clone();
            var json = JsonSerializer.Serialize(_current, _serializerOptions);
            File.WriteAllText(_filePath, json);
        }
    }

    public ServiceConfig? Load()
    {
        lock (_lock)
        {
            return _current.Clone();
        }
    }

    private ServiceConfig? LoadInternal()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<ServiceConfig>(json, _serializerOptions);
    }
}

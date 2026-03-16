using System.Text.Json;

namespace FloatBrowser.App.Infrastructure;

public interface IJsonFileStorage
{
    Task<T?> ReadAsync<T>(string path);
    Task WriteAsync<T>(string path, T value);
}

public class JsonFileStorage : IJsonFileStorage
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public async Task<T?> ReadAsync<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    public async Task WriteAsync<T>(string path, T value)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(value, Options);
        await File.WriteAllTextAsync(path, json);
    }
}

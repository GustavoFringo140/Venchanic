using System.Text.Json;

namespace Venchanic.Core;

public sealed class StorageService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public AppState Load()
    {
        try
        {
            RuntimePaths.EnsureRuntimeDirectories();

            if (!File.Exists(RuntimePaths.StateFilePath))
            {
                return new AppState();
            }

            var json = File.ReadAllText(RuntimePaths.StateFilePath);
            return JsonSerializer.Deserialize<AppState>(json, JsonSerializerOptions) ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public void Save(AppState state)
    {
        RuntimePaths.EnsureRuntimeDirectories();

        var json = JsonSerializer.Serialize(state, JsonSerializerOptions);
        File.WriteAllText(RuntimePaths.StateFilePath, json);
    }
}

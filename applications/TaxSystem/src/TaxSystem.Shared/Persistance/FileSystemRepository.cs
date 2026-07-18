using System.Text.Json;

namespace TaxSystem.Shared.Persistance;

public class FileSystemRepository
{
    private const string DataPathEnvironmentVariable = "TAXSYSTEM_DATA_PATH";
    private readonly string _repositoryPath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public FileSystemRepository(string subPath, string? basePath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subPath);

        if (Path.IsPathRooted(subPath) || subPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part is ".."))
        {
            throw new ArgumentException("The repository path must be a relative directory without parent traversal.", nameof(subPath));
        }

        var rootPath = basePath
            ?? Environment.GetEnvironmentVariable(DataPathEnvironmentVariable)
            ?? AppDomain.CurrentDomain.BaseDirectory;

        _repositoryPath = Path.GetFullPath(Path.Combine(rootPath, subPath));
        Directory.CreateDirectory(_repositoryPath);
    }

    public void Save<T>(string key, T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var filePath = GetFilePath(key);
        var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            using (var stream = File.Create(temporaryPath))
            {
                JsonSerializer.Serialize(stream, value, _jsonOptions);
            }

            File.Move(temporaryPath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public T? Get<T>(string key)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return default;
        }

        using var stream = File.OpenRead(filePath);
        return JsonSerializer.Deserialize<T>(stream, _jsonOptions);
    }

    public IReadOnlyList<T> GetAll<T>()
    {
        return Directory.EnumerateFiles(_repositoryPath, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path =>
            {
                using var stream = File.OpenRead(path);
                return JsonSerializer.Deserialize<T>(stream, _jsonOptions)
                    ?? throw new InvalidDataException($"The repository file '{path}' contains a null value.");
            })
            .ToList();
    }

    public bool Delete(string key)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return false;
        }

        File.Delete(filePath);
        return true;
    }

    public bool Exists(string key) => File.Exists(GetFilePath(key));

    private string GetFilePath(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || key is "." or "..")
        {
            throw new ArgumentException("The repository key must be a valid file name.", nameof(key));
        }

        return Path.Combine(_repositoryPath, $"{key}.json");
    }
}
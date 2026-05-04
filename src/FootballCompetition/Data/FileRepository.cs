using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FootballCompetition.Data;

/// <summary>
/// Generic JSON-backed repository.
/// </summary>
/// <remarks>
/// The repository keeps the entire collection in memory (<see cref="_items"/>)
/// and only writes to disk when <see cref="SaveChanges"/> is called or when
/// individual mutating operations explicitly flush. This keeps the UI snappy
/// and lets us batch writes without hammering the filesystem.
/// <para/>
/// Any <see cref="IOException"/> or <see cref="JsonException"/> is wrapped in
/// a <see cref="DataAccessException"/> so the UI can surface a single,
/// well-typed error to the user without leaking implementation details.
/// </remarks>
public class FileRepository<T> where T : class, IKeyedEntity
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _filePath;
    private readonly List<T> _items;

    public FileRepository(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _items = Load();
    }

    /// <summary>Path to the JSON file backing this repository.</summary>
    public string FilePath => _filePath;

    public IEnumerable<T> GetAll() => _items.ToList();

    public T? GetByKey(Guid key) => _items.FirstOrDefault(i => i.Key == key);

    public void Add(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity.Key == Guid.Empty)
        {
            entity.Key = Guid.NewGuid();
        }
        _items.Add(entity);
        SaveChanges();
    }

    public void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var idx = _items.FindIndex(i => i.Key == entity.Key);
        if (idx < 0)
        {
            throw new DataAccessException(
                $"Entity with key {entity.Key} was not found and cannot be updated.",
                _filePath);
        }
        _items[idx] = entity;
        SaveChanges();
    }

    public void Delete(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DeleteByKey(entity.Key);
    }

    public void DeleteByKey(Guid key)
    {
        var removed = _items.RemoveAll(i => i.Key == key);
        if (removed == 0)
        {
            throw new DataAccessException(
                $"No entity with key {key} was found to delete.",
                _filePath);
        }
        SaveChanges();
    }

    /// <summary>
    /// Reload the collection from disk, discarding any in-memory edits that
    /// have not been persisted via <see cref="SaveChanges"/>.
    /// </summary>
    public void Reload()
    {
        var fresh = Load();
        _items.Clear();
        _items.AddRange(fresh);
    }

    public void SaveChanges()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Serialise to a temp file first, then move into place. This makes
            // the write effectively atomic so we never leave a half-written
            // file on disk if the process is killed mid-flush.
            var tmp = _filePath + ".tmp";
            using (var stream = File.Create(tmp))
            {
                JsonSerializer.Serialize(stream, _items, JsonOptions);
            }

            if (File.Exists(_filePath))
            {
                File.Replace(tmp, _filePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tmp, _filePath);
            }
        }
        catch (JsonException ex)
        {
            throw new DataAccessException(
                $"Failed to serialise data to '{_filePath}': {ex.Message}",
                _filePath, ex);
        }
        catch (IOException ex)
        {
            throw new DataAccessException(
                $"Failed to write data file '{_filePath}': {ex.Message}",
                _filePath, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new DataAccessException(
                $"Access denied when writing '{_filePath}': {ex.Message}",
                _filePath, ex);
        }
    }

    private List<T> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new List<T>();
            }

            using var stream = File.OpenRead(_filePath);
            if (stream.Length == 0)
            {
                return new List<T>();
            }

            var items = JsonSerializer.Deserialize<List<T>>(stream, JsonOptions);
            return items ?? new List<T>();
        }
        catch (JsonException ex)
        {
            throw new DataAccessException(
                $"Data file '{_filePath}' is corrupt or malformed: {ex.Message}",
                _filePath, ex);
        }
        catch (IOException ex)
        {
            throw new DataAccessException(
                $"Failed to read data file '{_filePath}': {ex.Message}",
                _filePath, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new DataAccessException(
                $"Access denied when reading '{_filePath}': {ex.Message}",
                _filePath, ex);
        }
    }
}

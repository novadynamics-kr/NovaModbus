using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NovaModbus.ClientWPF.ViewModels;

namespace NovaModbus.ClientWPF.Services;

/// <summary>JSON persistence for watch lists.</summary>
public static class WatchListStore
{
    /// <summary>One file entry.</summary>
    /// <param name="SlaveId">1-247.</param>
    /// <param name="Area">Watch area.</param>
    /// <param name="Start">Start address.</param>
    /// <param name="Count">Quantity.</param>
    /// <param name="Extra">Area-specific text.</param>
    public sealed record WatchFileItem(byte SlaveId, WatchArea Area, ushort Start, ushort Count, string Extra);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Save entries to a JSON file.</summary>
    public static void Save(string path, IEnumerable<WatchEntry> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(entries);
        List<WatchFileItem> items = entries
            .Select(e => new WatchFileItem(e.SlaveId, e.Area, e.Start, e.Count, e.Extra))
            .ToList();
        File.WriteAllText(path, JsonSerializer.Serialize(items, Options));
    }

    /// <summary>Load entries from a JSON file.</summary>
    public static WatchEntry[] Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        List<WatchFileItem>? items = JsonSerializer.Deserialize<List<WatchFileItem>>(File.ReadAllText(path), Options);
        if (items is null)
        {
            throw new InvalidDataException($"Invalid watch list: '{path}'.");
        }

        return items.Select(e => new WatchEntry(e.SlaveId, e.Area, e.Start, e.Count, e.Extra ?? string.Empty)).ToArray();
    }
}

using System.IO;
using System.Text.Json;

namespace NovaModbus.Slave.Services;

/// <summary>JSON persistence for user register maps (per slave ID).</summary>
public static class RegisterMapStore
{
    /// <summary>One holding entry.</summary>
    /// <param name="Address">Register address.</param>
    /// <param name="Value">Initial value.</param>
    public sealed record HoldingMapItem(ushort Address, ushort Value);

    /// <summary>One coil entry.</summary>
    /// <param name="Address">Coil address.</param>
    /// <param name="IsOn">Initial state.</param>
    public sealed record CoilMapItem(ushort Address, bool IsOn);

    /// <summary>File content.</summary>
    /// <param name="Holding">Holding entries.</param>
    /// <param name="Coils">Coil entries.</param>
    public sealed record RegisterMapFile(List<HoldingMapItem> Holding, List<CoilMapItem> Coils);

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>Save maps to a JSON file.</summary>
    public static void Save(string path, IReadOnlyList<HoldingMapItem> holding, IReadOnlyList<CoilMapItem> coils)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(holding);
        ArgumentNullException.ThrowIfNull(coils);
        File.WriteAllText(path, JsonSerializer.Serialize(new RegisterMapFile([.. holding], [.. coils]), Options));
    }

    /// <summary>Load maps from a JSON file.</summary>
    public static RegisterMapFile Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        RegisterMapFile? file = JsonSerializer.Deserialize<RegisterMapFile>(File.ReadAllText(path), Options);
        return file is null ? throw new InvalidDataException($"Invalid register map: '{path}'.") : file;
    }
}

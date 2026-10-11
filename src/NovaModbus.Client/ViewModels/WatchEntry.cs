namespace NovaModbus.Client.ViewModels;

/// <summary>One polled address range.</summary>
/// <param name="SlaveId">1-247.</param>
/// <param name="Area">Register/coil area.</param>
/// <param name="Start">Start address (area-specific: sub-function, file, pointer).</param>
/// <param name="Count">Quantity (area-specific: record number for files).</param>
/// <param name="Extra">Area-specific text (diagnostic data bytes, file length).</param>
public sealed record WatchEntry(byte SlaveId, WatchArea Area, ushort Start, ushort Count, string Extra = "")
{
    /// <summary>Start address as hex (e.g. 0x000A).</summary>
    public string AddressHex => $"0x{Start:X4}";
}

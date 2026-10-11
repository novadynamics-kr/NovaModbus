namespace NovaModbus.Client.ViewModels;

/// <summary>One polled address range.</summary>
/// <param name="SlaveId">1-247.</param>
/// <param name="Area">Register/coil area.</param>
/// <param name="Start">Start address.</param>
/// <param name="Count">Quantity (area limit applies).</param>
public sealed record WatchEntry(byte SlaveId, WatchArea Area, ushort Start, ushort Count);

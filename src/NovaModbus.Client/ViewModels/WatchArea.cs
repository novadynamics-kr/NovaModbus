namespace NovaModbus.Client.ViewModels;

/// <summary>Watchable Modbus areas.</summary>
public enum WatchArea
{
    /// <summary>Holding registers (FC03).</summary>
    HoldingRegisters,

    /// <summary>Input registers (FC04).</summary>
    InputRegisters,

    /// <summary>Coils (FC01).</summary>
    Coils,

    /// <summary>Discrete inputs (FC02).</summary>
    DiscreteInputs,
}

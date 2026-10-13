namespace NovaModbus.Client.Wpf.ViewModels;

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

    /// <summary>Exception status (FC07).</summary>
    ExceptionStatus,

    /// <summary>Diagnostics counter (FC08, Start = sub-function).</summary>
    DiagnosticsCounter,

    /// <summary>Comm event counter (FC0B).</summary>
    CommEventCounter,

    /// <summary>Comm event log (FC0C).</summary>
    CommEventLog,

    /// <summary>Server ID (FC11).</summary>
    ServerId,

    /// <summary>File record (FC14, Start = file, Count = record, Extra = length).</summary>
    FileRecord,

    /// <summary>FIFO queue (FC18, Start = pointer).</summary>
    FifoQueue,
}

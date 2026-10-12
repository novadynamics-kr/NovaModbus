namespace NovaModbus.Rtu.Transport;

/// <summary>Line-level receive errors reported by a serial transport.</summary>
[Flags]
public enum SerialLineError
{
    /// <summary>No error.</summary>
    None = 0,

    /// <summary>Parity check failed on a received character.</summary>
    Parity = 1,

    /// <summary>Framing (stop-bit) error on a received character.</summary>
    Frame = 2,

    /// <summary>Character overrun: data arrived faster than it could be stored.</summary>
    Overrun = 4,
}

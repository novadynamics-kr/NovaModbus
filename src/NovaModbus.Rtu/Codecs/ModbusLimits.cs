namespace NovaModbus.Rtu.Codecs;

/// <summary>Modbus PDU limits from the specification (phase 1 functions).</summary>
public static class ModbusLimits
{
    /// <summary>Max read bits per request (FC01, FC02).</summary>
    public const int MaxReadBits = 2000;

    /// <summary>Max read registers per request (FC03, FC04).</summary>
    public const int MaxReadRegisters = 125;

    /// <summary>Max coils per FC15 request.</summary>
    public const int MaxWriteCoils = 1968;

    /// <summary>Max registers per FC16 request.</summary>
    public const int MaxWriteRegisters = 123;

    /// <summary>Max address value (0xFFFF).</summary>
    public const int MaxAddress = 0xFFFF;
}

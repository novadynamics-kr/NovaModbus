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

    /// <summary>Max file-record sub-requests per FC14/15 request.</summary>
    public const int MaxFileSubRequests = 35;

    /// <summary>Max registers per FC14/15 file sub-request (read).</summary>
    public const int MaxFileReadLength = 125;

    /// <summary>Max registers per FC15 file sub-request (write).</summary>
    public const int MaxFileWriteLength = 120;

    /// <summary>Max record number (0x270F).</summary>
    public const int MaxFileRecordNumber = 0x270F;

    /// <summary>Max FIFO queue values per FC18 response.</summary>
    public const int MaxFifoCount = 31;

    /// <summary>Max event bytes per FC0C response.</summary>
    public const int MaxEventLogBytes = 64;

    /// <summary>Max address value (0xFFFF).</summary>
    public const int MaxAddress = 0xFFFF;

    /// <summary>Max bulk-add item count (UI range operations).</summary>
    public const int MaxBulkAdd = 65535;
}

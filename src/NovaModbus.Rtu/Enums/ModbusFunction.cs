namespace NovaModbus.Rtu.Enums;

/// <summary>Modbus function codes (serial-line set).</summary>
public enum ModbusFunction : byte
{
    /// <summary>Read coils.</summary>
    ReadCoils = 0x01,

    /// <summary>Read discrete inputs.</summary>
    ReadDiscreteInputs = 0x02,

    /// <summary>Read holding registers.</summary>
    ReadHoldingRegisters = 0x03,

    /// <summary>Read input registers.</summary>
    ReadInputRegisters = 0x04,

    /// <summary>Write single coil.</summary>
    WriteSingleCoil = 0x05,

    /// <summary>Write single register.</summary>
    WriteSingleRegister = 0x06,

    /// <summary>Read exception status.</summary>
    ReadExceptionStatus = 0x07,

    /// <summary>Diagnostics.</summary>
    Diagnostics = 0x08,

    /// <summary>Get comm event counter.</summary>
    GetCommEventCounter = 0x0B,

    /// <summary>Get comm event log.</summary>
    GetCommEventLog = 0x0C,

    /// <summary>Write multiple coils.</summary>
    WriteMultipleCoils = 0x0F,

    /// <summary>Write multiple registers.</summary>
    WriteMultipleRegisters = 0x10,

    /// <summary>Report server ID.</summary>
    ReportServerId = 0x11,

    /// <summary>Read file record.</summary>
    ReadFileRecord = 0x14,

    /// <summary>Write file record.</summary>
    WriteFileRecord = 0x15,

    /// <summary>Mask write register.</summary>
    MaskWriteRegister = 0x16,

    /// <summary>Read/write multiple registers.</summary>
    ReadWriteMultipleRegisters = 0x17,

    /// <summary>Read FIFO queue.</summary>
    ReadFifoQueue = 0x18,
}

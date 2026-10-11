namespace NovaModbus.Rtu.Errors;

/// <summary>Base Modbus protocol error.</summary>
public class ModbusException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    public ModbusException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with inner error.</summary>
    public ModbusException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>CRC mismatch on a received frame.</summary>
public sealed class CrcMismatchException : ModbusException
{
    /// <summary>Initializes a new instance.</summary>
    public CrcMismatchException(string message)
        : base(message)
    {
    }
}

/// <summary>Slave returned a Modbus exception response.</summary>
public sealed class ModbusSlaveException : ModbusException
{
    /// <summary>Gets the exception code byte.</summary>
    public byte ExceptionCode { get; }

    /// <summary>Initializes a new instance.</summary>
    public ModbusSlaveException(byte exceptionCode)
        : base($"Slave exception 0x{exceptionCode:X2}.")
    {
        ExceptionCode = exceptionCode;
    }
}

namespace NovaModbus.Rtu.Errors;

/// <summary>Base Modbus protocol error.</summary>
public class ModbusException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">Error message describing the protocol failure.</param>
    public ModbusException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with inner error.</summary>
    /// <param name="message">Error message describing the protocol failure.</param>
    /// <param name="inner">Underlying exception that caused this error.</param>
    public ModbusException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>CRC mismatch on a received frame.</summary>
public sealed class CrcMismatchException : ModbusException
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">Error message describing the CRC failure.</param>
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
    /// <param name="exceptionCode">Modbus exception code byte returned by the slave.</param>
    public ModbusSlaveException(byte exceptionCode)
        : base($"Slave exception 0x{exceptionCode:X2}.")
    {
        ExceptionCode = exceptionCode;
    }
}

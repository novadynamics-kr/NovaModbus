using NovaModbus.Rtu.Enums;

namespace NovaModbus.Rtu.Codecs;

/// <summary>Modbus exception response body: [code(1)]. Function byte carries 0x80 flag.</summary>
public static class ExceptionCodec
{
    /// <summary>Encode exception body for the given code.</summary>
    public static byte[] Encode(ModbusExceptionCode code) => [(byte)code];

    /// <summary>Decode exception body into its code.</summary>
    public static ModbusExceptionCode Decode(ReadOnlySpan<byte> body)
    {
        if (body.Length != 1)
        {
            throw new ArgumentException("Exception body must be 1 byte.", nameof(body));
        }

        return (ModbusExceptionCode)body[0];
    }

    /// <summary>Build the exception function byte from a base function.</summary>
    public static byte ExceptionFunction(ModbusFunction function) => (byte)((byte)function | 0x80);
}

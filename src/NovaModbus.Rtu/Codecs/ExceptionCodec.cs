using NovaModbus.Rtu.Enums;

namespace NovaModbus.Rtu.Codecs;

/// <summary>Modbus exception response body: [code(1)]. Function byte carries 0x80 flag.</summary>
public static class ExceptionCodec
{
    /// <summary>Encode exception body for the given code.</summary>
    /// <param name="code">The Modbus exception code to encode (0x01-0x0B).</param>
    /// <returns>A one-byte array holding <paramref name="code"/>.</returns>
    public static byte[] Encode(ModbusExceptionCode code) => [(byte)code];

    /// <summary>Decode exception body into its code.</summary>
    /// <param name="body">Exception body; must be exactly 1 byte holding the exception code.</param>
    /// <returns>The decoded <see cref="ModbusExceptionCode"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="body"/> is not exactly 1 byte.</exception>
    public static ModbusExceptionCode Decode(ReadOnlySpan<byte> body)
    {
        if (body.Length != 1)
        {
            throw new ArgumentException("Exception body must be 1 byte.", nameof(body));
        }

        return (ModbusExceptionCode)body[0];
    }

    /// <summary>Build the exception function byte from a base function.</summary>
    /// <param name="function">The base Modbus function code (0x01-0x18).</param>
    /// <returns>The function byte with the 0x80 error flag set.</returns>
    public static byte ExceptionFunction(ModbusFunction function) => (byte)((byte)function | 0x80);
}

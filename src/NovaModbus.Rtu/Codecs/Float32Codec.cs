namespace NovaModbus.Rtu.Codecs;

/// <summary>Byte order of a 32-bit float across two registers (MSB first: A B C D).</summary>
public enum FloatWordOrder
{
    /// <summary>[AABB][CCDD] big-endian.</summary>
    Abcd,

    /// <summary>[CCDD][AABB] word-swapped.</summary>
    Cdab,

    /// <summary>[BBAA][DDCC] byte-swapped.</summary>
    Badc,

    /// <summary>[DDCC][BBAA] little-endian.</summary>
    Dcba,
}

/// <summary>IEEE-754 float32 across two Modbus registers.</summary>
public static class Float32Codec
{
    /// <summary>Encode a float into two registers in the given order.</summary>
    /// <param name="value">The IEEE-754 single-precision value to encode.</param>
    /// <param name="order">Byte/word order applied across the two registers.</param>
    /// <returns>A two-register encoding of <paramref name="value"/> in <paramref name="order"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="order"/> is not a known <see cref="FloatWordOrder"/>.</exception>
    public static ushort[] Encode(float value, FloatWordOrder order)
    {
        uint bits = BitConverter.SingleToUInt32Bits(value);
        byte a = (byte)(bits >> 24);
        byte b = (byte)(bits >> 16);
        byte c = (byte)(bits >> 8);
        byte d = (byte)bits;
        return order switch
        {
            FloatWordOrder.Abcd => [(ushort)((a << 8) | b), (ushort)((c << 8) | d)],
            FloatWordOrder.Cdab => [(ushort)((c << 8) | d), (ushort)((a << 8) | b)],
            FloatWordOrder.Badc => [(ushort)((b << 8) | a), (ushort)((d << 8) | c)],
            FloatWordOrder.Dcba => [(ushort)((d << 8) | c), (ushort)((b << 8) | a)],
            _ => throw new ArgumentOutOfRangeException(nameof(order), "Unknown word order."),
        };
    }

    /// <summary>Decode two registers into a float.</summary>
    /// <param name="registers">Source registers; must contain exactly 2 entries laid out per <paramref name="order"/>.</param>
    /// <param name="order">Byte/word order used to interpret <paramref name="registers"/>.</param>
    /// <returns>The decoded IEEE-754 single-precision value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="registers"/> does not contain exactly 2 entries.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="order"/> is not a known <see cref="FloatWordOrder"/>.</exception>
    public static float Decode(ReadOnlySpan<ushort> registers, FloatWordOrder order)
    {
        if (registers.Length != 2)
        {
            throw new ArgumentException("Float32 needs exactly 2 registers.", nameof(registers));
        }

        (ushort first, ushort second) = order switch
        {
            FloatWordOrder.Abcd => (registers[0], registers[1]),
            FloatWordOrder.Cdab => (registers[1], registers[0]),
            FloatWordOrder.Badc => (Swap(registers[0]), Swap(registers[1])),
            FloatWordOrder.Dcba => (Swap(registers[1]), Swap(registers[0])),
            _ => throw new ArgumentOutOfRangeException(nameof(order), "Unknown word order."),
        };

        return BitConverter.UInt32BitsToSingle((uint)((first << 16) | second));
    }

    /// <summary>Swap the two bytes of a register.</summary>
    /// <param name="value">The register value (0x0000-0xFFFF) to byte-swap.</param>
    /// <returns><paramref name="value"/> with its high and low bytes exchanged.</returns>
    private static ushort Swap(ushort value) => (ushort)(((value & 0xFF) << 8) | (value >> 8));
}

/// <summary>Display names for <see cref="FloatWordOrder"/>.</summary>
public static class FloatWordOrderText
{
    /// <summary>Big/Little Endian display name (e.g. "Big Endian (ABCD)").</summary>
    /// <param name="order">The word order to describe.</param>
    /// <returns>The human-readable display name for <paramref name="order"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="order"/> is not a known <see cref="FloatWordOrder"/>.</exception>
    public static string ToDisplayString(FloatWordOrder order) => order switch
    {
        FloatWordOrder.Abcd => "Big Endian (ABCD)",
        FloatWordOrder.Badc => "Big Endian, Byte Swap (BADC)",
        FloatWordOrder.Cdab => "Little Endian, Byte Swap (CDAB)",
        FloatWordOrder.Dcba => "Little Endian (DCBA)",
        _ => throw new ArgumentOutOfRangeException(nameof(order), "Unknown word order."),
    };
}

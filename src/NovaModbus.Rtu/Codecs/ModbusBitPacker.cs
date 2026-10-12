namespace NovaModbus.Rtu.Codecs;

/// <summary>LSB-first bit packing used by FC01/FC02/FC15.</summary>
public static class ModbusBitPacker
{
    /// <summary>Pack bools into bytes, LSB first. Last byte zero-padded.</summary>
    /// <param name="values">Coil/discrete states in address order.</param>
    /// <returns>Packed bytes, <c>(values.Count + 7) / 8</c> long.</returns>
    public static byte[] Pack(IReadOnlyList<bool> values)
    {
        int byteCount = (values.Count + 7) / 8;
        byte[] packed = new byte[byteCount];
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i])
            {
                packed[i / 8] |= (byte)(1 << (i % 8));
            }
        }

        return packed;
    }

    /// <summary>Unpack <paramref name="count"/> bits from packed bytes, LSB first.</summary>
    /// <param name="packed">Packed source bytes.</param>
    /// <param name="count">Bits to extract; must fit in <paramref name="packed"/>.</param>
    /// <returns>Unpacked states in address order.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="packed"/> holds fewer than <paramref name="count"/> bits.</exception>
    public static bool[] Unpack(ReadOnlySpan<byte> packed, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
        if (count > 0 && packed.Length < ((count + 7) / 8))
        {
            throw new ArgumentException("Not enough packed bytes for count.", nameof(packed));
        }

        bool[] values = new bool[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = (packed[i / 8] & (1 << (i % 8))) != 0;
        }

        return values;
    }
}

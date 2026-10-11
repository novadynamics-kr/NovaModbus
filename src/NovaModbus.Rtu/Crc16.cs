namespace NovaModbus.Rtu;

/// <summary>Modbus CRC-16/ARC (poly 0xA001, init 0xFFFF).</summary>
public static class Crc16
{
    /// <summary>Compute CRC over the given span (little-endian result).</summary>
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
            }
        }
        return crc;
    }

    /// <summary>Append CRC (Lo first) to a frame buffer.</summary>
    public static void AppendCrc(Span<byte> frameWithoutCrc, Span<byte> crcDestination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(crcDestination.Length, 2);
        ushort crc = Compute(frameWithoutCrc);
        crcDestination[0] = (byte)(crc & 0xFF);
        crcDestination[1] = (byte)((crc >> 8) & 0xFF);
    }

    /// <summary>Verify trailing 2-byte CRC of a complete frame.</summary>
    public static bool Verify(ReadOnlySpan<byte> frameWithCrc)
    {
        if (frameWithCrc.Length < 4)
        {
            return false;
        }

        ushort actual = Compute(frameWithCrc[..^2]);
        ushort expected = (ushort)(frameWithCrc[^2] | (frameWithCrc[^1] << 8));
        return actual == expected;
    }
}

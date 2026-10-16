using NovaModbus.Rtu;

namespace NovaModbus.Client.Services;

/// <summary>Shared value formatting for read results.</summary>
public static class ValueFormatter
{
    /// <summary>Bits as "1 0 1".</summary>
    public static string FormatBits(bool[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return string.Join(' ', values.Select(b => b ? "1" : "0"));
    }

    /// <summary>Registers as "[i] 0xXXXX (n)" lines.</summary>
    public static string FormatRegisters(ushort[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return string.Join(Environment.NewLine, values.Select((v, i) => $"[{i}] 0x{v:X4} ({v})"));
    }

    /// <summary>Registers as "v, v" (decimal).</summary>
    public static string FormatRegistersDec(ushort[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return string.Join(", ", values.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    /// <summary>Registers as "0xXXXX, 0xXXXX".</summary>
    public static string FormatRegistersHex(ushort[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return string.Join(", ", values.Select(v => $"0x{v:X4}"));
    }

    /// <summary>Bytes as decimal list.</summary>
    public static string FormatBytesDec(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return string.Join(' ', data.Select(b => b.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    /// <summary>Bytes as hex list.</summary>
    public static string FormatBytesHex(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Hex.Format(data);
    }

    /// <summary>Device ID bytes as ASCII text, or hex when not printable.</summary>
    public static string FormatDeviceId(byte[] id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return id.All(b => b is >= 0x20 and < 0x7F)
            ? System.Text.Encoding.ASCII.GetString(id)
            : Hex.Format(id);
    }
}

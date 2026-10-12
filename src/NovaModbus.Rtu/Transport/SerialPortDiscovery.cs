using System.IO.Ports;

namespace NovaModbus.Rtu.Transport;

/// <summary>System serial port enumeration.</summary>
public static class SerialPortDiscovery
{
    /// <summary>Gets available port names, COM numbers sorted naturally (COM2 before COM10).</summary>
    /// <returns>Available serial port names in natural sort order.</returns>
    public static string[] GetPortNames()
    {
        return SerialPort.GetPortNames().OrderBy(SortKey, Comparer<(string, int)>.Default).ToArray();
    }

    private static (string Prefix, int Number) SortKey(string name)
    {
        int i = name.Length;
        while (i > 0 && char.IsAsciiDigit(name[i - 1]))
        {
            i--;
        }

        int number = i < name.Length && int.TryParse(name[i..], out int n) ? n : int.MaxValue;
        return (name[..i], number);
    }
}

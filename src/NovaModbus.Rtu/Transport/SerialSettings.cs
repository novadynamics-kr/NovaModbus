namespace NovaModbus.Rtu.Transport;

/// <summary>Serial port settings for RTU transport.</summary>
/// <param name="PortName">e.g. COM3.</param>
/// <param name="BaudRate">e.g. 9600.</param>
/// <param name="DataBits">RTU requires 8.</param>
/// <param name="Parity">System.IO.Ports.Parity name (spec default Even).</param>
/// <param name="StopBits">System.IO.Ports.StopBits name.</param>
public sealed record SerialSettings(
    string PortName,
    int BaudRate = 9600,
    int DataBits = 8,
    string Parity = "Even",
    string StopBits = "One")
{
    /// <summary>Enforce the RTU 11-bit character rule and known enum names.</summary>
    /// <exception cref="InvalidOperationException">Data bits are not 8, parity/stop-bits names are unknown, or no-parity is used without two stop bits.</exception>
    public void Validate()
    {
        if (DataBits != 8)
        {
            throw new InvalidOperationException($"RTU requires 8 data bits (got {DataBits}).");
        }

        if (!Enum.TryParse<System.IO.Ports.Parity>(Parity, ignoreCase: true, out _))
        {
            throw new InvalidOperationException($"Unknown parity: '{Parity}'.");
        }

        if (!Enum.TryParse<System.IO.Ports.StopBits>(StopBits, ignoreCase: true, out _))
        {
            throw new InvalidOperationException($"Unknown stop bits: '{StopBits}'.");
        }

        if (Parity.Equals("None", StringComparison.OrdinalIgnoreCase)
            && !StopBits.Equals("Two", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("RTU without parity requires two stop bits (11-bit characters).");
        }
    }
}

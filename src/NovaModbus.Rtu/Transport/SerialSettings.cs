namespace NovaModbus.Rtu.Transport;

/// <summary>Serial port settings for RTU transport.</summary>
/// <param name="PortName">e.g. COM3.</param>
/// <param name="BaudRate">e.g. 9600.</param>
/// <param name="DataBits">Usually 8.</param>
/// <param name="Parity">System.IO.Ports.Parity name.</param>
/// <param name="StopBits">System.IO.Ports.StopBits name.</param>
public sealed record SerialSettings(
    string PortName,
    int BaudRate = 9600,
    int DataBits = 8,
    string Parity = "None",
    string StopBits = "One");

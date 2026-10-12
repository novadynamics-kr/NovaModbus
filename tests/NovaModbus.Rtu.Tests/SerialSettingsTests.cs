using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class SerialSettingsTests
{
    [TestMethod]
    public void DefaultsAreSpecCompliant()
    {
        SerialSettings settings = new("COM1");
        Assert.AreEqual("Even", settings.Parity);
        Assert.AreEqual("One", settings.StopBits);
        settings.Validate();
    }

    [TestMethod]
    public void NoParityRequiresTwoStopBits()
    {
        SerialSettings invalid = new("COM1", 9600, 8, "None", "One");
        Assert.ThrowsExactly<InvalidOperationException>(() => invalid.Validate());

        SerialSettings valid = new("COM1", 9600, 8, "None", "Two");
        valid.Validate();
    }

    [TestMethod]
    public void NonEightDataBitsRejected()
    {
        SerialSettings settings = new("COM1", 9600, 7, "Even", "One");
        Assert.ThrowsExactly<InvalidOperationException>(() => settings.Validate());
    }

    [TestMethod]
    public void UnknownNamesRejected()
    {
        SerialSettings parity = new("COM1", 9600, 8, "Sideways", "One");
        Assert.ThrowsExactly<InvalidOperationException>(() => parity.Validate());

        SerialSettings stops = new("COM1", 9600, 8, "Even", "Three");
        Assert.ThrowsExactly<InvalidOperationException>(() => stops.Validate());
    }
}

using NovaModbus.Client.Wpf.Services;

namespace NovaModbus.Client.Wpf.Tests;

[TestClass]
public sealed class ValueFormatterTests
{
    [TestMethod]
    public void FormatBitsJoinsWithSpaces()
    {
        Assert.AreEqual("1 0 1", ValueFormatter.FormatBits([true, false, true]));
        Assert.AreEqual(string.Empty, ValueFormatter.FormatBits([]));
    }

    [TestMethod]
    public void FormatRegistersUsesIndexHexAndDecimal()
    {
        string text = ValueFormatter.FormatRegisters([0x1234, 0x00AB]);
        string[] lines = text.Split(Environment.NewLine);
        Assert.HasCount(2, lines);
        Assert.AreEqual("[0] 0x1234 (4660)", lines[0]);
        Assert.AreEqual("[1] 0x00AB (171)", lines[1]);
    }

    [TestMethod]
    public void FormatRegistersDecAndHex()
    {
        Assert.AreEqual("1, 2", ValueFormatter.FormatRegistersDec([1, 2]));
        Assert.AreEqual("0x00AB, 0x1234", ValueFormatter.FormatRegistersHex([0x00AB, 0x1234]));
    }

    [TestMethod]
    public void FormatBytesDecAndHex()
    {
        Assert.AreEqual("1 171", ValueFormatter.FormatBytesDec([0x01, 0xAB]));
        Assert.AreEqual("01 AB", ValueFormatter.FormatBytesHex([0x01, 0xAB]));
    }

    [TestMethod]
    public void FormatDeviceIdPrintableReturnsAscii()
    {
        byte[] id = "Nova"u8.ToArray();
        Assert.AreEqual("Nova", ValueFormatter.FormatDeviceId(id));
    }

    [TestMethod]
    public void FormatDeviceIdNonPrintableFallsBackToHex()
    {
        Assert.AreEqual("01 AB", ValueFormatter.FormatDeviceId([0x01, 0xAB]));
    }

    [TestMethod]
    public void NullInputsThrow()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ValueFormatter.FormatBits(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => ValueFormatter.FormatRegisters(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => ValueFormatter.FormatRegistersDec(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => ValueFormatter.FormatRegistersHex(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => ValueFormatter.FormatBytesDec(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => ValueFormatter.FormatBytesHex(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => ValueFormatter.FormatDeviceId(null!));
    }
}

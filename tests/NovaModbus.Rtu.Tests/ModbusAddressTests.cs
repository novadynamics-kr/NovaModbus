namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class ModbusAddressTests
{
    [TestMethod]
    public void DecimalAndHexParse()
    {
        Assert.AreEqual((ushort)10, ModbusAddress.Parse("10"));
        Assert.AreEqual((ushort)10, ModbusAddress.Parse("0x000A"));
        Assert.AreEqual((ushort)10, ModbusAddress.Parse("0X000A"));
        Assert.AreEqual((ushort)0xFFFF, ModbusAddress.Parse("0xFFFF"));
        Assert.AreEqual((ushort)0, ModbusAddress.Parse(" 0 "));
    }

    [TestMethod]
    public void InvalidInputsRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(() => ModbusAddress.Parse("xyz"));
        Assert.ThrowsExactly<ArgumentException>(() => ModbusAddress.Parse("0x"));
        Assert.ThrowsExactly<ArgumentException>(() => ModbusAddress.Parse("70000"));
        Assert.ThrowsExactly<ArgumentException>(() => ModbusAddress.Parse(""));
    }

    [TestMethod]
    public void CapacityCheck()
    {
        Assert.AreEqual((ushort)999, ModbusAddress.Parse("999", 1000));
        Assert.ThrowsExactly<ArgumentException>(() => ModbusAddress.Parse("1000", 1000));
        Assert.ThrowsExactly<ArgumentException>(() => ModbusAddress.Parse("0xFFFF", 1000));
    }
}

using NovaModbus.Slave.ViewModels;

namespace NovaModbus.Slave.WinForms.Tests;

[TestClass]
public sealed class RegisterRowTests
{
    [TestMethod]
    public void AddressHexFormatsStartAs4DigitHex()
    {
        var row = new RegisterRow(10, 0x022B, (_, _) => { });
        Assert.AreEqual("0x000A", row.AddressHex);
        Assert.AreEqual("0x022B", row.HexText);
        Assert.AreEqual("555", row.ValueText);
    }

    [TestMethod]
    public void ValueTextCommitsDecimalAndHex()
    {
        ushort committed = 0;
        var row = new RegisterRow(5, 0, (_, v) => committed = v);
        row.ValueText = "0x12";
        Assert.AreEqual((ushort)0x12, committed);
        Assert.AreEqual("0x0012", row.HexText);
    }

    [TestMethod]
    public void ValueTextRejectsInvalidInput()
    {
        int commits = 0;
        var row = new RegisterRow(5, 7, (_, _) => commits++);
        row.ValueText = "abc";
        Assert.AreEqual(0, commits);
        Assert.AreEqual("7", row.ValueText);
    }

    [TestMethod]
    public void IsSelectedSuppressionSkipsNotification()
    {
        var row = new RegisterRow(1, 0, (_, _) => { });
        int count = 0;
        row.PropertyChanged += (_, _) => count++;
        row.SuppressSelectionNotification = true;
        row.IsSelected = true;
        Assert.IsTrue(row.IsSelected);
        Assert.AreEqual(0, count);
    }
}

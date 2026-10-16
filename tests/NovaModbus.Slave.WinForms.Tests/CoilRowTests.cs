using NovaModbus.Slave.ViewModels;

namespace NovaModbus.Slave.WinForms.Tests;

[TestClass]
public sealed class CoilRowTests
{
    [TestMethod]
    public void AddressHexFormatsAs4DigitHex()
    {
        var row = new CoilRow(10, false, (_, _) => { });
        Assert.AreEqual("0x000A", row.AddressHex);
    }

    [TestMethod]
    public void IsOnCommitsToStore()
    {
        bool committed = false;
        var row = new CoilRow(3, false, (_, v) => committed = v);
        row.IsOn = true;
        Assert.IsTrue(committed);
        Assert.IsTrue(row.IsOn);
    }

    [TestMethod]
    public void IsOnRevertsWhenCommitThrows()
    {
        var row = new CoilRow(3, false, (_, _) => throw new InvalidOperationException("no store"));
        row.IsOn = true;
        Assert.IsFalse(row.IsOn);
    }
}

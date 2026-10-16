using NovaModbus.Slave.ViewModels;

namespace NovaModbus.Slave.WinForms.Tests;

[TestClass]
public sealed class SlaveStoreTests
{
    private static SlaveStore Create() => new(() => null);

    [TestMethod]
    public void DefaultIdIsOneAndSelected()
    {
        using var store = Create();
        Assert.HasCount(1, store.SlaveIds);
        Assert.AreEqual((byte)1, store.SlaveIds[0]);
        Assert.AreEqual((byte)1, store.SelectedSlaveId);
    }

    [TestMethod]
    public void AddSlaveRejectsInvalidAndDuplicate()
    {
        using var store = Create();
        store.NewSlaveIdText = "0";
        store.AddSlave();
        Assert.HasCount(1, store.SlaveIds);
        Assert.IsTrue(store.StatusText.StartsWith("Invalid", StringComparison.Ordinal));
        store.NewSlaveIdText = "1";
        store.AddSlave();
        Assert.AreEqual("Slave ID 1 already exists.", store.StatusText);
        store.NewSlaveIdText = "3";
        store.AddSlave();
        Assert.HasCount(2, store.SlaveIds);
        CollectionAssert.AreEqual(new byte[] { 1, 3 }, store.SlaveIds.ToArray());
    }

    [TestMethod]
    public void RemoveSelectedSlaveReselectsFirst()
    {
        using var store = Create();
        store.NewSlaveIdText = "2";
        store.AddSlave();
        store.SelectedSlaveId = 2;
        store.RemoveSelectedSlave();
        Assert.HasCount(1, store.SlaveIds);
        Assert.AreEqual((byte)1, store.SelectedSlaveId);
    }

    [TestMethod]
    public void AddHoldingAndDuplicate()
    {
        using var store = Create();
        store.HoldingAddressText = "5";
        store.HoldingValueText = "0x12";
        store.AddHolding();
        Assert.HasCount(1, store.HoldingRows);
        Assert.AreEqual((ushort)5, store.HoldingRows[0].Address);
        store.AddHolding();
        Assert.HasCount(1, store.HoldingRows);
        Assert.IsTrue(store.StatusText.Contains("already", StringComparison.Ordinal));
    }

    [TestMethod]
    public void AddCoilAndDuplicate()
    {
        using var store = Create();
        store.CoilAddressText = "7";
        store.AddCoil();
        Assert.HasCount(1, store.CoilRows);
        store.AddCoil();
        Assert.HasCount(1, store.CoilRows);
        Assert.IsTrue(store.StatusText.Contains("already", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RemoveHoldingsAtDropsSubsetKeepsOrder()
    {
        using var store = Create();
        store.HoldingAddressText = "0";
        store.HoldingEndText = "4";
        store.AddHoldingRangeAsync().GetAwaiter().GetResult();
        Assert.HasCount(5, store.HoldingRows);
        store.RemoveHoldingsAt(new HashSet<int> { 1, 3 });
        Assert.HasCount(3, store.HoldingRows);
        ushort[] addresses = store.HoldingRows.Select(r => r.Address).ToArray();
        CollectionAssert.AreEqual(new ushort[] { 0, 2, 4 }, addresses);
    }

    [TestMethod]
    public void RemoveCoilsAtAllClears()
    {
        using var store = Create();
        store.CoilAddressText = "0";
        store.CoilEndText = "2";
        store.AddCoilRangeAsync().GetAwaiter().GetResult();
        Assert.HasCount(3, store.CoilRows);
        store.RemoveCoilsAt(new HashSet<int> { 0, 1, 2, 3 });
        Assert.HasCount(0, store.CoilRows);
    }

    [TestMethod]
    public void AddRangeRejectsReversedRange()
    {
        using var store = Create();
        store.HoldingAddressText = "9";
        store.HoldingEndText = "0";
        store.AddHoldingRangeAsync().GetAwaiter().GetResult();
        Assert.HasCount(0, store.HoldingRows);
        Assert.IsTrue(store.StatusText.Contains("Invalid range", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SaveLoadMapRoundTrip()
    {
        string path = Path.Combine(Path.GetTempPath(), $"slavemap-{Guid.NewGuid():N}.json");
        try
        {
            using var store = Create();
            store.HoldingAddressText = "5";
            store.HoldingValueText = "42";
            store.AddHolding();
            store.CoilAddressText = "7";
            store.AddCoil();
            store.SaveMapTo(path);
            using var reloaded = Create();
            reloaded.LoadMapFrom(path);
            Assert.HasCount(1, reloaded.HoldingRows);
            Assert.AreEqual((ushort)5, reloaded.HoldingRows[0].Address);
            Assert.AreEqual("42", reloaded.HoldingRows[0].ValueText);
            Assert.HasCount(1, reloaded.CoilRows);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void WriteFloatAtWritesTwoRegisters()
    {
        using var store = Create();
        store.WriteFloatAt(10, 1.5f, NovaModbus.Rtu.Codecs.FloatWordOrder.Abcd);
        Assert.IsTrue(store.StatusText.StartsWith("Wrote 1.5", StringComparison.Ordinal));
        NovaModbus.Rtu.ModbusDataStore? dataStore = store.GetStore(1);
        Assert.IsNotNull(dataStore);
        ushort[] regs = dataStore.ReadHolding(10, 2);
        Assert.AreSequenceEqual(NovaModbus.Rtu.Codecs.Float32Codec.Encode(1.5f, NovaModbus.Rtu.Codecs.FloatWordOrder.Abcd), regs);
    }

    [TestMethod]
    public void WriteFloatAtRejectsOutOfRange()
    {
        using var store = Create();
        store.WriteFloatAt(ushort.MaxValue, 1.0f, NovaModbus.Rtu.Codecs.FloatWordOrder.Abcd);
        Assert.IsTrue(store.StatusText.StartsWith("Invalid address", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BuildCountersTextContainsIdLine()
    {
        using var store = Create();
        string text = store.BuildCountersText();
        Assert.IsTrue(text.Contains("ID 1: requests=0", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PortOptionsRejectsInvalidBaud()
    {
        var options = new SlavePortOptions { BaudRateText = "abc" };
        Assert.IsFalse(options.IsBaudRateValid);
        Assert.ThrowsExactly<InvalidOperationException>(() => options.BuildSettings());
    }
}

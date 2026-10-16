using NovaModbus.ClientWPF.Services;
using NovaModbus.ClientWPF.ViewModels;

namespace NovaModbus.ClientWPF.Tests;

[TestClass]
public sealed class WatchEntryTests
{
    [TestMethod]
    public void AddressHexFormatsStartAs4DigitHex()
    {
        var entry = new WatchEntry(1, WatchArea.HoldingRegisters, 10, 2);
        Assert.AreEqual("0x000A", entry.AddressHex);
    }

    [TestMethod]
    public void EqualEntriesCompareByValue()
    {
        var a = new WatchEntry(1, WatchArea.Coils, 7, 3, "x");
        var b = new WatchEntry(1, WatchArea.Coils, 7, 3, "x");
        var other = new WatchEntry(2, WatchArea.Coils, 7, 3, "x");
        Assert.IsTrue(a.Equals(b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        Assert.IsFalse(a.Equals(other));
        Assert.IsFalse(a.Equals(null));
    }

    [TestMethod]
    public void IsSelectedRaisesPropertyChanged()
    {
        var entry = new WatchEntry(1, WatchArea.HoldingRegisters, 0, 1);
        string? changed = null;
        entry.PropertyChanged += (_, e) => changed = e.PropertyName;
        entry.IsSelected = true;
        Assert.AreEqual(nameof(WatchEntry.IsSelected), changed);
    }

    [TestMethod]
    public void SuppressedSelectionSkipsNotification()
    {
        var entry = new WatchEntry(1, WatchArea.HoldingRegisters, 0, 1)
        {
            SuppressSelectionNotification = true,
        };
        int count = 0;
        entry.PropertyChanged += (_, _) => count++;
        entry.IsSelected = true;
        Assert.IsTrue(entry.IsSelected);
        Assert.AreEqual(0, count);
    }
}

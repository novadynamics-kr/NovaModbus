using NovaModbus.Client.Services;
using NovaModbus.Client.ViewModels;

namespace NovaModbus.Client.WinForms.Tests;

[TestClass]
public sealed class WatchListStoreTests
{
    [TestMethod]
    public void SaveLoadRoundTripPreservesEntries()
    {
        string path = TempPath();
        try
        {
            WatchEntry[] entries =
            [
                new(1, WatchArea.HoldingRegisters, 0, 10),
                new(2, WatchArea.Coils, 7, 3, "extra"),
            ];
            WatchListStore.Save(path, entries);
            WatchEntry[] loaded = WatchListStore.Load(path);
            Assert.HasCount(2, loaded);
            Assert.AreEqual(entries[0], loaded[0]);
            Assert.AreEqual(entries[1], loaded[1]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadMissingFileThrows()
    {
        Assert.ThrowsExactly<FileNotFoundException>(() => WatchListStore.Load(TempPath()));
    }

    [TestMethod]
    public void NullInputsThrow()
    {
        Assert.ThrowsExactly<ArgumentException>(() => WatchListStore.Save(" ", []));
        Assert.ThrowsExactly<ArgumentNullException>(() => WatchListStore.Save(TempPath(), null!));
        Assert.ThrowsExactly<ArgumentException>(() => WatchListStore.Load(" "));
    }

    private static string TempPath() => Path.Combine(Path.GetTempPath(), $"watch-{Guid.NewGuid():N}.json");
}

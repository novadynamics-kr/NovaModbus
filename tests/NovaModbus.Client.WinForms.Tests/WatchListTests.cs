using NovaModbus.Client.ViewModels;

namespace NovaModbus.Client.WinForms.Tests;

[TestClass]
public sealed class WatchListTests
{
    [TestMethod]
    public void AddEntryAppendsEntryAndResult()
    {
        var list = new WatchList(() => null);
        list.SlaveIdText = "1";
        list.Area = WatchArea.HoldingRegisters;
        list.StartText = "0";
        list.CountText = "10";
        list.AddEntry();
        Assert.HasCount(1, list.Entries);
        Assert.HasCount(1, list.Results);
        Assert.AreEqual(string.Empty, list.StatusText);
    }

    [TestMethod]
    public void AddDuplicateReportsAlreadyInList()
    {
        var list = new WatchList(() => null);
        list.AddEntry();
        list.AddEntry();
        Assert.HasCount(1, list.Entries);
        Assert.AreEqual("Already in the list.", list.StatusText);
    }

    [TestMethod]
    public void AddInvalidReportsError()
    {
        var list = new WatchList(() => null);
        list.SlaveIdText = "0";
        list.AddEntry();
        Assert.HasCount(0, list.Entries);
        Assert.IsTrue(list.StatusText.StartsWith("Error:", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RemoveEntriesDropsInstances()
    {
        var list = new WatchList(() => null);
        list.AddEntry();
        list.StartText = "5";
        list.AddEntry();
        Assert.HasCount(2, list.Entries);
        list.Entries[0].IsSelected = true;
        list.RemoveEntries(list.Entries.Where(e => e.IsSelected).ToArray());
        Assert.HasCount(1, list.Entries);
        Assert.HasCount(1, list.Results);
        Assert.AreEqual((ushort)5, list.Entries[0].Start);
    }

    [TestMethod]
    public void SelectAndDeselectAll()
    {
        var list = new WatchList(() => null);
        list.AddEntry();
        list.StartText = "5";
        list.AddEntry();
        list.SelectAll();
        Assert.IsTrue(list.Entries.All(e => e.IsSelected));
        list.DeselectAll();
        Assert.IsTrue(list.Entries.All(e => !e.IsSelected));
    }

    [TestMethod]
    public void ClearAllEmptiesCollections()
    {
        var list = new WatchList(() => null);
        list.AddEntry();
        list.ClearAll();
        Assert.HasCount(0, list.Entries);
        Assert.HasCount(0, list.Results);
    }

    [TestMethod]
    public void SaveLoadRoundTripViaPaths()
    {
        string path = Path.Combine(Path.GetTempPath(), $"watchlist-{Guid.NewGuid():N}.json");
        try
        {
            var list = new WatchList(() => null);
            list.AddEntry();
            list.SaveWatchTo(path);
            Assert.HasCount(1, list.Entries);

            var reloaded = new WatchList(() => null);
            reloaded.LoadWatchFrom(path);
            Assert.HasCount(1, reloaded.Entries);
            Assert.AreEqual(list.Entries[0], reloaded.Entries[0]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task AddRangeAddsAddressSpan()
    {
        var list = new WatchList(() => null);
        list.StartText = "0";
        list.AddressEndText = "2";
        list.CountText = "1";
        await list.AddRangeAsync();
        Assert.HasCount(3, list.Entries);
        Assert.HasCount(3, list.Results);
        Assert.IsFalse(list.IsBulkWorking);
    }

    [TestMethod]
    public async Task ReadAllWithoutClientMarksError()
    {
        var list = new WatchList(() => null);
        list.AddEntry();
        await list.ReadAllAsync();
        Assert.AreEqual("Error", list.Results[0].Status);
        Assert.IsFalse(list.IsBusy);
    }

    [TestMethod]
    public async Task RemoveAtIndicesDropsSubsetKeepsOrder()
    {
        var list = new WatchList(() => null);
        list.StartText = "0";
        list.AddressEndText = "4";
        list.CountText = "1";
        await list.AddRangeAsync();
        Assert.HasCount(5, list.Entries);
        list.RemoveAtIndices(new HashSet<int> { 1, 3 });
        Assert.HasCount(3, list.Entries);
        Assert.HasCount(3, list.Results);
        ushort[] starts = list.Entries.Select(e => e.Start).ToArray();
        CollectionAssert.AreEqual(new ushort[] { 0, 2, 4 }, starts);
        // Results stay 1:1 with entries.
        for (int i = 0; i < list.Results.Count; i++)
        {
            Assert.AreSame(list.Entries[i], list.Results[i].Entry);
        }
    }

    [TestMethod]
    public async Task RemoveAtIndicesAllClears()
    {
        var list = new WatchList(() => null);
        list.StartText = "0";
        list.AddressEndText = "2";
        list.CountText = "1";
        await list.AddRangeAsync();
        list.RemoveAtIndices(new HashSet<int> { 0, 1, 2 });
        Assert.HasCount(0, list.Entries);
        Assert.HasCount(0, list.Results);
    }

    [TestMethod]
    public async Task RemoveAtIndicesEmptyIsNoOp()
    {
        var list = new WatchList(() => null);
        list.AddEntry();
        list.RemoveAtIndices(new HashSet<int>());
        Assert.HasCount(1, list.Entries);
        Assert.HasCount(1, list.Results);
    }
}

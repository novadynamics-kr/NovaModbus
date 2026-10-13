using NovaModbus.Client.ViewModels;

namespace NovaModbus.Client.Tests;

[TestClass]
public sealed class WatchViewModelTests
{
    [TestMethod]
    public void AddEntryAppendsEntryAndResult()
    {
        var vm = new WatchViewModel(() => null);
        vm.SlaveIdText = "1";
        vm.Area = WatchArea.HoldingRegisters;
        vm.StartText = "0";
        vm.CountText = "10";
        vm.AddCommand.Execute(null);
        Assert.HasCount(1, vm.Entries);
        Assert.HasCount(1, vm.Results);
        Assert.AreEqual(string.Empty, vm.StatusText);
    }

    [TestMethod]
    public void AddDuplicateReportsAlreadyInList()
    {
        var vm = new WatchViewModel(() => null);
        vm.AddCommand.Execute(null);
        vm.AddCommand.Execute(null);
        Assert.HasCount(1, vm.Entries);
        Assert.AreEqual("Already in the list.", vm.StatusText);
    }

    [TestMethod]
    public void AddInvalidReportsError()
    {
        var vm = new WatchViewModel(() => null);
        vm.SlaveIdText = "0";
        vm.AddCommand.Execute(null);
        Assert.HasCount(0, vm.Entries);
        Assert.IsTrue(vm.StatusText.StartsWith("Error:", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RemoveEntriesDropsInstances()
    {
        var vm = new WatchViewModel(() => null);
        vm.AddCommand.Execute(null);
        vm.StartText = "5";
        vm.AddCommand.Execute(null);
        Assert.HasCount(2, vm.Entries);
        vm.Entries[0].IsSelected = true;
        vm.RemoveEntries(vm.Entries.Where(e => e.IsSelected).ToArray());
        Assert.HasCount(1, vm.Entries);
        Assert.HasCount(1, vm.Results);
        Assert.AreEqual((ushort)5, vm.Entries[0].Start);
    }

    [TestMethod]
    public void SelectAndDeselectAll()
    {
        var vm = new WatchViewModel(() => null);
        vm.AddCommand.Execute(null);
        vm.StartText = "5";
        vm.AddCommand.Execute(null);
        vm.SelectAllCommand.Execute(null);
        Assert.IsTrue(vm.Entries.All(e => e.IsSelected));
        vm.DeselectAllCommand.Execute(null);
        Assert.IsTrue(vm.Entries.All(e => !e.IsSelected));
    }

    [TestMethod]
    public void ClearAllEmptiesCollections()
    {
        var vm = new WatchViewModel(() => null);
        vm.AddCommand.Execute(null);
        vm.ClearAllCommand.Execute(null);
        Assert.HasCount(0, vm.Entries);
        Assert.HasCount(0, vm.Results);
    }

    [TestMethod]
    public void SaveLoadRoundTripViaPaths()
    {
        string path = Path.Combine(Path.GetTempPath(), $"watchvm-{Guid.NewGuid():N}.json");
        try
        {
            var vm = new WatchViewModel(() => null);
            vm.AddCommand.Execute(null);
            vm.SaveWatchTo(path);
            Assert.HasCount(1, vm.Entries);

            var reloaded = new WatchViewModel(() => null);
            reloaded.LoadWatchFrom(path);
            Assert.HasCount(1, reloaded.Entries);
            Assert.AreEqual(vm.Entries[0], reloaded.Entries[0]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task AddRangeAddsAddressSpan()
    {
        var vm = new WatchViewModel(() => null);
        vm.StartText = "0";
        vm.AddressEndText = "2";
        vm.CountText = "1";
        await vm.AddRangeCommand.ExecuteAsync(null);
        Assert.HasCount(3, vm.Entries);
        Assert.HasCount(3, vm.Results);
        Assert.IsFalse(vm.IsBulkWorking);
    }

    [TestMethod]
    public async Task ReadAllWithoutClientMarksError()
    {
        var vm = new WatchViewModel(() => null);
        vm.AddCommand.Execute(null);
        await vm.ReadAllCommand.ExecuteAsync(null);
        Assert.AreEqual("Error", vm.Results[0].Status);
        Assert.IsFalse(vm.IsBusy);
    }
}

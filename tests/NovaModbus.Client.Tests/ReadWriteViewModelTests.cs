using NovaModbus.Client.ViewModels;

namespace NovaModbus.Client.Tests;

[TestClass]
public sealed class ReadWriteViewModelTests
{
    [TestMethod]
    public async Task RunOnceWithoutClientReportsNotConnected()
    {
        var vm = new ReadWriteViewModel(() => null);
        string result = await vm.RunOnceAsync();
        Assert.AreEqual("Not connected.", result);
        Assert.AreEqual("Not connected.", vm.ResultText);
    }

    [TestMethod]
    public async Task RunOnceWithInvalidSlaveIdReportsError()
    {
        var vm = new ReadWriteViewModel(() => throw new InvalidOperationException("no client in test"));
        vm.SlaveIdText = "0";
        string result = await vm.RunOnceAsync();
        Assert.IsTrue(result.StartsWith("Error:", StringComparison.Ordinal));
    }

    [TestMethod]
    public void FunctionSwitchUpdatesHints()
    {
        var vm = new ReadWriteViewModel(() => null);
        vm.SelectedFunction = NovaModbus.Rtu.Enums.ModbusFunction.WriteSingleCoil;
        Assert.IsTrue(vm.NeedsValues);
        Assert.IsFalse(vm.NeedsCount);
        Assert.AreEqual("Values: 1 or 0.", vm.HintText);
    }
}

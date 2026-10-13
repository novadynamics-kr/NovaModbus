using NovaModbus.Client.ViewModels;

namespace NovaModbus.Client.Tests;

[TestClass]
public sealed class PollingAndSettingsTests
{
    [TestMethod]
    public async Task PollingWithInvalidIntervalReportsError()
    {
        var target = new ReadWriteViewModel(() => null);
        var vm = new PollingViewModel(target) { IntervalMsText = "0" };
        await vm.StartCommand.ExecuteAsync(null);
        Assert.HasCount(1, vm.History);
        Assert.IsFalse(vm.IsPolling);
    }

    [TestMethod]
    public void SerialSettingsRejectsInvalidBaud()
    {
        var vm = new SerialPortSettingsViewModel();
        vm.BaudRateText = "abc";
        Assert.IsFalse(vm.IsBaudRateValid);
        Assert.ThrowsExactly<InvalidOperationException>(() => vm.BuildSettings());
    }

    [TestMethod]
    public void SerialSettingsDetectsVirtualPrefix()
    {
        var vm = new SerialPortSettingsViewModel();
        vm.SelectedPort = SerialPortSettingsViewModel.VirtualPrefix + "demo";
        Assert.IsTrue(vm.IsVirtualSelected);
        Assert.AreEqual("demo", vm.VirtualEndpointName);
    }
}

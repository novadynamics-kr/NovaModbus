using NovaModbus.Client.ViewModels;

namespace NovaModbus.Client.WinForms.Tests;

[TestClass]
public sealed class OptionsRunnerTests
{
    [TestMethod]
    public void SerialOptionsRejectsInvalidBaud()
    {
        var options = new SerialPortOptions();
        options.BaudRateText = "abc";
        Assert.IsFalse(options.IsBaudRateValid);
        Assert.ThrowsExactly<InvalidOperationException>(() => options.BuildSettings());
    }

    [TestMethod]
    public void SerialOptionsDetectsVirtualPrefix()
    {
        var options = new SerialPortOptions();
        options.SelectedPort = SerialPortOptions.VirtualPrefix + "demo";
        Assert.IsTrue(options.IsVirtualSelected);
        Assert.AreEqual("demo", options.VirtualEndpointName);
    }

    [TestMethod]
    public async Task RunnerWithoutClientReportsNotConnected()
    {
        var runner = new ReadWriteRunner(() => null);
        string result = await runner.RunOnceAsync();
        Assert.AreEqual("Not connected.", result);
    }

    [TestMethod]
    public async Task RunnerWithInvalidSlaveIdReportsError()
    {
        var runner = new ReadWriteRunner(() => throw new InvalidOperationException("no client in test"));
        runner.SlaveIdText = "0";
        string result = await runner.RunOnceAsync();
        Assert.IsTrue(result.StartsWith("Error:", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RunnerFunctionSwitchUpdatesHints()
    {
        var runner = new ReadWriteRunner(() => null);
        runner.SelectedFunction = NovaModbus.Rtu.Enums.ModbusFunction.WriteSingleCoil;
        Assert.IsTrue(runner.NeedsValues);
        Assert.IsFalse(runner.NeedsCount);
        Assert.AreEqual("Values: 1 or 0.", runner.HintText);
    }

    [TestMethod]
    public async Task PollingWithInvalidIntervalReportsError()
    {
        var target = new ReadWriteRunner(() => null);
        var polling = new PollingRunner(target) { IntervalMsText = "0" };
        await polling.StartAsync();
        Assert.HasCount(1, polling.History);
        Assert.IsFalse(polling.IsPolling);
    }
}

using NovaModbus.Slave.Services;
using NovaModbus.Slave.ViewModels;

namespace NovaModbus.Slave.WinForms.Tests;

[TestClass]
public sealed class RegisterMapStoreTests
{
    [TestMethod]
    public void SaveLoadRoundTripPreservesMap()
    {
        string path = TempPath();
        try
        {
            RegisterMapStore.HoldingMapItem[] holdings = [new(0, 10), new(7, 0x1234)];
            RegisterMapStore.CoilMapItem[] coils = [new(3, true), new(4, false)];
            RegisterMapStore.Save(path, holdings, coils);
            RegisterMapStore.RegisterMapFile loaded = RegisterMapStore.Load(path);
            Assert.HasCount(2, loaded.Holding);
            Assert.HasCount(2, loaded.Coils);
            Assert.AreEqual(holdings[1], loaded.Holding[1]);
            Assert.AreEqual(coils[0], loaded.Coils[0]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadMissingFileThrows()
    {
        Assert.ThrowsExactly<FileNotFoundException>(() => RegisterMapStore.Load(TempPath()));
    }

    [TestMethod]
    public void NullInputsThrow()
    {
        Assert.ThrowsExactly<ArgumentException>(() => RegisterMapStore.Save(" ", [], []));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            RegisterMapStore.Save(TempPath(), null!, []));
        Assert.ThrowsExactly<ArgumentException>(() => RegisterMapStore.Load(" "));
    }

    private static string TempPath() => Path.Combine(Path.GetTempPath(), $"regmap-{Guid.NewGuid():N}.json");
}

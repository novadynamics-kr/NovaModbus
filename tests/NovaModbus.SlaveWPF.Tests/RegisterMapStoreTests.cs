using NovaModbus.SlaveWPF.Services;

namespace NovaModbus.SlaveWPF.Tests;

[TestClass]
public sealed class RegisterMapStoreTests
{
    [TestMethod]
    public void SaveLoadRoundTripPreservesMaps()
    {
        string path = TempPath();
        try
        {
            RegisterMapStore.HoldingMapItem[] holding = [new(0, 0x1234), new(10, 7)];
            RegisterMapStore.CoilMapItem[] coils = [new(3, true), new(4, false)];
            RegisterMapStore.Save(path, holding, coils);
            RegisterMapStore.RegisterMapFile file = RegisterMapStore.Load(path);
            CollectionAssert.AreEqual(holding, file.Holding);
            CollectionAssert.AreEqual(coils, file.Coils);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadNullJsonThrowsInvalidData()
    {
        string path = TempPath();
        try
        {
            File.WriteAllText(path, "null");
            Assert.ThrowsExactly<InvalidDataException>(() => RegisterMapStore.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void NullInputsThrow()
    {
        Assert.ThrowsExactly<ArgumentException>(() => RegisterMapStore.Save(" ", [], []));
        Assert.ThrowsExactly<ArgumentNullException>(() => RegisterMapStore.Save(TempPath(), null!, []));
        Assert.ThrowsExactly<ArgumentNullException>(() => RegisterMapStore.Save(TempPath(), [], null!));
        Assert.ThrowsExactly<ArgumentException>(() => RegisterMapStore.Load(" "));
    }

    private static string TempPath() => Path.Combine(Path.GetTempPath(), $"regmap-{Guid.NewGuid():N}.json");
}

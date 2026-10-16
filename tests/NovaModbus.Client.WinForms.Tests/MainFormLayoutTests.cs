using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace NovaModbus.Client.WinForms.Tests;

/// <summary>Guards the hand-written Designer layout (regression: invisible controls).</summary>
[TestClass]
public sealed class MainFormLayoutTests
{
    [TestMethod]
    public void ReadWriteControlsHaveNonZeroSize()
    {
        using var form = new MainForm();
        form.ClientSize = new Size(1000, 650);
        form.PerformLayout();
        // Headless TabControl gives pages no size; size the page explicitly.
        var page = (TabPage)typeof(MainForm)
            .GetField("_readWritePage", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;
        page.ClientSize = new Size(900, 600);
        page.PerformLayout();
        AssertHasSize(form, "_slaveBox");
        AssertHasSize(form, "_startBox");
        AssertHasSize(form, "_countBox");
        AssertHasSize(form, "_valuesBox");
        AssertHasSize(form, "_resultBox");
    }

    private static void AssertHasSize(Form form, string field)
    {
        var box = (Control)typeof(MainForm)
            .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;
        Assert.IsTrue(box.Width > 0 && box.Height > 0, $"{field} has zero size: {box.Size}");
    }
}

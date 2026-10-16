using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace NovaModbus.Slave.WinForms.Tests;

/// <summary>Guards the hand-written Designer layout (regression: invisible controls).</summary>
[TestClass]
public sealed class MainFormLayoutTests
{
    [TestMethod]
    public void EditorControlsHaveNonZeroSize()
    {
        using var form = new MainForm();
        form.ClientSize = new Size(1000, 650);
        form.PerformLayout();
        // Headless TabControl gives pages no size; size the pages explicitly.
        foreach (string page in new[] { "_holdingPage", "_coilsPage", "_floatPage" })
        {
            var tab = (TabPage)typeof(MainForm)
                .GetField(page, BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(form)!;
            tab.ClientSize = new Size(900, 500);
            tab.PerformLayout();
        }

        AssertHasSize(form, "_holdingAddressBox");
        AssertHasSize(form, "_holdingValueBox");
        AssertHasSize(form, "_coilAddressBox");
        AssertHasSize(form, "_holdingGrid");
        AssertHasSize(form, "_coilGrid");
        AssertHasSize(form, "_floatAddressBox");
        AssertHasSize(form, "_floatOrderBox");
    }

    private static void AssertHasSize(Form form, string field)
    {
        var box = (Control)typeof(MainForm)
            .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;
        Assert.IsTrue(box.Width > 0 && box.Height > 0, $"{field} has zero size: {box.Size}");
    }
}

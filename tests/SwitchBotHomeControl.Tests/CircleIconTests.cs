using System.Drawing;
using SwitchBotHomeControl.Tray;

namespace SwitchBotHomeControl.Tests;

public class CircleIconTests
{
    [Fact]
    public void CreateIcon_StaysUsableAfterTheSourceHandleIsDestroyed()
    {
        var fill = ColorTranslator.FromHtml("#8BC43F");

        using var icon = CircleIcon.CreateIcon(fill, 32);
        using var bitmap = icon.ToBitmap();

        Assert.Equal(new Size(32, 32), icon.Size);
        Assert.Equal(fill.ToArgb(), bitmap.GetPixel(16, 16).ToArgb());
        Assert.Equal(0, bitmap.GetPixel(0, 0).A); // corners stay transparent
    }
}

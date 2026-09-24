using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace SwitchBotHomeControl.Tray;

/// <summary>
/// Draws a filled circle for the tray icon and the menu items.
/// </summary>
public static class CircleIcon
{
    public static Bitmap CreateBitmap(Color fill, int size)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        // A thin dark outline keeps light colors visible on both light and dark taskbars
        var outlineWidth = Math.Max(1f, size / 16f);
        var inset = outlineWidth / 2 + 0.5f;
        var bounds = new RectangleF(inset, inset, size - inset * 2, size - inset * 2);
        using var brush = new SolidBrush(fill);
        using var pen = new Pen(Color.FromArgb(160, 0, 0, 0), outlineWidth);
        graphics.FillEllipse(brush, bounds);
        graphics.DrawEllipse(pen, bounds);
        return bitmap;
    }

    /// <summary>
    /// Returns an icon that owns its handle; dispose it when replaced.
    /// </summary>
    public static Icon CreateIcon(Color fill, int size)
    {
        using var bitmap = CreateBitmap(fill, size);
        var handle = bitmap.GetHicon();
        try
        {
            using var borrowed = Icon.FromHandle(handle);
            // Clone copies the image into a handle owned by the new Icon, so the original can be destroyed
            return (Icon)borrowed.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}

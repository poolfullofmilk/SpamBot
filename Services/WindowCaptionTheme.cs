using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SpamBot.Services;

internal static class WindowCaptionTheme
{
    private const int UseImmersiveDarkMode = 20;

    public static void Apply(Window window, bool isDark)
    {
        nint handle = new WindowInteropHelper(window).Handle;
        if (handle == 0)
            return;

        // Attribute 20 Is Windows 11, Windows 10 Builds Before 18985 Used 19
        int enabled = isDark ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref enabled, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        nint window,
        int attribute,
        ref int value,
        int size
    );
}

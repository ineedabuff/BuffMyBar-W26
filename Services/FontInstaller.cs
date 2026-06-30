using Microsoft.Win32;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace BuffBarW26.Services;

public static class FontInstaller
{
    private const string FontUrl =
        "https://github.com/ryanoasis/nerd-fonts/releases/latest/download/JetBrainsMono.zip";

    private static readonly string FontDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Windows", "Fonts");

    public static async Task EnsureAsync()
    {
        if (IsInstalled()) return;

        Directory.CreateDirectory(FontDir);

        var tempDir = Path.Combine(Path.GetTempPath(), "BuffBar_Fonts");
        var zipPath = Path.Combine(tempDir, "JetBrainsMono.zip");

        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);

        Directory.CreateDirectory(tempDir);

        using var http = new HttpClient();
        var bytes = await http.GetByteArrayAsync(FontUrl);
        await File.WriteAllBytesAsync(zipPath, bytes);

        ZipFile.ExtractToDirectory(zipPath, tempDir);

        foreach (var font in Directory.GetFiles(tempDir, "*.ttf", SearchOption.AllDirectories))
        {
            var file = Path.GetFileName(font);
            var target = Path.Combine(FontDir, file);

            File.Copy(font, target, true);

            using var key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows NT\CurrentVersion\Fonts");

            key?.SetValue($"{Path.GetFileNameWithoutExtension(file)} (TrueType)", target);
        }

        BroadcastFontChange();
    }

    private static bool IsInstalled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows NT\CurrentVersion\Fonts");

        if (key == null) return false;

        return key.GetValueNames().Any(x =>
            x.Contains("JetBrainsMono", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("JetBrains Mono", StringComparison.OrdinalIgnoreCase));
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        UIntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out UIntPtr result);

    private static void BroadcastFontChange()
    {
        const uint WM_FONTCHANGE = 0x001D;
        const uint SMTO_ABORTIFHUNG = 0x0002;

        SendMessageTimeout(
            new IntPtr(0xffff),
            WM_FONTCHANGE,
            UIntPtr.Zero,
            IntPtr.Zero,
            SMTO_ABORTIFHUNG,
            1000,
            out _);
    }
}

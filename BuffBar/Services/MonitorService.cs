using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BuffBar.Interop;
using static BuffBar.Interop.NativeMethods;

namespace BuffBar.Services;

/// <summary>Un moniteur détecté.</summary>
public readonly record struct MonitorEntry(IntPtr Handle, bool IsPrimary);

/// <summary>Énumère les moniteurs physiques connectés.</summary>
public static class MonitorService
{
    public static List<MonitorEntry> Enumerate()
    {
        var result = new List<MonitorEntry>();

        MonitorEnumProc callback = (IntPtr hMon, IntPtr hdc, ref RECT rect, IntPtr data) =>
        {
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            bool primary = false;
            if (GetMonitorInfo(hMon, ref mi))
                primary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0;

            result.Add(new MonitorEntry(hMon, primary));
            return true;
        };

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        return result;
    }
}

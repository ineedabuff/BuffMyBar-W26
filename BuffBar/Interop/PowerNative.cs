using System;
using System.Runtime.InteropServices;

namespace BuffBar.Interop;

/// <summary>
/// Accès à l'état d'alimentation via kernel32.GetSystemPowerStatus.
/// Aucune dépendance : P/Invoke direct.
/// </summary>
internal static class PowerNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;        // 0 = secteur débranché, 1 = branché, 255 = inconnu
        public byte BatteryFlag;         // bit 8 = en charge, 128 = pas de batterie, 255 = inconnu
        public byte BatteryLifePercent;  // 0-100, 255 = inconnu
        public byte SystemStatusFlag;
        public int BatteryLifeTime;      // secondes restantes, -1 = inconnu
        public int BatteryFullLifeTime;  // secondes à pleine charge, -1 = inconnu
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);
}

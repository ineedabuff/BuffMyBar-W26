using BuffBar.Interop;

namespace BuffBar.Services;

/// <summary>État de la batterie à un instant donné.</summary>
public readonly record struct BatteryInfo(int Percent, bool Charging, bool Present);

/// <summary>
/// Lit l'état de la batterie. Renvoie Present=false sur un poste fixe
/// (le widget se masque alors automatiquement).
/// </summary>
public static class BatteryService
{
    private const byte NoBattery = 128;
    private const byte Unknown = 255;
    private const byte ChargingFlag = 8;

    public static BatteryInfo Read()
    {
        if (!PowerNative.GetSystemPowerStatus(out var s))
            return new BatteryInfo(0, false, false);

        bool present = s.BatteryFlag != NoBattery
                       && (s.BatteryFlag & NoBattery) == 0
                       && s.BatteryLifePercent != Unknown;

        if (!present)
            return new BatteryInfo(0, false, false);

        bool charging = (s.BatteryFlag & ChargingFlag) != 0 || s.ACLineStatus == 1;
        int percent = s.BatteryLifePercent;

        return new BatteryInfo(percent, charging, true);
    }
}

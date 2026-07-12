using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace BuffBar.Services;

/// <summary>Dispositif Bluetooth connecté (Battery = -1 si inconnu).</summary>
public readonly record struct BtDevice(string Name, int Battery);

/// <summary>
/// Énumère les dispositifs Bluetooth connectés (classique + BLE) via WinRT et
/// tente de lire leur niveau de batterie (propriété PnP). Le niveau n'est pas
/// exposé par tous les périphériques/pilotes : dans ce cas seul le nom s'affiche.
/// </summary>
public sealed class BluetoothService
{
    // Propriété PnP du niveau de batterie Bluetooth.
    private const string BatteryKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";

    public async Task<IReadOnlyList<BtDevice>> ReadAsync()
    {
        var list = new List<BtDevice>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await AddFromSelector(
                BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected),
                list, seen);
        }
        catch { /* ignore */ }

        try
        {
            await AddFromSelector(
                BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected),
                list, seen);
        }
        catch { /* ignore */ }

        return list;
    }

    /// <summary>
    /// Vrai pour un nom non résolu (ex. périphérique BLE annonçant « 4 ») : que des
    /// chiffres. Ces entrées parasites masquaient le vrai dispositif dans le widget.
    /// </summary>
    private static bool IsUnresolvedName(string name)
    {
        foreach (char c in name)
            if (!char.IsDigit(c))
                return false;
        return true;
    }

    private static async Task AddFromSelector(string selector, List<BtDevice> list, HashSet<string> seen)
    {
        DeviceInformationCollection found =
            await DeviceInformation.FindAllAsync(selector, new[] { BatteryKey });

        foreach (DeviceInformation di in found)
        {
            string name = (di.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name) || IsUnresolvedName(name) || !seen.Add(name))
                continue;

            int battery = -1;
            if (di.Properties.TryGetValue(BatteryKey, out object? v) && v is not null)
            {
                try { battery = Convert.ToInt32(v); } catch { battery = -1; }
            }

            list.Add(new BtDevice(name, battery));
        }
    }
}

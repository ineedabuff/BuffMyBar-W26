using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Bluetooth;

/// <summary>
/// Module Bluetooth : nom + batterie du dispositif connecté.
/// S'il y en a plusieurs, alternance toutes les ~3 s. Se masque si aucun.
/// </summary>
public partial class BluetoothWidget : UserControl, IBarWidget
{
    private readonly BluetoothService _service = new();
    private IDisposable? _cycleTick;    // alternance ~3 s
    private IDisposable? _refreshTick;  // re-énumération

    private IReadOnlyList<BtDevice> _devices = Array.Empty<BtDevice>();
    private int _index;
    private bool _busy;

    public string WidgetId => "bluetooth";
    public FrameworkElement View => this;

    public BluetoothWidget()
    {
        InitializeComponent();
        Root.Visibility = Visibility.Collapsed;

        Loaded += async (_, _) =>
        {
            await RefreshList();
            _cycleTick?.Dispose();
            _refreshTick?.Dispose();
            _cycleTick = WidgetScheduler.Subscribe(TimeSpan.FromSeconds(3), CycleDevice);
            _refreshTick = WidgetScheduler.Subscribe(TimeSpan.FromSeconds(8), () => _ = RefreshList());
        };
        Unloaded += (_, _) =>
        {
            _cycleTick?.Dispose(); _cycleTick = null;
            _refreshTick?.Dispose(); _refreshTick = null;
        };
    }

    private void CycleDevice()
    {
        if (_devices.Count > 1)
        {
            _index = (_index + 1) % _devices.Count;
            UpdateDisplay();
        }
    }

    private async Task RefreshList()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            _devices = await _service.ReadAsync();
            if (_index >= _devices.Count) _index = 0;
            UpdateDisplay();
        }
        finally
        {
            _busy = false;
        }
    }

    private void UpdateDisplay()
    {
        if (_devices.Count == 0)
        {
            Root.Visibility = Visibility.Collapsed;
            return;
        }

        Root.Visibility = Visibility.Visible;
        BtDevice d = _devices[_index % _devices.Count];

        DeviceName.Text = d.Name;
        Batt.Text = d.Battery >= 0 ? $"{d.Battery}%" : string.Empty;
        Batt.Visibility = d.Battery >= 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}

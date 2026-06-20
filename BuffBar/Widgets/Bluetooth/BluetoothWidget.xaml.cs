using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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
    private readonly DispatcherTimer _cycle;    // alternance ~3 s
    private readonly DispatcherTimer _refresh;  // re-énumération

    private IReadOnlyList<BtDevice> _devices = Array.Empty<BtDevice>();
    private int _index;
    private bool _busy;

    public string WidgetId => "bluetooth";
    public FrameworkElement View => this;

    public BluetoothWidget()
    {
        InitializeComponent();
        Root.Visibility = Visibility.Collapsed;

        _cycle = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _cycle.Tick += (_, _) =>
        {
            if (_devices.Count > 1)
            {
                _index = (_index + 1) % _devices.Count;
                UpdateDisplay();
            }
        };

        _refresh = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(8)
        };
        _refresh.Tick += async (_, _) => await RefreshList();

        Loaded += async (_, _) => { await RefreshList(); _cycle.Start(); _refresh.Start(); };
        Unloaded += (_, _) => { _cycle.Stop(); _refresh.Stop(); };
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

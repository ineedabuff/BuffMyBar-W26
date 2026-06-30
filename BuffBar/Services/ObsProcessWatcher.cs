using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;

namespace BuffBar.Services;

public sealed class ObsProcessWatcher : IDisposable
{
    private readonly DispatcherTimer _timer;
    private bool _isRunning;

    public event EventHandler<bool>? IsRunningChanged;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning == value)
                return;

            _isRunning = value;
            IsRunningChanged?.Invoke(this, value);
        }
    }

    public ObsProcessWatcher(TimeSpan? interval = null)
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = interval ?? TimeSpan.FromSeconds(2)
        };
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        Refresh();
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    public void Refresh()
    {
        try
        {
            IsRunning = Process.GetProcesses()
                .Any(p => IsObsProcess(p));
        }
        catch
        {
            IsRunning = false;
        }
    }

    private static bool IsObsProcess(Process process)
    {
        try
        {
            string name = process.ProcessName;
            return name.Equals("obs64", StringComparison.OrdinalIgnoreCase)
                || name.Equals("obs32", StringComparison.OrdinalIgnoreCase)
                || name.Equals("obs", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private void OnTick(object? sender, EventArgs e) => Refresh();

    public void Dispose()
    {
        Stop();
        _timer.Tick -= OnTick;
    }
}
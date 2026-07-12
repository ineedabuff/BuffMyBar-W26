using System;
using System.Diagnostics;
using System.Linq;

namespace BuffBar.Services;

public sealed class ObsProcessWatcher : IDisposable
{
    private readonly TimeSpan _interval;
    private IDisposable? _tick;
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
        _interval = interval ?? TimeSpan.FromSeconds(2);
    }

    public void Start()
    {
        Refresh();
        _tick?.Dispose();
        _tick = WidgetScheduler.Subscribe(_interval, Refresh);
    }

    public void Stop()
    {
        _tick?.Dispose();
        _tick = null;
    }

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

    public void Dispose() => Stop();
}
using System;
using System.Runtime.InteropServices;

namespace BuffBar.Services;

/// <summary>
/// Lightweight system metrics used by the System Indicators widget.
/// No external dependencies, no NuGet packages.
/// </summary>
public sealed class SystemMetricsService
{
    private CpuSample? _lastCpuSample;

    public SystemMetricsSnapshot Read()
    {
        int? cpu = ReadCpuPercent();
        int? ram = ReadRamPercent();
        int? gpu = ReadGpuPercent();

        return new SystemMetricsSnapshot(cpu, ram, gpu);
    }

    private int? ReadCpuPercent()
    {
        if (!GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime))
            return null;

        ulong idle = ToUInt64(idleTime);
        ulong kernel = ToUInt64(kernelTime);
        ulong user = ToUInt64(userTime);
        ulong total = kernel + user;

        var current = new CpuSample(idle, total);

        if (_lastCpuSample is not { } previous)
        {
            _lastCpuSample = current;
            return null;
        }

        ulong totalDelta = current.Total - previous.Total;
        ulong idleDelta = current.Idle - previous.Idle;

        _lastCpuSample = current;

        if (totalDelta == 0 || idleDelta > totalDelta)
            return null;

        double usage = 100.0 * (1.0 - (double)idleDelta / totalDelta);
        return ClampPercent((int)Math.Round(usage));
    }

    private static int? ReadRamPercent()
    {
        var status = new MEMORYSTATUSEX();
        status.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();

        if (!GlobalMemoryStatusEx(ref status))
            return null;

        return ClampPercent((int)status.dwMemoryLoad);
    }

    /// <summary>
    /// GPU usage is intentionally conservative for now.
    /// Windows exposes GPU counters through performance counters / WMI providers that
    /// are not always present and may require extra framework packages.
    /// Returning null keeps the widget hidden instead of showing unreliable data.
    /// </summary>
    private static int? ReadGpuPercent()
        => null;

    private static int ClampPercent(int value)
        => Math.Max(0, Math.Min(100, value));

    private static ulong ToUInt64(FILETIME time)
        => ((ulong)time.dwHighDateTime << 32) | time.dwLowDateTime;

    private readonly record struct CpuSample(ulong Idle, ulong Total);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(
        out FILETIME lpIdleTime,
        out FILETIME lpKernelTime,
        out FILETIME lpUserTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}

public readonly record struct SystemMetricsSnapshot(int? CpuPercent, int? RamPercent, int? GpuPercent);

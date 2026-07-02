using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;

namespace BuffBar.Services;

/// <summary>
/// Lightweight system metrics used by the System Indicators widget.
/// CPU/RAM/GPU stay native/lightweight; CPU temperature is read through
/// LibreHardwareMonitor when available, with a safe null fallback.
/// </summary>
public sealed class SystemMetricsService : IDisposable
{
    private CpuSample? _lastCpuSample;
    private readonly GpuUsageReader _gpu = new();
    private readonly CpuTemperatureReader _cpuTemperature = new();
    private bool _disposed;

    public SystemMetricsSnapshot Read()
    {
        if (_disposed)
            return default;

        int? cpu = ReadCpuPercent();
        int? ram = ReadRamPercent();
        int? gpu = _gpu.ReadGpuPercent();
        int? cpuTemperature = _cpuTemperature.ReadCelsius();

        return new SystemMetricsSnapshot(cpu, ram, gpu, cpuTemperature);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _gpu.Dispose();
        _cpuTemperature.Dispose();
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
        var status = new MEMORYSTATUSEX
        {
            dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
        };

        if (!GlobalMemoryStatusEx(ref status))
            return null;

        return ClampPercent((int)status.dwMemoryLoad);
    }

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

    /// <summary>
    /// Reads CPU package temperature through LibreHardwareMonitor.
    ///
    /// The reader is cached to avoid polling sensors too often. If the current
    /// hardware/driver stack does not expose CPU sensors, it returns null and
    /// the widget keeps displaying CPU usage without a temperature suffix.
    /// </summary>
    private sealed class CpuTemperatureReader : IDisposable
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);

        private Computer? _computer;
        private DateTime _lastReadUtc = DateTime.MinValue;
        private int? _cachedTemperature;
        private bool _failed;
        private bool _disposed;

        public int? ReadCelsius()
        {
            if (_disposed || _failed)
                return null;

            DateTime now = DateTime.UtcNow;
            if (now - _lastReadUtc < CacheDuration)
                return _cachedTemperature;

            _lastReadUtc = now;
            _cachedTemperature = TryReadCelsius();
            return _cachedTemperature;
        }

        private int? TryReadCelsius()
        {
            try
            {
                Computer computer = EnsureComputer();

                foreach (IHardware hardware in computer.Hardware)
                    hardware.Update();

                List<ISensor> temperatureSensors = computer.Hardware
                    .Where(h => h.HardwareType == HardwareType.Cpu)
                    .SelectMany(FlattenHardware)
                    .SelectMany(h => h.Sensors)
                    .Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue)
                    .ToList();

                if (temperatureSensors.Count == 0)
                    return null;

                ISensor? preferred =
                    temperatureSensors.FirstOrDefault(IsPreferredCpuPackageSensor) ??
                    temperatureSensors.FirstOrDefault(s => s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase)) ??
                    temperatureSensors.FirstOrDefault(s => s.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase)) ??
                    temperatureSensors.FirstOrDefault(s => s.Name.Contains("CCD", StringComparison.OrdinalIgnoreCase));

                float value = preferred?.Value
                    ?? temperatureSensors.Max(s => s.Value ?? 0);

                int celsius = (int)Math.Round(value);

                return celsius is > 0 and < 125
                    ? celsius
                    : null;
            }
            catch
            {
                _failed = true;
                return null;
            }
        }

        private Computer EnsureComputer()
        {
            if (_computer is not null)
                return _computer;

            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsMotherboardEnabled = false,
                IsMemoryEnabled = false,
                IsGpuEnabled = false,
                IsStorageEnabled = false,
                IsNetworkEnabled = false,
                IsControllerEnabled = false
            };

            _computer.Open();
            return _computer;
        }

        private static IEnumerable<IHardware> FlattenHardware(IHardware hardware)
        {
            yield return hardware;

            foreach (IHardware subHardware in hardware.SubHardware)
            {
                foreach (IHardware flattened in FlattenHardware(subHardware))
                    yield return flattened;
            }
        }

        private static bool IsPreferredCpuPackageSensor(ISensor sensor)
        {
            string name = sensor.Name;

            return name.Equals("CPU Package", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Core Max", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Package", StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _computer?.Close();
            }
            catch
            {
                // Best-effort cleanup only.
            }

            _computer = null;
        }
    }

    /// <summary>
    /// Reads Windows GPU Engine counters through PDH.
    ///
    /// On hybrid laptops, Windows exposes per-engine GPU counters. The instance name
    /// does not reliably expose the vendor name, so the reader uses the most active
    /// 3D engine as the displayed GPU value. In practice, when the dGPU is active
    /// for games or rendering workloads, that engine is the one that surfaces here.
    /// If the counters are unavailable, the value remains hidden.
    /// </summary>
    private sealed class GpuUsageReader : IDisposable
    {
        private const uint ERROR_SUCCESS = 0;
        private const uint PDH_MORE_DATA = 0x800007D2;
        private const uint PDH_FMT_DOUBLE = 0x00000200;
        private const string GpuCounterPath = @"\GPU Engine(*)\Utilization Percentage";

        private IntPtr _query;
        private IntPtr _counter;
        private bool _initialized;
        private bool _disposed;
        private bool _hasFirstSample;

        public int? ReadGpuPercent()
        {
            if (_disposed)
                return null;

            if (!_initialized && !Initialize())
                return null;

            uint collectStatus = PdhCollectQueryData(_query);
            if (collectStatus != ERROR_SUCCESS)
                return null;

            // PDH counters need one warm-up sample before a useful formatted value.
            if (!_hasFirstSample)
            {
                _hasFirstSample = true;
                return null;
            }

            uint bufferSize = 0;
            uint itemCount = 0;
            uint status = PdhGetFormattedCounterArray(
                _counter,
                PDH_FMT_DOUBLE,
                ref bufferSize,
                ref itemCount,
                IntPtr.Zero);

            if (status != PDH_MORE_DATA || bufferSize == 0 || itemCount == 0)
                return null;

            IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
            try
            {
                status = PdhGetFormattedCounterArray(
                    _counter,
                    PDH_FMT_DOUBLE,
                    ref bufferSize,
                    ref itemCount,
                    buffer);

                if (status != ERROR_SUCCESS)
                    return null;

                double max3D = 0;
                int structSize = Marshal.SizeOf<PDH_FMT_COUNTERVALUE_ITEM_DOUBLE>();

                for (int i = 0; i < itemCount; i++)
                {
                    IntPtr itemPtr = IntPtr.Add(buffer, i * structSize);
                    var item = Marshal.PtrToStructure<PDH_FMT_COUNTERVALUE_ITEM_DOUBLE>(itemPtr);
                    string instance = Marshal.PtrToStringUni(item.szName) ?? string.Empty;

                    if (!Is3DEngine(instance))
                        continue;

                    if ((item.FmtValue.CStatus & 0xC0000000) != 0)
                        continue;

                    if (double.IsNaN(item.FmtValue.doubleValue) || double.IsInfinity(item.FmtValue.doubleValue))
                        continue;

                    max3D = Math.Max(max3D, item.FmtValue.doubleValue);
                }

                if (max3D <= 0.0)
                    return null;

                return ClampPercent((int)Math.Round(max3D));
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static bool Is3DEngine(string instance)
            => instance.IndexOf("engtype_3D", StringComparison.OrdinalIgnoreCase) >= 0;

        private bool Initialize()
        {
            if (PdhOpenQuery(null, UIntPtr.Zero, out _query) != ERROR_SUCCESS)
                return false;

            if (PdhAddEnglishCounter(_query, GpuCounterPath, UIntPtr.Zero, out _counter) != ERROR_SUCCESS)
            {
                Dispose();
                return false;
            }

            _initialized = true;
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_query != IntPtr.Zero)
            {
                PdhCloseQuery(_query);
                _query = IntPtr.Zero;
                _counter = IntPtr.Zero;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_FMT_COUNTERVALUE_ITEM_DOUBLE
        {
            public IntPtr szName;
            public PDH_FMT_COUNTERVALUE_DOUBLE FmtValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PDH_FMT_COUNTERVALUE_DOUBLE
        {
            public uint CStatus;
            public double doubleValue;
        }

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhOpenQuery(
            string? szDataSource,
            UIntPtr dwUserData,
            out IntPtr phQuery);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        private static extern uint PdhAddEnglishCounter(
            IntPtr hQuery,
            string szFullCounterPath,
            UIntPtr dwUserData,
            out IntPtr phCounter);

        [DllImport("pdh.dll")]
        private static extern uint PdhCollectQueryData(IntPtr hQuery);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode, EntryPoint = "PdhGetFormattedCounterArrayW")]
        private static extern uint PdhGetFormattedCounterArray(
            IntPtr hCounter,
            uint dwFormat,
            ref uint lpdwBufferSize,
            ref uint lpdwItemCount,
            IntPtr ItemBuffer);

        [DllImport("pdh.dll")]
        private static extern uint PdhCloseQuery(IntPtr hQuery);
    }
}

public readonly record struct SystemMetricsSnapshot(
    int? CpuPercent,
    int? RamPercent,
    int? GpuPercent,
    int? CpuTemperatureCelsius);

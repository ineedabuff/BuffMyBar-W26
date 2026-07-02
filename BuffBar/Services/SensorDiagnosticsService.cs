using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using LibreHardwareMonitor.Hardware;

namespace BuffBar.Services;

/// <summary>
/// One-shot LibreHardwareMonitor diagnostic dump.
///
/// Purpose:
/// - discover the exact sensor names exposed by the current machine;
/// - avoid guessing CPU/GPU temperature sensor names;
/// - write a readable log under LocalAppData without crashing the bar.
///
/// Log path (même dossier que settings.json / buffbar.log) :
/// %AppData%\BuffMyBar-W26\logs\sensors.log
/// </summary>
public sealed class SensorDiagnosticsService : IDisposable
{
    private readonly object _sync = new();
    private Computer? _computer;
    private bool _captured;
    private bool _disposed;

    public string LogPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BuffMyBar-W26",
        "logs",
        "sensors.log");

    public void CaptureOnce()
    {
        if (_disposed || _captured)
            return;

        lock (_sync)
        {
            if (_disposed || _captured)
                return;

            _captured = true;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.WriteAllText(LogPath, BuildDiagnosticReport(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                TryWriteFallback(ex);
            }
        }
    }

    private string BuildDiagnosticReport()
    {
        var sb = new StringBuilder();

        sb.AppendLine("BuffMyBar sensor diagnostics");
        sb.AppendLine("================================");
        sb.AppendLine($"Generated UTC : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z");
        sb.AppendLine($"Generated local: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Process admin  : {IsAdministrator()}");
        sb.AppendLine($"OS             : {Environment.OSVersion}");
        sb.AppendLine($"Runtime        : {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"App directory  : {AppContext.BaseDirectory}");
        sb.AppendLine($"LHM version    : {typeof(Computer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? typeof(Computer).Assembly.GetName().Version?.ToString() ?? "unknown"}");
        sb.AppendLine();

        try
        {
            Computer computer = EnsureComputer();
            UpdateAll(computer.Hardware);

            if (computer.Hardware.Count == 0)
            {
                sb.AppendLine("No hardware detected by LibreHardwareMonitor.");
                sb.AppendLine("Try running BuffMyBar as Administrator once to check if sensor access is restricted.");
                return sb.ToString();
            }

            foreach (IHardware hardware in computer.Hardware)
                AppendHardware(sb, hardware, 0);

            sb.AppendLine();
            sb.AppendLine("CPU temperature candidates");
            sb.AppendLine("--------------------------");

            List<ISensor> cpuTemperatures = computer.Hardware
                .SelectMany(FlattenHardware)
                .Where(h => h.HardwareType == HardwareType.Cpu)
                .SelectMany(h => h.Sensors)
                .Where(s => s.SensorType == SensorType.Temperature)
                .ToList();

            if (cpuTemperatures.Count == 0)
            {
                sb.AppendLine("No CPU temperature sensor found.");
                sb.AppendLine("Possible causes:");
                sb.AppendLine("- firmware does not expose CPU sensors to user mode;");
                sb.AppendLine("- sensors require Administrator privileges;");
                sb.AppendLine("- laptop vendor blocks direct sensor access;");
                sb.AppendLine("- LibreHardwareMonitor does not yet support this CPU/sensor controller.");
            }
            else
            {
                foreach (ISensor sensor in cpuTemperatures.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
                    AppendSensor(sb, sensor, 0);
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("Diagnostic failed.");
            sb.AppendLine(ex.ToString());
        }

        return sb.ToString();
    }

    private Computer EnsureComputer()
    {
        if (_computer is not null)
            return _computer;

        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true,
            IsNetworkEnabled = true,
            IsControllerEnabled = true,
            IsPsuEnabled = true,
            IsBatteryEnabled = true
        };

        _computer.Open();
        return _computer;
    }

    private static void UpdateAll(IEnumerable<IHardware> hardwareList)
    {
        foreach (IHardware hardware in hardwareList)
        {
            try
            {
                hardware.Update();
            }
            catch
            {
                // Keep diagnostic resilient: one broken device must not stop the dump.
            }

            UpdateAll(hardware.SubHardware);
        }
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

    private static void AppendHardware(StringBuilder sb, IHardware hardware, int depth)
    {
        string indent = new(' ', depth * 2);

        sb.AppendLine($"{indent}Hardware: {hardware.Name}");
        sb.AppendLine($"{indent}  Type      : {hardware.HardwareType}");
        sb.AppendLine($"{indent}  Identifier: {hardware.Identifier}");

        if (hardware.Sensors.Length == 0)
        {
            sb.AppendLine($"{indent}  Sensors   : none");
        }
        else
        {
            sb.AppendLine($"{indent}  Sensors:");
            foreach (ISensor sensor in hardware.Sensors.OrderBy(s => s.SensorType).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
                AppendSensor(sb, sensor, depth + 2);
        }

        foreach (IHardware subHardware in hardware.SubHardware)
            AppendHardware(sb, subHardware, depth + 1);

        sb.AppendLine();
    }

    private static void AppendSensor(StringBuilder sb, ISensor sensor, int depth)
    {
        string indent = new(' ', depth * 2);
        string value = sensor.Value.HasValue
            ? sensor.Value.Value.ToString("0.##", CultureInfo.InvariantCulture)
            : "null";

        string min = sensor.Min.HasValue
            ? sensor.Min.Value.ToString("0.##", CultureInfo.InvariantCulture)
            : "null";

        string max = sensor.Max.HasValue
            ? sensor.Max.Value.ToString("0.##", CultureInfo.InvariantCulture)
            : "null";

        sb.AppendLine($"{indent}- Name      : {sensor.Name}");
        sb.AppendLine($"{indent}  Type      : {sensor.SensorType}");
        sb.AppendLine($"{indent}  Value     : {value}");
        sb.AppendLine($"{indent}  Min / Max : {min} / {max}");
        sb.AppendLine($"{indent}  Identifier: {sensor.Identifier}");
    }

    private static bool IsAdministrator()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private void TryWriteFallback(Exception ex)
    {
        try
        {
            string fallback = Path.Combine(Path.GetTempPath(), "BuffMyBar-sensors.log");
            File.WriteAllText(fallback, ex.ToString(), Encoding.UTF8);
        }
        catch
        {
            // Last-resort best effort. Diagnostics must never crash the bar.
        }
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

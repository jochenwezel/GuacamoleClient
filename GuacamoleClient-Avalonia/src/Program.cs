using Avalonia;
using GuacamoleClient.Common.Localization;
using GuacamoleClient.Common.Settings;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using WebViewControl;

namespace GuacClient;
internal static class Program
{
    private const string DisableGpuArgument = "--disable-gpu";
    private const string EnableGpuArgument = "--enable-gpu";
    private const string LauncherChildArgument = "--guacamoleclient-launcher-child";
    private const string LauncherModeVariable = "GUACAMOLECLIENT_LAUNCHER_CHILD";
    private const string DisableGpuVariable = "GUACAMOLECLIENT_DISABLE_GPU";
    private static readonly TimeSpan EarlyStartupFailureWindow = TimeSpan.FromSeconds(10);

    [STAThread]
    public static int Main(string[] args)
    {
        RegisterGlobalExceptionHandlers();

        try
        {
            if (TryShowCommandLineHelp(args))
                return 0;

            if (TryRunLinuxLauncher(args, out int launcherExitCode))
                return launcherExitCode;

            ConfigureBrowserCompatibilitySwitches(args);
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return 0;
        }
        catch (Exception ex)
        {
            StartupErrorDialog.Show(LocalizationProvider.Get(LocalizationKeys.AppStart_StartupError_Title), BuildErrorMessage(ex));
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static bool TryShowCommandLineHelp(string[] args)
    {
        if (!args.Any(IsHelpArgument))
            return false;

        Console.WriteLine("GuacamoleClient Avalonia");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  guacamoleclient [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --help                         Show this help text.");
        Console.WriteLine("  --enable-gpu                       Start once with GPU hardware acceleration enabled.");
        Console.WriteLine("  --disable-gpu                      Start once with GPU hardware acceleration disabled.");
        Console.WriteLine();
        Console.WriteLine("Linux CEF diagnostics:");
        Console.WriteLine("  --no-sandbox                       Forward Chromium/CEF no-sandbox mode.");
        Console.WriteLine("  --disable-dev-shm-usage            Avoid Chromium shared memory usage under /dev/shm.");
        Console.WriteLine("  --disable-features=<features>      Disable Chromium/CEF features, for example Vulkan or VA-API.");
        Console.WriteLine("  --enable-features=<features>       Enable Chromium/CEF features.");
        Console.WriteLine("  --disable-software-rasterizer      Disable Chromium software rasterizer.");
        Console.WriteLine("  --use-angle=<backend>              Select ANGLE backend, for example swiftshader.");
        Console.WriteLine("  --use-gl=<backend>                 Select GL backend, for example swiftshader or desktop.");
        Console.WriteLine("  --enable-logging[=stderr]          Enable Chromium/CEF logging.");
        Console.WriteLine("  --log-file=<path>                  Write Chromium/CEF log output to a file.");
        Console.WriteLine("  --v=<level>                        Set Chromium/CEF verbose logging level.");
        Console.WriteLine();
        Console.WriteLine("Environment:");
        Console.WriteLine("  GUACAMOLECLIENT_DISABLE_GPU=1      Prefer disabled GPU hardware acceleration.");
        return true;
    }

    private static bool IsHelpArgument(string arg)
        => string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)
           || string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase)
           || string.Equals(arg, "/?", StringComparison.OrdinalIgnoreCase);

    private static bool TryRunLinuxLauncher(string[] args, out int exitCode)
    {
        exitCode = 0;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || IsLauncherChild(args))
        {
            return false;
        }

        if (IsDisableGpuRequested(args) || IsEnableGpuRequested(args))
        {
            var explicitAttempt = RunChildAndWatchStartup(args);
            if (explicitAttempt.FailedEarly)
                ShowEarlyStartupFailure(explicitAttempt, disableGpuWasUsed: IsDisableGpuRequested(args));

            exitCode = explicitAttempt.ExitCode;
            return true;
        }

        bool preferDisableGpu = LoadBrowserCompatibilityState().PreferDisableGpu;
        string[] firstAttemptArgs = preferDisableGpu
            ? AddArgument(args, DisableGpuArgument)
            : args;

        var firstAttempt = RunChildAndWatchStartup(firstAttemptArgs);
        if (!firstAttempt.FailedEarly)
        {
            exitCode = firstAttempt.ExitCode;
            return true;
        }

        if (preferDisableGpu)
        {
            ShowEarlyStartupFailure(firstAttempt, disableGpuWasUsed: true);
            exitCode = firstAttempt.ExitCode;
            return true;
        }

        var fallbackAttempt = RunChildAndWatchStartup(AddArgument(args, DisableGpuArgument));
        if (!fallbackAttempt.FailedEarly)
        {
            SaveBrowserCompatibilityState(preferDisableGpu: true, reason: "early-gpu-child-crash");
            exitCode = fallbackAttempt.ExitCode;
            return true;
        }

        ShowEarlyStartupFailure(fallbackAttempt, disableGpuWasUsed: true);
        exitCode = fallbackAttempt.ExitCode;
        return true;
    }

    private static ChildRunResult RunChildAndWatchStartup(string[] args)
    {
        using var process = StartChildProcess(args);
        bool exitedEarly = process.WaitForExit((int)EarlyStartupFailureWindow.TotalMilliseconds);
        if (!exitedEarly)
        {
            process.WaitForExit();
            return new ChildRunResult(process.ExitCode, FailedEarly: false, args);
        }

        return new ChildRunResult(process.ExitCode, FailedEarly: process.ExitCode != 0, args);
    }

    private static Process StartChildProcess(string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.ProcessPath ?? throw new InvalidOperationException("Current executable path is not available."),
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory
        };

        foreach (string argument in AddArgument(args.Where(IsPublicArgument).ToArray(), LauncherChildArgument))
            startInfo.ArgumentList.Add(argument);

        startInfo.Environment[LauncherModeVariable] = "1";

        var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Unable to start GuacamoleClient child process.");

        return process;
    }

    internal static bool IsGpuHardwareAccelerationDisabledByPreference()
        => LoadBrowserCompatibilityState().PreferDisableGpu;

    internal static void SetGpuHardwareAccelerationEnabledPreference(bool enabled)
        => SaveBrowserCompatibilityState(
            preferDisableGpu: !enabled,
            reason: enabled ? "manual-gpu-enabled" : "manual-gpu-disabled");

    internal static void StartNewApplicationProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.ProcessPath ?? throw new InvalidOperationException("Current executable path is not available."),
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory
        };
        startInfo.Environment.Remove(LauncherModeVariable);
        startInfo.Environment.Remove(DisableGpuVariable);

        Process.Start(startInfo);
    }

    private static void ConfigureBrowserCompatibilitySwitches(string[] args)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return;

        if (IsDisableGpuRequested(args))
        {
            AddDefaultBrowserCommandLineSwitch(args, "disable-accelerated-2d-canvas", string.Empty);
            AddDefaultBrowserCommandLineSwitch(args, "disable-accelerated-video-decode", string.Empty);
            AddDefaultBrowserCommandLineSwitch(args, "disable-accelerated-video-encode", string.Empty);
            AddDefaultBrowserCommandLineSwitch(args, "disable-dev-shm-usage", string.Empty);
            AddDefaultBrowserCommandLineSwitch(args, "disable-features", "Vulkan");
            AddDefaultBrowserCommandLineSwitch(args, "disable-gpu", string.Empty);
            AddDefaultBrowserCommandLineSwitch(args, "disable-gpu-compositing", string.Empty);
            AddDefaultBrowserCommandLineSwitch(args, "disable-gpu-rasterization", string.Empty);
            AddDefaultBrowserCommandLineSwitch(args, "ignore-gpu-blocklist", string.Empty);
            AddDefaultBrowserCommandLineSwitch(args, "use-angle", "swiftshader");
            AddDefaultBrowserCommandLineSwitch(args, "use-gl", "swiftshader");
        }

        AddOptionalBrowserCommandLineSwitches(
            args,
            "disable-dev-shm-usage",
            "disable-features",
            "disable-software-rasterizer",
            "enable-features",
            "enable-logging",
            "log-file",
            "no-sandbox",
            "use-angle",
            "use-gl",
            "v");
    }

    private static void AddDefaultBrowserCommandLineSwitch(string[] args, string switchName, string value)
    {
        if (FindBrowserCommandLineSwitch(args, switchName) != null)
            return;

        WebView.Settings.AddCommandLineSwitch(switchName, value);
    }

    private static void AddOptionalBrowserCommandLineSwitches(string[] args, params string[] switchNames)
    {
        foreach (string switchName in switchNames)
            AddOptionalBrowserCommandLineSwitch(args, switchName);
    }

    private static void AddOptionalBrowserCommandLineSwitch(string[] args, string switchName)
    {
        string? argument = FindBrowserCommandLineSwitch(args, switchName);
        if (argument == null)
            return;

        string value = string.Empty;
        int equalsIndex = argument.IndexOf('=');
        if (equalsIndex >= 0)
            value = argument[(equalsIndex + 1)..];

        WebView.Settings.AddCommandLineSwitch(switchName, value);
    }

    private static string? FindBrowserCommandLineSwitch(string[] args, string switchName)
    {
        string prefix = "--" + switchName;
        return args.FirstOrDefault(arg =>
            string.Equals(arg, prefix, StringComparison.OrdinalIgnoreCase)
            || arg.StartsWith(prefix + "=", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDisableGpuRequested(string[] args)
        => args.Any(arg => string.Equals(arg, DisableGpuArgument, StringComparison.OrdinalIgnoreCase))
           || IsTruthy(Environment.GetEnvironmentVariable(DisableGpuVariable));

    private static bool IsEnableGpuRequested(string[] args)
        => args.Any(arg => string.Equals(arg, EnableGpuArgument, StringComparison.OrdinalIgnoreCase));

    private static bool IsLauncherChild(string[] args)
        => args.Any(arg => string.Equals(arg, LauncherChildArgument, StringComparison.OrdinalIgnoreCase))
           || IsTruthy(Environment.GetEnvironmentVariable(LauncherModeVariable));

    private static bool IsPublicArgument(string arg)
        => !string.Equals(arg, LauncherChildArgument, StringComparison.OrdinalIgnoreCase);

    private static string[] AddArgument(string[] args, string argument)
        => args.Any(arg => string.Equals(arg, argument, StringComparison.OrdinalIgnoreCase))
            ? args
            : args.Concat(new[] { argument }).ToArray();

    private static void ShowEarlyStartupFailure(ChildRunResult result, bool disableGpuWasUsed)
    {
        string mode = disableGpuWasUsed ? DisableGpuArgument : "normal GPU mode";
        string message =
            "GuacamoleClient failed during early Linux browser startup."
            + Environment.NewLine + Environment.NewLine
            + $"Startup mode: {mode}"
            + Environment.NewLine
            + $"Startup arguments: {FormatStartupArguments(result.Arguments)}"
            + Environment.NewLine
            + $"Exit code: {result.ExitCode}"
            + Environment.NewLine + Environment.NewLine
            + "The embedded Chromium/CEF browser process could not be started. "
            + "This often happens in virtual machines or remote desktop sessions when GPU/OpenGL support is not usable.";

        StartupErrorDialog.Show("GuacamoleClient startup failed", message);
    }

    private static string FormatStartupArguments(string[] args)
        => args.Length == 0
            ? "(none)"
            : string.Join(" ", args.Select(QuoteArgumentIfNeeded));

    private static string QuoteArgumentIfNeeded(string arg)
        => arg.Any(char.IsWhiteSpace)
            ? "\"" + arg.Replace("\"", "\\\"", StringComparison.Ordinal) + "\""
            : arg;

    private static BrowserCompatibilityState LoadBrowserCompatibilityState()
    {
        try
        {
            string path = GetBrowserCompatibilityStatePath();
            if (!File.Exists(path))
                return new BrowserCompatibilityState();

            return JsonSerializer.Deserialize<BrowserCompatibilityState>(
                File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new BrowserCompatibilityState();
        }
        catch
        {
            return new BrowserCompatibilityState();
        }
    }

    private static void SaveBrowserCompatibilityState(bool preferDisableGpu, string reason)
    {
        try
        {
            string path = GetBrowserCompatibilityStatePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var state = new BrowserCompatibilityState
            {
                PreferDisableGpu = preferDisableGpu,
                Reason = reason,
                UpdatedUtc = DateTimeOffset.UtcNow
            };
            File.WriteAllText(path, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Best effort only.
        }
    }

    private static string GetBrowserCompatibilityStatePath()
        => Path.Combine(
            GuacamoleSettingsPaths.GetDefaultSettingsDirectory("GuacamoleClient-Avalonia"),
            "browser-compatibility.json");

    private static bool IsTruthy(string? value)
        => string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

    private static void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                StartupErrorDialog.Show(LocalizationProvider.Get(LocalizationKeys.AppStart_UnexpectedError_Title), BuildErrorMessage(ex));
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            StartupErrorDialog.Show(LocalizationProvider.Get(LocalizationKeys.AppStart_BackgroundError_Title), BuildErrorMessage(e.Exception));
            e.SetObserved();
        };
    }

    private static string BuildErrorMessage(Exception ex)
    {
        return
            LocalizationProvider.Get(LocalizationKeys.AppStart_ErrorMessage_Text)
            + "\r\n\r\n"
            + ex.Message
            + "\r\n\r\n"
            + LocalizationProvider.Get(LocalizationKeys.AppStart_ErrorDetails_Label)
            + "\r\n"
            + ex;
    }

    private sealed record ChildRunResult(int ExitCode, bool FailedEarly, string[] Arguments);

    private sealed class BrowserCompatibilityState
    {
        public bool PreferDisableGpu { get; init; }
        public string? Reason { get; init; }
        public DateTimeOffset? UpdatedUtc { get; init; }
    }
}

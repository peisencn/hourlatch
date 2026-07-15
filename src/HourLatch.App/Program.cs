using HourLatch.Core.Configuration;
using HourLatch.Core.Overrides;
using HourLatch.Core.Runtime;
using HourLatch.Core.Security;
using HourLatch.Diagnostics;
using HourLatch.Power;
using HourLatch.Startup;
using HourLatch.SystemEvents;
using HourLatch.Tray;
using HourLatch.UI;

namespace HourLatch;

internal static class Program
{
    private static LocalLog? _log;
    private static int _errorDisplayed;

    [STAThread]
    private static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            _log = LocalLog.CreateDefault();
            RegisterExceptionHandlers();
            Application.Run(CreateApplicationContext());
        }
        catch (Exception exception)
        {
            HandleUnhandledException("main", exception);
        }
    }

    private static TrayApplicationContext CreateApplicationContext()
    {
        var validator = new SettingsValidator();
        var settingsPath = GetSettingsPath();
        var settingsStore = new JsonSettingsStore(settingsPath, validator);
        var firstLaunch = !File.Exists(settingsPath);
        if (firstLaunch)
        {
            settingsStore.Save(AppSettings.CreateDefaults());
        }

        var loadResult = settingsStore.Load();
        return ComposeApplication(
            settingsStore,
            validator,
            firstLaunch || !loadResult.IsValid);
    }

    private static TrayApplicationContext ComposeApplication(
        ISettingsStore settingsStore,
        SettingsValidator validator,
        bool showSettingsOnStart)
    {
        var passwordHasher = new PasswordHasher();
        var overrideManager = new OverrideManager();
        var prompt = new RestrictionPromptForm(passwordHasher);
        var systemEvents = new SystemEventMonitor();
        var power = new LoggingPowerActionExecutor(new WindowsPowerActionExecutor(), _log!);
        var controller = new RestrictionController(new RestrictionControllerDependencies
        {
            Clock = new SystemClock(),
            SettingsStore = settingsStore,
            Validator = validator,
            Prompt = prompt,
            PowerExecutor = power,
            OverrideManager = overrideManager
        }, TimeZoneInfo.Local);
        var mainForm = CreateMainForm(settingsStore, validator, passwordHasher);

        return new TrayApplicationContext(new TrayApplicationDependencies
        {
            Controller = controller,
            MainForm = mainForm,
            PromptForm = prompt,
            SystemEvents = systemEvents,
            SettingsStore = settingsStore,
            OverrideManager = overrideManager,
            PasswordHasher = passwordHasher,
            Log = _log!,
            ShowSettingsOnStart = showSettingsOnStart
        });
    }

    private static MainForm CreateMainForm(
        ISettingsStore store,
        SettingsValidator validator,
        PasswordHasher passwordHasher)
    {
        var startup = new StartupRegistration(Environment.ProcessPath ?? Application.ExecutablePath);
        return new MainForm(new MainFormDependencies
        {
            SettingsStore = store,
            Validator = validator,
            PasswordHasher = passwordHasher,
            StartupRegistration = startup
        });
    }

    private static string GetSettingsPath()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppIdentity.DataDirectoryName);
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "settings.json");
    }

    private static void RegisterExceptionHandlers()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) =>
            HandleUnhandledException("ui_thread", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                HandleUnhandledException("app_domain", exception);
            }
        };
    }

    private static void HandleUnhandledException(string eventType, Exception exception)
    {
        _log?.Exception(eventType, exception);
        if (Interlocked.Exchange(ref _errorDisplayed, 1) != 0)
        {
            return;
        }

        MessageBox.Show(
            "程序遇到错误。请重新打开设置检查配置；详细信息已写入本地日志。",
            AppIdentity.DisplayName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}

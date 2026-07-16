# HourLatch

[简体中文](README.md)

HourLatch is a lightweight self-discipline tool for Windows. It runs in the system tray and, during one daily restriction window, warns the user before locking the workstation or putting the computer to sleep. A separate application password can grant temporary access for a selected duration.

## Requirements

- Windows 10 or Windows 11 (x64)
- To run a release build: .NET 8 Desktop Runtime
- To build from source: .NET 8 SDK

The current application interface is available in Simplified Chinese only.

HourLatch is intended as a reminder and self-imposed constraint. It is not parental-control or administrator-resistant software. A local administrator can terminate the process, remove the startup entry, or edit the configuration. The HourLatch password does not replace the Windows sign-in password.

## Getting Started

1. Start `HourLatch.exe`. The settings window opens automatically on first launch.
2. Set the daily start and end times. Cross-midnight windows are supported, for example `23:00-07:00`.
3. Select either `Lock` or `Sleep`, then configure the warning countdown.
4. Set an application password. Restrictions cannot be enabled without one.
5. Select the default temporary override duration and optionally enable automatic startup after sign-in.
6. Enable the daily restriction and save the settings.

HourLatch continues running in the system tray after the settings window is closed. Right-click the tray icon to open settings, request or end a temporary override, pause the current restriction window, or exit. Pausing the current window and exiting require the application password.

## Temporary Overrides

The restriction prompt provides the following options:

- 15 minutes
- 30 minutes
- 60 minutes
- A custom duration from 1 to 1440 minutes
- Until the current restriction window ends, when enabled in settings

Fixed and custom durations are capped at the end of the current restriction window. Active overrides are persisted, so restarting HourLatch restores an override that is still valid for the same window.

When an override expires, HourLatch shows the warning countdown again before locking or sleeping. The override expiration time is therefore not the exact action time. The password can be entered again to grant another override during the same restriction window.

## Automatic Startup

When automatic startup is enabled, HourLatch creates an entry for the current user at:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

The registry value is named `HourLatch`. Disabling the option and saving the settings removes the entry.

## Data Locations

```text
Settings:     %AppData%\HourLatch\settings.json
Log:          %LocalAppData%\HourLatch\logs\app.log
Previous log: %LocalAppData%\HourLatch\logs\app.previous.log
```

Settings are saved using a temporary-file replacement. Passwords are stored only as salted PBKDF2-SHA256 hashes. Logs do not contain passwords, hashes, salts, or user input.

## Build and Test

```powershell
dotnet build HourLatch.sln -c Debug
dotnet test HourLatch.sln -c Debug
```

## Publish

The Release configuration produces a framework-dependent, single-file `win-x64` executable without trimming:

```powershell
dotnet publish src/HourLatch.App/HourLatch.App.csproj -c Release -r win-x64 --self-contained false
```

Output directory:

```text
src\HourLatch.App\bin\Release\net8.0-windows\win-x64\publish
```

## Third-Party Code

The Windows lock and sleep implementation was adapted with reference to the MIT-licensed ShutdownTimerClassic project. See `THIRD_PARTY_NOTICES.md` and `licenses/ShutdownTimerClassic-MIT.txt` for attribution.

## License

HourLatch is licensed under the MIT License. See `LICENSE`.

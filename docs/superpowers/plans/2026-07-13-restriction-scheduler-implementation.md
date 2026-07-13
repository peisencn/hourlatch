# 定时限制与临时放行 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个 Windows 托盘应用，在单个每日时间段内反复执行锁屏或休眠，并允许通过独立密码按用户选择的时长临时放行。

**Architecture:** 新建 `.NET 8` WinForms 应用和独立 Core 类库。Core 提供时间窗口、密码哈希、配置持久化、临时放行和策略控制；App 提供 Windows 电源动作、系统事件、自动启动、托盘和窗口。移植 `ShutdownTimerClassic` 的 MIT 许可电源操作思路，但不继承其静态单次倒计时架构。

**Tech Stack:** C# 12、.NET 8、WinForms、System.Text.Json、PBKDF2-SHA256、xUnit、Windows `SystemEvents`、Win32 `LockWorkStation`。

---

## 执行前提

- 上游参考仓库：`https://github.com/lukaslangrock/ShutdownTimerClassic`
- 已审查上游提交：`37c955ed448e48ea1ce1ea087b084b2badcf7ee2`
- 上游许可证：MIT，Copyright (c) 2026 Lukas Langrock
- 当前机器只有 .NET Runtime，没有 .NET SDK。执行 Task 1 前需要用户批准安装 `.NET 8 SDK`。
- 当前仓库只有已批准的设计文档。实现开始前，按用户选择使用 `superpowers:executing-plans`；任务共享核心文件且存在顺序依赖，不适合并行写入。
- 创建分支、安装 SDK、添加依赖、删除模板文件、真实锁屏或休眠验证均在执行到相应步骤时单独确认。

## 文件结构

```text
WiseAutoShutdown.sln
THIRD_PARTY_NOTICES.md
licenses/
  ShutdownTimerClassic-MIT.txt
src/
  WiseAutoShutdown.Core/
    WiseAutoShutdown.Core.csproj
    Configuration/
      AppSettings.cs
      ISettingsStore.cs
      SettingsLoadResult.cs
      SettingsValidator.cs
      JsonSettingsStore.cs
    Power/
      IPowerActionExecutor.cs
      PowerActionResult.cs
      RestrictionAction.cs
    Runtime/
      IClock.cs
      SystemClock.cs
      IRestrictionPrompt.cs
      PromptModels.cs
      RestrictionController.cs
      RestrictionState.cs
      RestrictionTrigger.cs
    Scheduling/
      DailyRestrictionSchedule.cs
      RestrictionWindow.cs
    Security/
      PasswordHashRecord.cs
      PasswordHasher.cs
    Overrides/
      OverrideGrant.cs
      OverrideManager.cs
      OverrideRequest.cs
  WiseAutoShutdown.App/
    WiseAutoShutdown.App.csproj
    Program.cs
    Diagnostics/
      LocalLog.cs
    Power/
      WindowsPowerActionExecutor.cs
    Startup/
      StartupRegistration.cs
    SystemEvents/
      SystemEventMonitor.cs
    Tray/
      TrayApplicationContext.cs
    UI/
      MainForm.cs
      MainForm.Designer.cs
      RestrictionPromptForm.cs
      RestrictionPromptForm.Designer.cs
      PasswordVerificationDialog.cs
      PasswordVerificationDialog.Designer.cs
tests/
  WiseAutoShutdown.Core.Tests/
    WiseAutoShutdown.Core.Tests.csproj
    Configuration/
      JsonSettingsStoreTests.cs
      SettingsValidatorTests.cs
    Overrides/
      OverrideManagerTests.cs
    Runtime/
      RestrictionControllerTests.cs
    Scheduling/
      DailyRestrictionScheduleTests.cs
    Security/
      PasswordHasherTests.cs
README.md
```

文件必须保持单一职责。生产代码函数不超过 50 行，文件不超过 300 行；如果 `RestrictionController` 或 WinForms code-behind 接近上限，在当前任务内继续拆分，不推迟到后续重构。

### Task 1: 建立 .NET 8 解决方案与上游归属

**Files:**
- Create: `WiseAutoShutdown.sln`
- Create: `src/WiseAutoShutdown.Core/WiseAutoShutdown.Core.csproj`
- Create: `src/WiseAutoShutdown.App/WiseAutoShutdown.App.csproj`
- Create: `src/WiseAutoShutdown.App/Program.cs`
- Create: `tests/WiseAutoShutdown.Core.Tests/WiseAutoShutdown.Core.Tests.csproj`
- Create: `licenses/ShutdownTimerClassic-MIT.txt`
- Create: `THIRD_PARTY_NOTICES.md`

- [ ] **Step 1: 检查并安装 SDK**

Run:

```powershell
dotnet --list-sdks
```

Expected before install: no output. After explicit approval, run:

```powershell
winget install --id Microsoft.DotNet.SDK.8 --exact --accept-package-agreements --accept-source-agreements
dotnet --list-sdks
```

Expected: at least one `8.0.x` SDK path.

- [ ] **Step 2: 创建解决方案和目录**

Run:

```powershell
dotnet new sln --name WiseAutoShutdown
New-Item -ItemType Directory -Force src/WiseAutoShutdown.Core, src/WiseAutoShutdown.App, tests/WiseAutoShutdown.Core.Tests, licenses
```

Expected: `WiseAutoShutdown.sln` exists.

- [ ] **Step 3: 创建项目文件**

Create `src/WiseAutoShutdown.Core/WiseAutoShutdown.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>12</LangVersion>
  </PropertyGroup>
</Project>
```

Create `src/WiseAutoShutdown.App/WiseAutoShutdown.App.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>12</LangVersion>
    <RootNamespace>WiseAutoShutdown</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\WiseAutoShutdown.Core\WiseAutoShutdown.Core.csproj" />
    <PackageReference Include="Microsoft.Win32.SystemEvents" Version="8.0.0" />
  </ItemGroup>
</Project>
```

Create `tests/WiseAutoShutdown.Core.Tests/WiseAutoShutdown.Core.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <ProjectReference Include="..\..\src\WiseAutoShutdown.Core\WiseAutoShutdown.Core.csproj" />
  </ItemGroup>
</Project>
```

Create a temporary buildable `src/WiseAutoShutdown.App/Program.cs`; Task 10 replaces its empty application context with the real tray runtime:

```csharp
namespace WiseAutoShutdown;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
    }
}
```

- [ ] **Step 4: 添加项目到解决方案并恢复依赖**

Run:

```powershell
dotnet sln WiseAutoShutdown.sln add src/WiseAutoShutdown.Core/WiseAutoShutdown.Core.csproj
dotnet sln WiseAutoShutdown.sln add src/WiseAutoShutdown.App/WiseAutoShutdown.App.csproj
dotnet sln WiseAutoShutdown.sln add tests/WiseAutoShutdown.Core.Tests/WiseAutoShutdown.Core.Tests.csproj
dotnet restore WiseAutoShutdown.sln
```

Expected: restore succeeds with 0 errors.

- [ ] **Step 5: 记录上游 MIT 归属**

Create `licenses/ShutdownTimerClassic-MIT.txt` with the exact upstream MIT license from commit `37c955e`. Create `THIRD_PARTY_NOTICES.md`:

```markdown
# Third-Party Notices

Parts of the Windows power-action implementation are derived from
[ShutdownTimerClassic](https://github.com/lukaslangrock/ShutdownTimerClassic)
at commit `37c955ed448e48ea1ce1ea087b084b2badcf7ee2`.

Copyright (c) 2026 Lukas Langrock. Licensed under the MIT License.
See `licenses/ShutdownTimerClassic-MIT.txt`.
```

- [ ] **Step 6: 验证空解决方案**

Run:

```powershell
dotnet build WiseAutoShutdown.sln -c Debug
dotnet test WiseAutoShutdown.sln -c Debug --no-build
```

Expected: build succeeds; test command reports no tests yet and exits 0.

- [ ] **Step 7: Commit**

```powershell
git add WiseAutoShutdown.sln src tests licenses THIRD_PARTY_NOTICES.md
git commit -m "chore: 初始化定时限制项目"
```

### Task 2: 实现每日限制时间窗口

**Files:**
- Create: `src/WiseAutoShutdown.Core/Scheduling/RestrictionWindow.cs`
- Create: `src/WiseAutoShutdown.Core/Scheduling/DailyRestrictionSchedule.cs`
- Create: `tests/WiseAutoShutdown.Core.Tests/Scheduling/DailyRestrictionScheduleTests.cs`

- [ ] **Step 1: 写失败测试**

Create tests covering normal, cross-midnight, boundaries, invalid equal times, and next window:

```csharp
public sealed class DailyRestrictionScheduleTests
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.Utc;

    [Theory]
    [InlineData("2026-07-13T23:30:00+00:00", true)]
    [InlineData("2026-07-14T06:59:59+00:00", true)]
    [InlineData("2026-07-14T07:00:00+00:00", false)]
    [InlineData("2026-07-13T22:59:59+00:00", false)]
    public void Cross_midnight_window_is_half_open(string instant, bool expected)
    {
        var schedule = new DailyRestrictionSchedule(new TimeOnly(23, 0), new TimeOnly(7, 0));
        var window = schedule.GetContainingWindow(DateTimeOffset.Parse(instant), Zone);
        Assert.Equal(expected, window is not null);
    }

    [Fact]
    public void Equal_start_and_end_is_invalid()
    {
        var schedule = new DailyRestrictionSchedule(new TimeOnly(8, 0), new TimeOnly(8, 0));
        Assert.False(schedule.IsValid);
    }
}
```

- [ ] **Step 2: 运行并确认失败**

Run:

```powershell
dotnet test tests/WiseAutoShutdown.Core.Tests/WiseAutoShutdown.Core.Tests.csproj --filter DailyRestrictionScheduleTests
```

Expected: FAIL because scheduling types do not exist.

- [ ] **Step 3: 实现窗口模型**

`RestrictionWindow` stores `Start`, `End`, and stable `Id`. `DailyRestrictionSchedule.GetContainingWindow` converts `now` to the supplied time zone, uses `[Start, End)` semantics, and handles cross-midnight by selecting either yesterday-to-today or today-to-tomorrow.

```csharp
public sealed record RestrictionWindow(DateTimeOffset Start, DateTimeOffset End)
{
    public string Id => $"{Start.UtcDateTime.Ticks}:{End.UtcDateTime.Ticks}";
    public bool Contains(DateTimeOffset instant) => instant >= Start && instant < End;
}
```

Keep `DailyRestrictionSchedule` below 100 lines by extracting `AtLocalTime(DateOnly, TimeOnly, TimeZoneInfo)` and `CreateWindow(...)` private helpers.

- [ ] **Step 4: 运行测试**

Run:

```powershell
dotnet test tests/WiseAutoShutdown.Core.Tests/WiseAutoShutdown.Core.Tests.csproj --filter DailyRestrictionScheduleTests
```

Expected: all scheduling tests PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/WiseAutoShutdown.Core/Scheduling tests/WiseAutoShutdown.Core.Tests/Scheduling
git commit -m "feat: 实现每日限制时间窗口"
```

### Task 3: 实现密码哈希与验证

**Files:**
- Create: `src/WiseAutoShutdown.Core/Security/PasswordHashRecord.cs`
- Create: `src/WiseAutoShutdown.Core/Security/PasswordHasher.cs`
- Create: `tests/WiseAutoShutdown.Core.Tests/Security/PasswordHasherTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
public sealed class PasswordHasherTests
{
    [Fact]
    public void Created_hash_verifies_only_the_original_password()
    {
        var hasher = new PasswordHasher(iterations: 210_000);
        var record = hasher.Hash("correct horse battery staple");

        Assert.True(hasher.Verify("correct horse battery staple", record));
        Assert.False(hasher.Verify("wrong", record));
        Assert.NotEqual("correct horse battery staple", record.HashBase64);
    }

    [Fact]
    public void Hashes_use_unique_salts()
    {
        var hasher = new PasswordHasher();
        Assert.NotEqual(hasher.Hash("same").SaltBase64, hasher.Hash("same").SaltBase64);
    }
}
```

- [ ] **Step 2: 运行并确认失败**

Run: `dotnet test --filter PasswordHasherTests`

Expected: FAIL because security types do not exist.

- [ ] **Step 3: 实现 PBKDF2-SHA256**

Use `RandomNumberGenerator.GetBytes(16)`, `Rfc2898DeriveBytes.Pbkdf2(..., SHA256, 32)`, and `CryptographicOperations.FixedTimeEquals`. Reject empty passwords in `Hash`; `Verify` returns false for malformed Base64 or unsupported algorithms.

```csharp
public sealed record PasswordHashRecord(
    string Algorithm,
    int Iterations,
    string SaltBase64,
    string HashBase64);
```

- [ ] **Step 4: 运行测试**

Run: `dotnet test --filter PasswordHasherTests`

Expected: all password tests PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/WiseAutoShutdown.Core/Security tests/WiseAutoShutdown.Core.Tests/Security
git commit -m "feat: 添加安全密码验证"
```

### Task 4: 实现配置模型、校验与原子持久化

**Files:**
- Create: `src/WiseAutoShutdown.Core/Power/RestrictionAction.cs`
- Create: `src/WiseAutoShutdown.Core/Configuration/AppSettings.cs`
- Create: `src/WiseAutoShutdown.Core/Configuration/ISettingsStore.cs`
- Create: `src/WiseAutoShutdown.Core/Configuration/SettingsLoadResult.cs`
- Create: `src/WiseAutoShutdown.Core/Configuration/SettingsValidator.cs`
- Create: `src/WiseAutoShutdown.Core/Configuration/JsonSettingsStore.cs`
- Create: `tests/WiseAutoShutdown.Core.Tests/Configuration/SettingsValidatorTests.cs`
- Create: `tests/WiseAutoShutdown.Core.Tests/Configuration/JsonSettingsStoreTests.cs`

- [ ] **Step 1: 写配置校验失败测试**

Cover equal start/end, enabled-without-password, invalid warning seconds, valid cross-midnight settings, and corrupted JSON disabling actions.

```csharp
[Fact]
public void Enabled_settings_require_a_password()
{
    var settings = AppSettings.CreateDefaults() with { Enabled = true, Password = null };
    var result = new SettingsValidator().Validate(settings);
    Assert.Contains(result.Errors, error => error.Code == "password_required");
}
```

- [ ] **Step 2: 运行并确认失败**

Run: `dotnet test --filter "SettingsValidatorTests|JsonSettingsStoreTests"`

Expected: FAIL because configuration types do not exist.

- [ ] **Step 3: 实现配置记录**

`AppSettings` must include `Enabled`, `StartTime`, `EndTime`, `Action`, `WarningSeconds`, `DefaultOverrideMinutes`, `AllowUntilWindowEnd`, `AutoStart`, and `Password`. Defaults are disabled, `23:00-07:00`, Lock, 60-second warning, 30-minute override. Task 5 adds `ActiveOverride` after the override type exists.

Define `ISettingsStore.Load()` and `ISettingsStore.Save(AppSettings settings)` so the controller can use an in-memory fake in tests and `JsonSettingsStore` in production.

- [ ] **Step 4: 实现安全加载与原子保存**

`JsonSettingsStore.Load` returns `SettingsLoadResult` with `IsValid`, `Settings`, and errors. Corrupted JSON returns disabled defaults and does not overwrite the broken file. `Save` writes `<settings>.tmp`, flushes, then uses `File.Move(temp, path, true)` on the same volume.

- [ ] **Step 5: 运行测试**

Run: `dotnet test --filter "SettingsValidatorTests|JsonSettingsStoreTests"`

Expected: all configuration tests PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/WiseAutoShutdown.Core/Configuration src/WiseAutoShutdown.Core/Power tests/WiseAutoShutdown.Core.Tests/Configuration
git commit -m "feat: 添加限制配置持久化"
```

### Task 5: 实现临时放行

**Files:**
- Create: `src/WiseAutoShutdown.Core/Overrides/OverrideGrant.cs`
- Create: `src/WiseAutoShutdown.Core/Overrides/OverrideRequest.cs`
- Create: `src/WiseAutoShutdown.Core/Overrides/OverrideManager.cs`
- Modify: `src/WiseAutoShutdown.Core/Configuration/AppSettings.cs`
- Modify: `tests/WiseAutoShutdown.Core.Tests/Configuration/JsonSettingsStoreTests.cs`
- Create: `tests/WiseAutoShutdown.Core.Tests/Overrides/OverrideManagerTests.cs`

- [ ] **Step 1: 写失败测试**

Cover 15/30/60-minute grants, custom duration, until-window-end, clipping at window end, wrong-window rejection, expiry, and repeated grants replacing the previous grant.

```csharp
[Fact]
public void Fixed_duration_is_clipped_to_window_end()
{
    var now = DateTimeOffset.Parse("2026-07-13T23:50:00+00:00");
    var window = new RestrictionWindow(
        DateTimeOffset.Parse("2026-07-13T23:00:00+00:00"),
        DateTimeOffset.Parse("2026-07-14T00:00:00+00:00"));

    var grant = new OverrideManager().Create(window, now, OverrideRequest.ForMinutes(30));
    Assert.Equal(window.End, grant.ExpiresAt);
}
```

- [ ] **Step 2: 运行并确认失败**

Run: `dotnet test --filter OverrideManagerTests`

Expected: FAIL because override types do not exist.

- [ ] **Step 3: 实现放行模型**

```csharp
public sealed record OverrideGrant(string WindowId, DateTimeOffset GrantedAt, DateTimeOffset ExpiresAt)
{
    public bool IsActive(RestrictionWindow window, DateTimeOffset now) =>
        WindowId == window.Id && now < ExpiresAt;
}
```

Reject non-positive and longer-than-24-hour custom durations. Always cap grants at `window.End`.

Add `OverrideGrant? ActiveOverride` to `AppSettings` and extend the JSON round-trip test to prove it persists.

- [ ] **Step 4: 运行测试并提交**

Run: `dotnet test --filter OverrideManagerTests`

Expected: PASS.

```powershell
git add src/WiseAutoShutdown.Core/Overrides src/WiseAutoShutdown.Core/Configuration/AppSettings.cs tests/WiseAutoShutdown.Core.Tests/Overrides tests/WiseAutoShutdown.Core.Tests/Configuration/JsonSettingsStoreTests.cs
git commit -m "feat: 实现临时放行规则"
```

### Task 6: 实现策略状态与控制器

**Files:**
- Create: `src/WiseAutoShutdown.Core/Runtime/IClock.cs`
- Create: `src/WiseAutoShutdown.Core/Runtime/SystemClock.cs`
- Create: `src/WiseAutoShutdown.Core/Runtime/RestrictionState.cs`
- Create: `src/WiseAutoShutdown.Core/Runtime/RestrictionTrigger.cs`
- Create: `src/WiseAutoShutdown.Core/Runtime/IRestrictionPrompt.cs`
- Create: `src/WiseAutoShutdown.Core/Runtime/PromptModels.cs`
- Create: `src/WiseAutoShutdown.Core/Power/IPowerActionExecutor.cs`
- Create: `src/WiseAutoShutdown.Core/Power/PowerActionResult.cs`
- Create: `src/WiseAutoShutdown.Core/Runtime/RestrictionController.cs`
- Create: `tests/WiseAutoShutdown.Core.Tests/Runtime/RestrictionControllerTests.cs`

- [ ] **Step 1: 写控制器失败测试**

Use fake clock, prompt, power executor, and in-memory settings store. Cover disabled, outside-window, active override, timeout executing action, password override, duplicate event suppression, locked-session resume deferral, action failure, and expired grant cleanup.

```csharp
[Fact]
public void Unlock_inside_window_executes_action_after_prompt_timeout()
{
    var harness = ControllerHarness.InsideWindow(PromptOutcome.TimedOut);
    harness.Controller.Evaluate(RestrictionTrigger.SessionUnlock, sessionLocked: false);

    Assert.Equal(RestrictionState.Restricted, harness.Controller.State);
    Assert.Single(harness.Power.Actions);
}
```

- [ ] **Step 2: 运行并确认失败**

Run: `dotnet test --filter RestrictionControllerTests`

Expected: FAIL because runtime types do not exist.

- [ ] **Step 3: 定义状态和边界接口**

States: `Disabled`, `OutsideWindow`, `Warning`, `Restricted`, `OverrideActive`, `ActionFailed`.

Triggers: `Startup`, `Periodic`, `WindowBoundary`, `SessionUnlock`, `Resume`, `SettingsChanged`, `SystemTimeChanged`, `OverrideExpired`.

`IRestrictionPrompt.Show` returns only approved override requests, timeout, or immediate action. Password verification remains inside the prompt implementation through an injected `PasswordHasher`; the controller receives no plaintext password.

- [ ] **Step 4: 实现控制器**

`Evaluate` performs this order:

1. validate settings;
2. find the current window;
3. clear stale grants;
4. honor active grant;
5. defer Resume while the session is locked;
6. suppress a second prompt while already in `Warning`;
7. show warning prompt;
8. persist an approved grant or execute the configured action;
9. set `ActionFailed` and schedule bounded retry after failure.

Use a private `bool evaluationInProgress` guard and keep each helper below 50 lines.

- [ ] **Step 5: 运行测试**

Run:

```powershell
dotnet test --filter RestrictionControllerTests
dotnet test tests/WiseAutoShutdown.Core.Tests/WiseAutoShutdown.Core.Tests.csproj
```

Expected: all controller and core tests PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/WiseAutoShutdown.Core/Runtime src/WiseAutoShutdown.Core/Power tests/WiseAutoShutdown.Core.Tests/Runtime
git commit -m "feat: 实现限制状态控制器"
```

### Task 7: 移植 Windows 锁屏与休眠执行器

**Files:**
- Create: `src/WiseAutoShutdown.App/Power/WindowsPowerActionExecutor.cs`
- Modify: `THIRD_PARTY_NOTICES.md`

- [ ] **Step 1: 定义不会误触发真实动作的执行边界**

Keep all real calls behind `IPowerActionExecutor.Execute(RestrictionAction action)`. Automated tests use a fake executor from Task 6; no automated test instantiates `WindowsPowerActionExecutor`.

- [ ] **Step 2: 移植最小 Windows 代码**

Implement `Lock` with `user32!LockWorkStation` and `Sleep` with `Application.SetSuspendState(PowerState.Suspend, false, false)`. Return `PowerActionResult.Success()` or `PowerActionResult.Failure(message, errorCode)`. Do not port shutdown, reboot, logout, custom command, privilege elevation, or force-close code.

Add a source comment:

```csharp
// LockWorkStation interop adapted from ShutdownTimerClassic (MIT), commit 37c955e.
```

- [ ] **Step 3: 构建验证**

Run:

```powershell
dotnet build src/WiseAutoShutdown.App/WiseAutoShutdown.App.csproj -c Debug
```

Expected: PASS with 0 warnings introduced by this file.

- [ ] **Step 4: Commit**

```powershell
git add src/WiseAutoShutdown.App/Power THIRD_PARTY_NOTICES.md
git commit -m "feat: 添加Windows限制动作"
```

### Task 8: 实现系统事件和登录自启动

**Files:**
- Create: `src/WiseAutoShutdown.App/SystemEvents/SystemEventMonitor.cs`
- Create: `src/WiseAutoShutdown.App/Startup/StartupRegistration.cs`

- [ ] **Step 1: 实现系统事件监视器**

Subscribe to `SystemEvents.SessionSwitch`, `PowerModeChanged`, and `TimeChanged`. Track `IsSessionLocked`. Raise a single `EvaluationRequested(RestrictionTrigger)` event. On Resume while locked, set `resumePending` and wait for SessionUnlock; do not execute sleep immediately behind the secure desktop. Unsubscribe all static handlers in `Dispose`.

- [ ] **Step 2: 实现当前用户自动启动**

Use `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, value name `WiseAutoShutdown`, and the quoted executable path. Provide `IsEnabled`, `Enable`, and `Disable`. Failures return a result instead of throwing into the UI event loop.

- [ ] **Step 3: 构建验证**

Run: `dotnet build WiseAutoShutdown.sln -c Debug`

Expected: PASS.

- [ ] **Step 4: Commit**

```powershell
git add src/WiseAutoShutdown.App/SystemEvents src/WiseAutoShutdown.App/Startup
git commit -m "feat: 监听系统事件和自启动"
```

### Task 9: 构建设置、密码和限制提示界面

**Files:**
- Create: `src/WiseAutoShutdown.App/UI/MainForm.cs`
- Create: `src/WiseAutoShutdown.App/UI/MainForm.Designer.cs`
- Create: `src/WiseAutoShutdown.App/UI/RestrictionPromptForm.cs`
- Create: `src/WiseAutoShutdown.App/UI/RestrictionPromptForm.Designer.cs`
- Create: `src/WiseAutoShutdown.App/UI/PasswordVerificationDialog.cs`
- Create: `src/WiseAutoShutdown.App/UI/PasswordVerificationDialog.Designer.cs`

- [ ] **Step 1: 创建设置窗口控件**

Use fixed WinForms controls with these names: `enabledCheckBox`, `startTimePicker`, `endTimePicker`, `actionComboBox`, `warningSecondsNumeric`, `defaultOverrideComboBox`, `allowUntilEndCheckBox`, `autoStartCheckBox`, `setPasswordButton`, `statusLabel`, `nextActionLabel`, `saveButton`.

`actionComboBox` contains only `锁屏` and `休眠`. `defaultOverrideComboBox` contains `15 分钟`, `30 分钟`, `60 分钟`. Prevent enabling when start equals end or no password exists. Saving validates before writing and immediately raises `SettingsChanged`.

- [ ] **Step 2: 创建密码验证窗口**

The dialog accepts a `PasswordHashRecord` and `PasswordHasher`. It uses a password textbox, disables submit for two seconds after three consecutive failures, clears the textbox after failure, and never exposes the configured hash in labels or logs.

- [ ] **Step 3: 创建限制提示窗口**

Controls: `windowLabel`, `actionLabel`, `countdownLabel`, `passwordTextBox`, `durationComboBox`, `customMinutesNumeric`, `allowButton`, `executeNowButton`.

Duration choices: `15 分钟`, `30 分钟`, `60 分钟`, `自定义`, and conditionally `直到本次限制结束`. A WinForms timer updates the warning countdown. Timeout returns `PromptOutcome.TimedOut`; correct password returns an `OverrideRequest`; execute-now returns `PromptOutcome.ExecuteNow`.

- [ ] **Step 4: 构建界面代码**

Run: `dotnet build src/WiseAutoShutdown.App/WiseAutoShutdown.App.csproj -c Debug`

Expected: PASS. The visual 100%/150% scaling check is performed after Task 10 wires the tray runtime.

- [ ] **Step 5: Commit**

```powershell
git add src/WiseAutoShutdown.App/UI
git commit -m "feat: 添加限制设置和放行界面"
```

### Task 10: 接入托盘运行时、日志和重试

**Files:**
- Create: `src/WiseAutoShutdown.App/Diagnostics/LocalLog.cs`
- Create: `src/WiseAutoShutdown.App/Tray/TrayApplicationContext.cs`
- Create: `src/WiseAutoShutdown.App/Program.cs`
- Modify: `src/WiseAutoShutdown.Core/Runtime/RestrictionController.cs`
- Modify: `tests/WiseAutoShutdown.Core.Tests/Runtime/RestrictionControllerTests.cs`

- [ ] **Step 1: 增加失败重试测试**

Add tests proving retries wait 5, 10, then 20 seconds, stop after three failed attempts for the same evaluation cycle, and reset after a successful action or a new window.

- [ ] **Step 2: 运行并确认失败**

Run: `dotnet test --filter RestrictionControllerTests`

Expected: new retry tests FAIL.

- [ ] **Step 3: 实现有界重试**

Store `retryAttempt` and `nextRetryAt`. Periodic evaluation executes only when `clock.Now >= nextRetryAt`. Do not busy-loop. Keep `ActionFailed` visible through the tray status.

- [ ] **Step 4: 实现本地日志**

Write logs to `%LocalAppData%\WiseAutoShutdown\logs\app.log`. Rotate to `app.previous.log` when the current log exceeds 1 MiB. Log event type, state transition, action result, and exception type; never log passwords, hashes, salts, or entered text.

- [ ] **Step 5: 实现托盘 ApplicationContext**

Own one `NotifyIcon`, `MainForm`, `SystemEventMonitor`, `RestrictionController`, and a 30-second WinForms timer. Tray commands: open settings, request temporary override, end override, pause current window, protected exit. Only one prompt window can exist.

- [ ] **Step 6: 实现程序入口**

`Program.Main` enables visual styles, loads `%AppData%\WiseAutoShutdown\settings.json`, creates disabled defaults on first launch, shows settings when configuration is invalid, starts `TrayApplicationContext`, and registers global exception handlers that log then display one actionable error.

- [ ] **Step 7: 运行全部自动化验证**

Run:

```powershell
dotnet test WiseAutoShutdown.sln -c Debug
dotnet build WiseAutoShutdown.sln -c Release
```

Expected: all tests PASS; build has 0 errors.

- [ ] **Step 8: Commit**

```powershell
git add src/WiseAutoShutdown.App src/WiseAutoShutdown.Core/Runtime tests/WiseAutoShutdown.Core.Tests/Runtime
git commit -m "feat: 接入托盘限制运行时"
```

### Task 11: 完成使用文档和发布构建

**Files:**
- Create: `README.md`
- Modify: `src/WiseAutoShutdown.App/WiseAutoShutdown.App.csproj`

- [ ] **Step 1: 编写 README**

Document prerequisites, first-run password setup, single daily window, lock/sleep actions, override durations, “暂停本次限制”, auto-start behavior, administrator bypass limitation, settings/log paths, build, test, and publish commands.

- [ ] **Step 2: 配置发布属性**

Add `RuntimeIdentifier=win-x64`, `PublishSingleFile=true`, `SelfContained=false`, and `DebugType=embedded` to the Release property group. Do not enable trimming for WinForms.

- [ ] **Step 3: 发布验证**

Run:

```powershell
dotnet publish src/WiseAutoShutdown.App/WiseAutoShutdown.App.csproj -c Release -r win-x64 --self-contained false
Get-ChildItem src/WiseAutoShutdown.App/bin/Release/net8.0-windows/win-x64/publish
```

Expected: `WiseAutoShutdown.App.exe` exists and launches on the current machine with the installed .NET 8 Desktop Runtime.

- [ ] **Step 4: Commit**

```powershell
git add README.md src/WiseAutoShutdown.App/WiseAutoShutdown.App.csproj
git commit -m "docs: 添加使用和发布说明"
```

### Task 12: Windows 真实冒烟验证与收尾

**Files:**
- Modify only if verification exposes a defect.

- [ ] **Step 1: 运行静态和自动化验证**

Run:

```powershell
dotnet test WiseAutoShutdown.sln -c Release
dotnet build WiseAutoShutdown.sln -c Release
git diff --check
```

Expected: tests PASS, build PASS, no whitespace errors.

- [ ] **Step 2: 无系统动作的策略冒烟**

Configure a window containing the current time, set warning to 10 seconds, use a fake/debug power executor, and verify: startup warning, password grant, 15-minute/custom/until-end choices, repeated grant, grant expiry, application restart restoring a grant, and time change reevaluation.

Open the settings and restriction forms at Windows 100% and 150% display scaling. Verify all labels, time pickers, buttons, password fields, and duration choices fit without overlap or layout shift.

- [ ] **Step 3: 经用户确认后执行真实锁屏冒烟**

Verify the lock action, unlock event, password prompt, temporary grant, grant expiry, and second relock. Do not trigger the real lock action until the user explicitly confirms they are ready.

- [ ] **Step 4: 经用户确认后执行真实休眠冒烟**

Verify sleep, resume, no immediate sleep loop behind the secure desktop, unlock prompt, temporary grant, and later re-sleep. Do not trigger real sleep until the user explicitly confirms they are ready.

- [ ] **Step 5: 检查日志和工作区**

Run:

```powershell
Get-Content "$env:LOCALAPPDATA\WiseAutoShutdown\logs\app.log" -Tail 100
git status --short
git log --oneline --decorate -12
```

Expected: no password material in logs; only intended source/doc changes; commit sequence matches the tasks.

- [ ] **Step 6: 最终修复提交（仅在验证发现问题时）**

Use a scoped commit such as:

```powershell
git add <verified-fix-files>
git commit -m "fix: 修复限制流程冒烟问题"
```

## 计划自检清单

- 设计中的单时间段、跨午夜、锁屏/休眠、启动/解锁/恢复事件均有对应任务。
- 固定、自定义、直到窗口结束和重复放行均有测试与实现任务。
- 密码使用 PBKDF2-SHA256、加盐和固定时间比较，不保存明文。
- 配置损坏默认禁用动作，原子保存和日志脱敏均有任务。
- MVP 明确不包含 Windows Service、多时间段、星期/节假日和管理员防绕过。
- 所有真实锁屏和休眠动作都要求执行时再次获得用户确认。
- Core 行为使用 TDD；Windows UI 和系统调用使用构建检查及真实冒烟验证。

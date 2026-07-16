# HourLatch

[English](README_EN.md)

一个面向 Windows 的轻量自我约束工具。程序常驻系统托盘，在每日单个限制时段内提醒并执行锁屏或休眠；输入独立密码后，可以按指定时长临时放行。

## 环境要求

- Windows 10 或 Windows 11（x64）
- 运行发布版本：.NET 8 Desktop Runtime
- 从源码构建：.NET 8 SDK

本工具面向提醒和自我约束，不是家长控制或管理员防绕过方案。本机管理员可以结束进程、删除自启动项或修改配置。应用密码也不替代 Windows 登录密码。

## 首次使用

1. 启动 `HourLatch.exe`，首次运行会打开设置窗口。
2. 设置每日开始时间和结束时间。跨午夜时段可直接使用，例如 `23:00-07:00`。
3. 选择 `锁屏` 或 `休眠`，设置提醒倒计时。
4. 设置应用密码。未设置密码时不能启用限制。
5. 选择默认临时放行时长，并按需启用登录后自动启动。
6. 勾选“启用每日限制”并保存。

程序关闭设置窗口后仍在系统托盘运行。右键托盘图标可以打开设置、请求临时放行、结束放行、暂停本次限制或退出。退出和“暂停本次限制”需要验证应用密码。

## 临时放行

限制提醒窗口支持以下选项：

- 15 分钟
- 30 分钟
- 60 分钟
- 1-1440 分钟自定义时长
- 直到本次限制时段结束（可在设置中关闭）

固定时长和自定义时长都会被截断到当前限制窗口结束。放行状态会写入配置，程序重启后仍会恢复尚未过期且属于当前窗口的放行。放行到期后会重新进入提醒流程，提醒倒计时结束后才执行锁屏或休眠；因此放行结束时间不是直接锁屏时间。同一限制时段内可以再次输入密码放行。

## 自动启动

启用自动启动后，程序在当前用户的以下注册表位置写入启动项：

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

值名为 `HourLatch`。关闭该选项并保存会删除对应启动项。

## 文件位置

```text
配置：%AppData%\HourLatch\settings.json
日志：%LocalAppData%\HourLatch\logs\app.log
旧日志：%LocalAppData%\HourLatch\logs\app.previous.log
```

配置使用临时文件替换方式保存。密码仅保存 PBKDF2-SHA256 加盐哈希，不保存明文。日志不记录密码、哈希、盐或输入内容。

## 构建与测试

```powershell
dotnet build HourLatch.sln -c Debug
dotnet test HourLatch.sln -c Debug
```

## 发布

Release 配置生成 `win-x64`、框架依赖的单文件程序，不启用 trimming：

```powershell
dotnet publish src/HourLatch.App/HourLatch.App.csproj -c Release -r win-x64 --self-contained false
```

发布目录：

```text
src\HourLatch.App\bin\Release\net8.0-windows\win-x64\publish
```

## 第三方代码

Windows 锁屏和休眠实现参考了 MIT 许可的 ShutdownTimerClassic。完整归属见 `THIRD_PARTY_NOTICES.md` 和 `licenses/ShutdownTimerClassic-MIT.txt`。

## 许可证

HourLatch 自身代码采用 MIT License，详见 `LICENSE`。

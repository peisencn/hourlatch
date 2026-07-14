# Third-Party Notices

The Windows lock and suspend action implementation in
`src/WiseAutoShutdown.App/Power/WindowsPowerActionExecutor.cs` is adapted from
[ShutdownTimerClassic](https://github.com/lukaslangrock/ShutdownTimerClassic)
at commit `37c955ed448e48ea1ce1ea087b084b2badcf7ee2`.

Only the minimal lock and suspend approach was retained. Shutdown, reboot,
logout, forced process termination, custom commands, and privilege-elevation
code were not incorporated.

Copyright (c) 2026 Lukas Langrock. Licensed under the MIT License.
See `licenses/ShutdownTimerClassic-MIT.txt`.

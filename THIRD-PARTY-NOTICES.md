# Third-party notices

Rooznama itself is released under the MIT License (see `LICENSE`). It uses or ships the following third-party components.

## Vazirmatn font (optional, bundled in `Assets/Fonts`)

- Project: https://github.com/rastikerdar/vazirmatn
- License: SIL Open Font License 1.1 — https://openfontlicense.org


## Libraries

| Component | Use | License |
|---|---|---|
| .NET 8 | runtime | MIT |
| Windows App SDK (WinUI 3) | UI framework | see the license of the `Microsoft.WindowsAppSDK` NuGet package |
| System.Drawing.Common | rendering the tray icon | MIT |

## Windows APIs

The tray icon uses `Shell_NotifyIcon`. Dark mode for the native tray menu uses undocumented `uxtheme.dll` ordinals (135, 136) that other Windows applications also rely on; they may change in future Windows versions.

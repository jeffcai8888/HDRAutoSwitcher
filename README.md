# HDRAutoSwitcher

[中文](#中文) | [English](#english)

---

## 中文

带托盘图形界面的 Windows 小工具：监控指定程序，自动切换主显示器的 HDR 状态。

- 任一被监控程序启动后，自动打开主显示器的 HDR（如果原本已打开则保持不变）
- 被监控程序全部退出后，恢复之前的 HDR 状态
- 工具退出时也会恢复

### 界面与托盘

- 双击 `HDRAutoSwitcher.exe` 启动，主窗口显示 HDR 状态、监控的进程列表和运行日志
- 在主窗口中可直接修改配置：点"添加 exe..."通过文件对话框选择要监控的程序（可多选），"删除选中"移除，"保存配置"写回 `HDRAutoSwitcher.ini` 并立即生效，无需重启
- 点关闭按钮或最小化都会隐藏到右下角托盘（通知区域），双击托盘图标重新打开窗口
- 托盘图标颜色表示状态：灰色 = HDR 关，橙色 = HDR 开；HDR 切换时弹出气泡通知
- 托盘右键菜单：显示窗口 / 退出（退出时恢复 HDR 状态）

### 环境要求

- Windows 10 1709 及以上（含 Windows 11；24H2 及以上自动使用新的 HDR 专用 API）
- 显示器支持 HDR
- 无需安装 .NET：使用系统自带的 .NET Framework 4.x 运行/编译

### 配置

编辑 exe 同目录下的 `HDRAutoSwitcher.ini`，每行写一个要监控的进程名（`.exe` 后缀可写可不写），`#` 或 `;` 开头的行为注释：

```ini
game.exe
Video Player.exe
```

### 命令行模式

带参数运行则进入控制台模式（输出显示在调用的终端中）：

```
HDRAutoSwitcher.exe game.exe "Video Player.exe"   # 监控指定进程（优先于配置文件）
HDRAutoSwitcher.exe --status                      # 查看当前 HDR 状态
HDRAutoSwitcher.exe --set on|off                  # 手动打开/关闭 HDR
HDRAutoSwitcher.exe --probe                       # 开关一次 HDR 并回读验证
```

### 编译

运行 `build.bat` 即可，使用 Windows 自带的 `csc.exe` 编译，无需安装任何工具链或依赖。

### 实现原理

- 通过 `EnumDisplayDevices` 找到主显示器，再用 `QueryDisplayConfig` 匹配对应的显示路径与目标 ID
- Windows 11 24H2（build 26100）之前：`DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO` / `SET_ADVANCED_COLOR_STATE`
- 24H2 起 HDR 与广色域（WCG）API 分离，自动改用 `GET_ADVANCED_COLOR_INFO_2` / `SET_HDR_STATE`（按 `RtlGetVersion` 的真实版本号选择）
- WinForms 界面 + `NotifyIcon` 托盘；每秒轮询进程列表检测目标程序的启动与退出

---

## English

A Windows tray utility that watches specific processes and automatically toggles HDR on the primary display.

- When any watched process starts, HDR on the primary display is enabled (left untouched if already on)
- When all watched processes exit, the previous HDR state is restored
- The state is also restored when the tool itself exits

### UI & System Tray

- Double-click `HDRAutoSwitcher.exe` to start; the main window shows the HDR state, the watched process list, and a running log
- Configuration can be edited right in the main window: "添加 exe..." opens a file picker to select programs to watch (multi-select supported), "删除选中" removes the selected entry, and "保存配置" writes everything back to `HDRAutoSwitcher.ini` and applies it immediately — no restart needed
- Closing or minimizing the window hides it to the system tray (notification area); double-click the tray icon to reopen
- Tray icon color indicates state: gray = HDR off, orange = HDR on; a balloon notification appears on every toggle
- Tray context menu: Show window / Exit (HDR state is restored on exit)

### Requirements

- Windows 10 1709 or later (including Windows 11; on 24H2+ the new HDR-specific API is used automatically)
- An HDR-capable primary display
- No .NET installation required: runs and builds with the .NET Framework 4.x bundled with Windows

### Configuration

Edit `HDRAutoSwitcher.ini` next to the exe — one process name per line (the `.exe` suffix is optional); lines starting with `#` or `;` are comments:

```ini
game.exe
Video Player.exe
```

### Command-Line Mode

Run with arguments to use console mode (output is attached to the calling terminal):

```
HDRAutoSwitcher.exe game.exe "Video Player.exe"   # Watch these processes (overrides the config file)
HDRAutoSwitcher.exe --status                      # Show current HDR state
HDRAutoSwitcher.exe --set on|off                  # Manually toggle HDR
HDRAutoSwitcher.exe --probe                       # Toggle HDR on/off once and read back
```

### Build

Run `build.bat` — it compiles with the `csc.exe` bundled with Windows; no toolchain or dependencies to install.

### How It Works

- Finds the primary display via `EnumDisplayDevices`, then matches it to a display path and target ID with `QueryDisplayConfig`
- Before Windows 11 24H2 (build 26100): `DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO` / `SET_ADVANCED_COLOR_STATE`
- Starting with 24H2, the HDR and wide-color-gamut (WCG) APIs are split, so `GET_ADVANCED_COLOR_INFO_2` / `SET_HDR_STATE` are used instead (selected by the real build number from `RtlGetVersion`)
- WinForms UI + `NotifyIcon` tray; the process list is polled once per second to detect watched processes starting and exiting

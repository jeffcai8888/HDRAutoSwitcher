# HDRAutoSwitcher

带托盘图形界面的 Windows 小工具：监控指定程序，自动切换主显示器的 HDR 状态。

- 任一被监控程序启动后，自动打开主显示器的 HDR（如果原本已打开则保持不变）
- 被监控程序全部退出后，恢复之前的 HDR 状态
- 工具退出时也会恢复

## 界面与托盘

- 双击 `HDRAutoSwitcher.exe` 启动，主窗口显示 HDR 状态、监控的进程列表和运行日志
- 点关闭按钮或最小化都会隐藏到右下角托盘（通知区域），双击托盘图标重新打开窗口
- 托盘图标颜色表示状态：灰色 = HDR 关，橙色 = HDR 开；HDR 切换时弹出气泡通知
- 托盘右键菜单：显示窗口 / 退出（退出时恢复 HDR 状态）
- 修改 `HDRAutoSwitcher.ini` 后在主窗口点"重新加载配置"即可生效，无需重启

## 环境要求

- Windows 10 1709 及以上（含 Windows 11；24H2 及以上自动使用新的 HDR 专用 API）
- 显示器支持 HDR
- 无需安装 .NET：使用系统自带的 .NET Framework 4.x 运行/编译

## 配置

编辑 exe 同目录下的 `HDRAutoSwitcher.ini`，每行写一个要监控的进程名（`.exe` 后缀可写可不写），`#` 或 `;` 开头的行为注释：

```ini
game.exe
Video Player.exe
```

## 命令行模式

带参数运行则进入控制台模式（输出显示在调用的终端中）：

```
HDRAutoSwitcher.exe game.exe "Video Player.exe"   # 监控指定进程（优先于配置文件）
HDRAutoSwitcher.exe --status                      # 查看当前 HDR 状态
HDRAutoSwitcher.exe --set on|off                  # 手动打开/关闭 HDR
HDRAutoSwitcher.exe --probe                       # 开关一次 HDR 并回读验证
```

## 编译

运行 `build.bat` 即可，使用 Windows 自带的 `csc.exe` 编译，无需安装任何工具链或依赖。

## 实现原理

- 通过 `EnumDisplayDevices` 找到主显示器，再用 `QueryDisplayConfig` 匹配对应的显示路径与目标 ID
- Windows 11 24H2（build 26100）之前：`DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO` / `SET_ADVANCED_COLOR_STATE`
- 24H2 起 HDR 与广色域（WCG）API 分离，自动改用 `GET_ADVANCED_COLOR_INFO_2` / `SET_HDR_STATE`（按 `RtlGetVersion` 的真实版本号选择）
- WinForms 界面 + `NotifyIcon` 托盘；每秒轮询进程列表检测目标程序的启动与退出

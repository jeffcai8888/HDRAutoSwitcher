# HDRAutoSwitcher

监控指定程序，自动切换主显示器的 HDR 状态：

- 任一被监控程序启动后，自动打开主显示器的 HDR（如果原本已打开则保持不变）
- 被监控程序全部退出后，恢复之前的 HDR 状态
- 工具自身退出（Ctrl+C / 关闭窗口）时也会恢复

## 环境要求

- Windows 10 1709 及以上（含 Windows 11；24H2 及以上自动使用新的 HDR 专用 API）
- 显示器支持 HDR
- 无需安装 .NET：使用系统自带的 .NET Framework 4.x 运行/编译

## 使用方法

1. 编辑 exe 同目录下的 `HDRAutoSwitcher.ini`，每行写一个要监控的进程名（`.exe` 后缀可写可不写），`#` 或 `;` 开头的行为注释：

   ```ini
   game.exe
   Video Player.exe
   ```

2. 双击运行 `HDRAutoSwitcher.exe`（修改配置后需重启程序生效）。

也可以用命令行参数指定进程名（优先于配置文件）：

```
HDRAutoSwitcher.exe game.exe "Video Player.exe"
```

## 编译

运行 `build.bat` 即可，使用 Windows 自带的 `csc.exe` 编译，无需安装任何工具链或依赖。

## 调试命令

```
HDRAutoSwitcher.exe --probe        # 开关一次 HDR 并回读验证
HDRAutoSwitcher.exe --set on|off   # 手动打开/关闭 HDR 并显示当前状态
```

## 实现原理

- 通过 `EnumDisplayDevices` 找到主显示器，再用 `QueryDisplayConfig` 匹配对应的显示路径与目标 ID
- Windows 11 24H2（build 26100）之前：`DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO` / `SET_ADVANCED_COLOR_STATE`
- 24H2 起 HDR 与广色域（WCG）API 分离，自动改用 `GET_ADVANCED_COLOR_INFO_2` / `SET_HDR_STATE`（按 `RtlGetVersion` 的真实版本号选择）
- 每秒轮询进程列表检测目标程序的启动与退出

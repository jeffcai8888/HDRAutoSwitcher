using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

static class NativeMethods
{
    public const int QDC_ONLY_ACTIVE_PATHS = 2;
    public const int DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
    public const int DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9;
    public const int DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10;
    public const int DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2 = 15;
    public const int DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE = 16;
    public const uint DISPLAYCONFIG_PATH_MODE_IDX_INVALID = 0xFFFFFFFF;
    public const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x4;

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_RATIONAL
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint outputTechnology;
        public uint rotation;
        public uint scaling;
        public DISPLAYCONFIG_RATIONAL refreshRate;
        public uint scanLineOrdering;
        public bool targetAvailable;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_2DREGION
    {
        public uint cx;
        public uint cy;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_MODE_INFO
    {
        public uint infoType;
        public uint id;
        public LUID adapterId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public byte[] payload;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public int type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string viewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_ADVANCED_COLOR_INFO
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public uint value; // bit0: supported, bit1: enabled, bit2: wideColorEnforced, bit3: forceDisabled
        public uint colorEncoding;
        public uint bitsPerColorChannel;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public uint value; // bit0: enableAdvancedColor
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_ADVANCED_COLOR_INFO_2
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public uint value; // bit0: supported, bit1: active, bit3: limitedByPolicy, bit4: hdrSupported, bit5: hdrUserEnabled, bit6: wcgSupported, bit7: wcgUserEnabled
        public uint colorEncoding;
        public uint bitsPerColorChannel;
        public uint activeColorMode; // 0=SDR, 1=WCG, 2=HDR
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OSVERSIONINFOEXW
    {
        public uint dwOSVersionInfoSize;
        public uint dwMajorVersion;
        public uint dwMinorVersion;
        public uint dwBuildNumber;
        public uint dwPlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szCSDVersion;
        public ushort wServicePackMajor;
        public ushort wServicePackMinor;
        public ushort wSuiteMask;
        public byte wProductType;
        public byte wReserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAY_DEVICE
    {
        public uint cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [DllImport("user32.dll")]
    public static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    public static extern int QueryDisplayConfig(uint flags, ref uint numPathArrayElements,
        [In, Out] DISPLAYCONFIG_PATH_INFO[] pathArray, ref uint numModeInfoArrayElements,
        [In, Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray, IntPtr currentTopologyId);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME requestPacket);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_ADVANCED_COLOR_INFO requestPacket);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE requestPacket);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE setPacket);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_ADVANCED_COLOR_INFO_2 requestPacket);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_ADVANCED_COLOR_INFO_2 setPacket);

    [DllImport("ntdll.dll")]
    public static extern int RtlGetVersion(ref OSVERSIONINFOEXW versionInfo);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);
}

class HdrController
{
    private NativeMethods.LUID _adapterId;
    private uint _targetId;
    private bool _located;

    public bool Supported { get; private set; }
    public bool WideColorEnforced { get; private set; }
    public string LastError { get; private set; }

    private bool LocatePrimaryTarget()
    {
        string primaryGdiName = null;
        var dd = new NativeMethods.DISPLAY_DEVICE();
        dd.cb = (uint)Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE));
        for (uint i = 0; NativeMethods.EnumDisplayDevices(null, i, ref dd, 0); i++)
        {
            if ((dd.StateFlags & NativeMethods.DISPLAY_DEVICE_PRIMARY_DEVICE) != 0)
            {
                primaryGdiName = dd.DeviceName;
                break;
            }
        }
        if (primaryGdiName == null)
        {
            LastError = "EnumDisplayDevices 未找到主显示器";
            return false;
        }

        uint pathCount, modeCount;
        int err = NativeMethods.GetDisplayConfigBufferSizes(NativeMethods.QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount);
        if (err != 0 || pathCount == 0)
        {
            LastError = "GetDisplayConfigBufferSizes 失败，错误码 " + err;
            return false;
        }

        var paths = new NativeMethods.DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new NativeMethods.DISPLAYCONFIG_MODE_INFO[modeCount];
        err = NativeMethods.QueryDisplayConfig(NativeMethods.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
        if (err != 0)
        {
            LastError = "QueryDisplayConfig 失败，错误码 " + err;
            return false;
        }

        foreach (var path in paths)
        {
            var srcName = new NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            srcName.header.type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            srcName.header.size = (uint)Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME));
            srcName.header.adapterId = path.sourceInfo.adapterId;
            srcName.header.id = path.sourceInfo.id;
            if (NativeMethods.DisplayConfigGetDeviceInfo(ref srcName) != 0)
                continue;
            if (string.Equals(srcName.viewGdiDeviceName, primaryGdiName, StringComparison.OrdinalIgnoreCase))
            {
                // 高级色彩信息应取 target mode 对应的 modeInfo 项的 adapterId/id
                if (path.targetInfo.modeInfoIdx != NativeMethods.DISPLAYCONFIG_PATH_MODE_IDX_INVALID
                    && path.targetInfo.modeInfoIdx < modeCount)
                {
                    _adapterId = modes[path.targetInfo.modeInfoIdx].adapterId;
                    _targetId = modes[path.targetInfo.modeInfoIdx].id;
                }
                else
                {
                    _adapterId = path.targetInfo.adapterId;
                    _targetId = path.targetInfo.id;
                }
                _located = true;
                return true;
            }
        }
        LastError = "显示路径中未匹配到主显示器 " + primaryGdiName;
        return false;
    }

    private static bool UseNewApi()
    {
        var ver = new NativeMethods.OSVERSIONINFOEXW();
        ver.dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(NativeMethods.OSVERSIONINFOEXW));
        if (NativeMethods.RtlGetVersion(ref ver) != 0)
            return false;
        return ver.dwBuildNumber >= 26100; // Windows 11 24H2 起 HDR 与广色域 API 分离
    }

    public bool RefreshColorInfo(out bool enabled)
    {
        enabled = false;
        if (!_located && !LocatePrimaryTarget())
            return false;

        if (UseNewApi())
        {
            var info2 = new NativeMethods.DISPLAYCONFIG_ADVANCED_COLOR_INFO_2();
            info2.header.type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2;
            info2.header.size = (uint)Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_ADVANCED_COLOR_INFO_2));
            info2.header.adapterId = _adapterId;
            info2.header.id = _targetId;
            int err = NativeMethods.DisplayConfigGetDeviceInfo(ref info2);
            if (err != 0)
            {
                _located = false; // 显示器可能被热插拔，下次重新定位
                LastError = "GET_ADVANCED_COLOR_INFO_2 失败，错误码 " + err;
                return false;
            }
            Supported = (info2.value & 0x10) != 0; // highDynamicRangeSupported
            enabled = info2.activeColorMode == 2;
            return true;
        }

        var info = new NativeMethods.DISPLAYCONFIG_ADVANCED_COLOR_INFO();
        info.header.type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
        info.header.size = (uint)Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_ADVANCED_COLOR_INFO));
        info.header.adapterId = _adapterId;
        info.header.id = _targetId;
        int errOld = NativeMethods.DisplayConfigGetDeviceInfo(ref info);
        if (errOld != 0)
        {
            _located = false; // 显示器可能被热插拔，下次重新定位
            LastError = "GET_ADVANCED_COLOR_INFO 失败，错误码 " + errOld;
            return false;
        }
        Supported = (info.value & 0x1) != 0;
        enabled = (info.value & 0x2) != 0;
        WideColorEnforced = (info.value & 0x4) != 0;
        return true;
    }

    public bool SetHdr(bool enable, out string error)
    {
        error = null;
        if (!_located && !LocatePrimaryTarget())
        {
            error = "找不到主显示器";
            return false;
        }

        var info = new NativeMethods.DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE();
        info.header.type = UseNewApi()
            ? NativeMethods.DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE
            : NativeMethods.DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE;
        info.header.size = (uint)Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE));
        info.header.adapterId = _adapterId;
        info.header.id = _targetId;
        info.value = enable ? 1u : 0u;
        int err = NativeMethods.DisplayConfigSetDeviceInfo(ref info);
        if (err != 0)
        {
            error = "DisplayConfigSetDeviceInfo 失败，错误码 " + err;
            _located = false;
            return false;
        }
        return true;
    }
}

class Program
{
    static volatile bool _hdrHeld;      // 当前是否处于"已记录并可能打开 HDR"状态
    static bool _originalHdrEnabled;
    static HdrController _hdr;

    static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--probe")
        {
            var h = new HdrController();
            bool en;
            if (!h.RefreshColorInfo(out en))
            {
                Console.WriteLine("GET 失败: " + h.LastError);
                return 1;
            }
            Console.WriteLine("Supported=" + h.Supported + " Enabled=" + en + " WideColorEnforced=" + h.WideColorEnforced);
            string err;
            Console.WriteLine("尝试打开 HDR...");
            if (!h.SetHdr(true, out err))
            {
                Console.WriteLine("SET(on) 失败: " + err);
                return 1;
            }
            Thread.Sleep(1500);
            if (h.RefreshColorInfo(out en))
                Console.WriteLine("SET(on) 后 Enabled=" + en);
            Console.WriteLine("恢复原状...");
            if (!h.SetHdr(false, out err))
            {
                Console.WriteLine("SET(off) 失败: " + err);
                return 1;
            }
            Thread.Sleep(1000);
            if (h.RefreshColorInfo(out en))
                Console.WriteLine("SET(off) 后 Enabled=" + en);
            return 0;
        }

        if (args.Length == 2 && args[0] == "--set")
        {
            bool on = args[1].Equals("on", StringComparison.OrdinalIgnoreCase);
            var h2 = new HdrController();
            string err2;
            if (!h2.SetHdr(on, out err2))
            {
                Console.WriteLine("切换失败: " + err2);
                return 1;
            }
            bool st;
            if (h2.RefreshColorInfo(out st))
                Console.WriteLine("当前 HDR 状态: " + (st ? "开" : "关"));
            return 0;
        }

        var rawNames = new List<string>(args);
        if (rawNames.Count == 0)
        {
            // 无命令行参数时，从 exe 同目录的 HDRAutoSwitcher.ini 读取进程名
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HDRAutoSwitcher.ini");
            if (!File.Exists(configPath))
            {
                Console.WriteLine("用法: HDRAutoSwitcher.exe <进程名1> [进程名2] ...");
                Console.WriteLine("或在 exe 同目录创建 HDRAutoSwitcher.ini，每行一个进程名（# 或 ; 开头为注释）。");
                Console.WriteLine("示例: HDRAutoSwitcher.exe game.exe \"Video Player.exe\"");
                Console.WriteLine("监控指定进程: 任一启动后自动打开主显示器 HDR, 全部退出后恢复之前的 HDR 状态。");
                return 1;
            }
            foreach (var line in File.ReadAllLines(configPath))
            {
                var t = line.Trim();
                if (t.Length > 0 && !t.StartsWith("#") && !t.StartsWith(";"))
                    rawNames.Add(t);
            }
            if (rawNames.Count == 0)
            {
                Console.WriteLine("配置文件 " + configPath + " 中没有有效的进程名（每行一个，# 或 ; 开头为注释）。");
                return 1;
            }
            Console.WriteLine("已从配置文件读取: " + configPath);
        }

        var names = new List<string>();
        foreach (var a in rawNames)
        {
            var n = a.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? a.Substring(0, a.Length - 4) : a;
            if (n.Length > 0 && !names.Contains(n))
                names.Add(n);
        }

        _hdr = new HdrController();
        bool current;
        if (!_hdr.RefreshColorInfo(out current))
        {
            Console.WriteLine("无法读取主显示器状态: " + _hdr.LastError);
            return 1;
        }
        if (!_hdr.Supported)
            Console.WriteLine("警告: 主显示器不支持 HDR，将仅监控进程而不切换 HDR。");
        Console.WriteLine("当前主显示器 HDR: " + (current ? "开" : "关"));
        Console.WriteLine("正在监控: " + string.Join(", ", names.ToArray()));

        Console.CancelKeyPress += OnExit;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        bool anyRunning = false;
        while (true)
        {
            Thread.Sleep(1000);
            bool nowRunning = AnyProcessRunning(names);

            if (nowRunning && !anyRunning)
            {
                bool enabled;
                if (_hdr.Supported && _hdr.RefreshColorInfo(out enabled))
                {
                    _originalHdrEnabled = enabled;
                    _hdrHeld = true;
                    Console.WriteLine("检测到目标进程启动。原 HDR 状态: " + (enabled ? "开" : "关"));
                    if (!enabled)
                    {
                        string error;
                        if (_hdr.SetHdr(true, out error))
                            Console.WriteLine("已打开主显示器 HDR。");
                        else
                            Console.WriteLine("打开 HDR 失败: " + error);
                    }
                    else
                    {
                        Console.WriteLine("HDR 已开启，保持不变。");
                    }
                }
            }
            else if (!nowRunning && anyRunning)
            {
                Console.WriteLine("目标进程已全部退出。");
                RestoreHdr();
            }
            anyRunning = nowRunning;
        }
    }

    static bool AnyProcessRunning(List<string> names)
    {
        foreach (var n in names)
        {
            if (Process.GetProcessesByName(n).Length > 0)
                return true;
        }
        return false;
    }

    static void RestoreHdr()
    {
        if (!_hdrHeld || !_hdr.Supported)
            return;
        _hdrHeld = false;

        bool enabled;
        if (_hdr.RefreshColorInfo(out enabled) && enabled == _originalHdrEnabled)
            return;

        string error;
        if (_hdr.SetHdr(_originalHdrEnabled, out error))
            Console.WriteLine("已恢复 HDR 状态: " + (_originalHdrEnabled ? "开" : "关"));
        else
            Console.WriteLine("恢复 HDR 失败: " + error);
    }

    static void OnExit(object sender, ConsoleCancelEventArgs e)
    {
        Console.WriteLine("正在退出...");
        RestoreHdr();
    }

    static void OnProcessExit(object sender, EventArgs e)
    {
        RestoreHdr();
    }
}

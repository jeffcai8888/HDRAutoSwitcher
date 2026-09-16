using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

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
    public static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE setPacket);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_ADVANCED_COLOR_INFO_2 requestPacket);

    [DllImport("ntdll.dll")]
    public static extern int RtlGetVersion(ref OSVERSIONINFOEXW versionInfo);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("kernel32.dll")]
    public static extern bool AttachConsole(uint dwProcessId);

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr handle);
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

// 监控状态机：控制台与图形界面共用
class HdrMonitor
{
    public readonly HdrController Hdr = new HdrController();
    public readonly List<string> Names = new List<string>();
    public bool CurrentHdrEnabled;
    public Action<string> OnLog = delegate { };
    public Action<bool> OnHdrStateChanged = delegate { };

    private bool _anyRunning;
    private bool _hdrHeld;
    private bool _originalHdrEnabled;

    public static string ConfigPath
    {
        get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HDRAutoSwitcher.ini"); }
    }

    // 返回 null 表示成功，否则为错误消息
    public string LoadConfig()
    {
        if (!File.Exists(ConfigPath))
            return "找不到配置文件 " + ConfigPath;

        var loaded = new List<string>();
        foreach (var line in File.ReadAllLines(ConfigPath))
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith("#") || t.StartsWith(";"))
                continue;
            if (t.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(0, t.Length - 4);
            if (t.Length > 0 && !loaded.Contains(t))
                loaded.Add(t);
        }
        if (loaded.Count == 0)
            return "配置文件中没有有效的进程名（每行一个，# 或 ; 开头为注释）";

        Names.Clear();
        Names.AddRange(loaded);
        _anyRunning = AnyProcessRunning();
        return null;
    }

    public bool InitDisplay(out string error)
    {
        error = null;
        bool current;
        if (!Hdr.RefreshColorInfo(out current))
        {
            error = "无法读取主显示器状态: " + Hdr.LastError;
            return false;
        }
        CurrentHdrEnabled = current;
        return true;
    }

    public void Tick()
    {
        bool enabled;
        if (Hdr.RefreshColorInfo(out enabled))
        {
            if (enabled != CurrentHdrEnabled)
            {
                CurrentHdrEnabled = enabled;
                OnHdrStateChanged(enabled);
            }
        }

        if (Names.Count == 0)
            return;

        bool nowRunning = AnyProcessRunning();
        if (nowRunning && !_anyRunning)
        {
            if (Hdr.Supported && Hdr.RefreshColorInfo(out enabled))
            {
                _originalHdrEnabled = enabled;
                _hdrHeld = true;
                OnLog("检测到目标进程启动。原 HDR 状态: " + (enabled ? "开" : "关"));
                if (!enabled)
                {
                    string error;
                    if (Hdr.SetHdr(true, out error))
                    {
                        CurrentHdrEnabled = true;
                        OnLog("已打开主显示器 HDR。");
                        OnHdrStateChanged(true);
                    }
                    else
                    {
                        OnLog("打开 HDR 失败: " + error);
                    }
                }
                else
                {
                    OnLog("HDR 已开启，保持不变。");
                }
            }
        }
        else if (!nowRunning && _anyRunning)
        {
            OnLog("目标进程已全部退出。");
            RestoreHdr();
        }
        _anyRunning = nowRunning;
    }

    public void RestoreHdr()
    {
        if (!_hdrHeld || !Hdr.Supported)
            return;
        _hdrHeld = false;

        bool enabled;
        if (Hdr.RefreshColorInfo(out enabled) && enabled == _originalHdrEnabled)
            return;

        string error;
        if (Hdr.SetHdr(_originalHdrEnabled, out error))
        {
            CurrentHdrEnabled = _originalHdrEnabled;
            OnLog("已恢复 HDR 状态: " + (_originalHdrEnabled ? "开" : "关"));
            OnHdrStateChanged(_originalHdrEnabled);
        }
        else
        {
            OnLog("恢复 HDR 失败: " + error);
        }
    }

    private bool AnyProcessRunning()
    {
        foreach (var n in Names)
        {
            if (Process.GetProcessesByName(n).Length > 0)
                return true;
        }
        return false;
    }
}

class MainForm : Form
{
    private readonly HdrMonitor _monitor = new HdrMonitor();
    private readonly System.Windows.Forms.Timer _timer;
    private readonly NotifyIcon _tray;
    private readonly Label _lblNames;
    private readonly Label _lblHdr;
    private readonly TextBox _txtLog;
    private Icon _iconOn;
    private Icon _iconOff;
    private bool _reallyExit;

    public MainForm()
    {
        Text = "HDR Auto Switcher";
        ClientSize = new Size(520, 380);
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = true;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;

        _lblHdr = new Label();
        _lblHdr.SetBounds(12, 10, 496, 20);
        _lblHdr.Font = new Font(Font.FontFamily, 10, FontStyle.Bold);

        _lblNames = new Label();
        _lblNames.SetBounds(12, 34, 496, 20);

        _txtLog = new TextBox();
        _txtLog.SetBounds(12, 60, 496, 266);
        _txtLog.Multiline = true;
        _txtLog.ReadOnly = true;
        _txtLog.ScrollBars = ScrollBars.Vertical;
        _txtLog.BackColor = Color.White;

        var btnReload = new Button();
        btnReload.Text = "重新加载配置";
        btnReload.SetBounds(12, 336, 120, 28);
        btnReload.Click += delegate { LoadConfig(); };

        var btnHide = new Button();
        btnHide.Text = "隐藏到托盘";
        btnHide.SetBounds(388, 336, 120, 28);
        btnHide.Click += delegate { HideToTray(); };

        Controls.Add(_lblHdr);
        Controls.Add(_lblNames);
        Controls.Add(_txtLog);
        Controls.Add(btnReload);
        Controls.Add(btnHide);

        _iconOff = CreateIcon(false);
        _iconOn = CreateIcon(true);
        Icon = _iconOff;

        var menu = new ContextMenuStrip();
        menu.Items.Add("显示窗口", null, delegate { ShowWindow(); });
        menu.Items.Add("退出", null, delegate { ExitApp(); });
        _tray = new NotifyIcon();
        _tray.Text = "HDR Auto Switcher";
        _tray.Icon = _iconOff;
        _tray.ContextMenuStrip = menu;
        _tray.Visible = true;
        _tray.DoubleClick += delegate { ShowWindow(); };

        _monitor.OnLog = Log;
        _monitor.OnHdrStateChanged = OnHdrStateChanged;

        string error;
        if (!_monitor.InitDisplay(out error))
        {
            Log(error);
        }
        else if (!_monitor.Hdr.Supported)
        {
            Log("警告: 主显示器不支持 HDR，将仅监控进程而不切换 HDR。");
        }
        UpdateHdrLabel();

        LoadConfig();

        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = 1000;
        _timer.Tick += delegate { _monitor.Tick(); UpdateHdrLabel(); };
        _timer.Start();
    }

    private void LoadConfig()
    {
        string err = _monitor.LoadConfig();
        if (err != null)
        {
            Log(err + "，请在 exe 同目录编辑 HDRAutoSwitcher.ini 后点击“重新加载配置”。");
        }
        else
        {
            Log("已加载配置，监控: " + string.Join(", ", _monitor.Names.ToArray()));
        }
        _lblNames.Text = "监控进程: " + (_monitor.Names.Count > 0 ? string.Join(", ", _monitor.Names.ToArray()) : "（未配置）");
    }

    private void Log(string msg)
    {
        string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg;
        _txtLog.AppendText(line + Environment.NewLine);
        if (_txtLog.Lines.Length > 500)
        {
            var lines = _txtLog.Lines;
            var kept = new string[400];
            Array.Copy(lines, lines.Length - 400, kept, 0, 400);
            _txtLog.Lines = kept;
        }
    }

    private void OnHdrStateChanged(bool hdrOn)
    {
        var icon = hdrOn ? _iconOn : _iconOff;
        _tray.Icon = icon;
        Icon = icon;
        UpdateHdrLabel();
    }

    private void UpdateHdrLabel()
    {
        _lblHdr.Text = "主显示器 HDR: " + (_monitor.CurrentHdrEnabled ? "开" : "关")
            + (_monitor.Hdr.Supported ? "" : "（显示器不支持 HDR）");
        _lblHdr.ForeColor = _monitor.CurrentHdrEnabled ? Color.DarkOrange : Color.DimGray;
    }

    private void HideToTray()
    {
        Hide();
        _tray.ShowBalloonTip(2000, "HDR Auto Switcher", "已隐藏到托盘，双击图标可重新打开窗口。", ToolTipIcon.Info);
    }

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApp()
    {
        _reallyExit = true;
        _monitor.RestoreHdr();
        _tray.Visible = false;
        Application.Exit();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
            HideToTray();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_reallyExit)
        {
            e.Cancel = true; // 关闭按钮 = 最小化到托盘，通过托盘菜单“退出”结束程序
            HideToTray();
            return;
        }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Dispose();
            _timer.Dispose();
            if (_iconOn != null) { NativeMethods.DestroyIcon(_iconOn.Handle); _iconOn.Dispose(); }
            if (_iconOff != null) { NativeMethods.DestroyIcon(_iconOff.Handle); _iconOff.Dispose(); }
        }
        base.Dispose(disposing);
    }

    private static Icon CreateIcon(bool hdrOn)
    {
        var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(hdrOn ? Color.DarkOrange : Color.DimGray);
            using (var font = new Font("Arial", 11, FontStyle.Bold))
                g.DrawString("HDR", font, Brushes.White, 1, 8);
        }
        Icon icon = Icon.FromHandle(bmp.GetHicon());
        bmp.Dispose();
        return icon;
    }
}

class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            // 无参数：图形界面模式
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }

        // 有参数：控制台模式（输出附加到调用方终端）
        NativeMethods.AttachConsole(0xFFFFFFFF);

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
            Console.WriteLine(on ? "HDR 已打开" : "HDR 已关闭");
            return 0;
        }

        if (args.Length == 1 && args[0] == "--status")
        {
            var h3 = new HdrController();
            bool st;
            if (!h3.RefreshColorInfo(out st))
            {
                Console.WriteLine("读取失败: " + h3.LastError);
                return 1;
            }
            Console.WriteLine("当前 HDR 状态: " + (st ? "开" : "关"));
            return 0;
        }

        // 命令行指定进程名的监控模式（输出到终端）
        var monitor = new HdrMonitor();
        monitor.OnLog = Console.WriteLine;
        foreach (var a in args)
        {
            var n = a.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? a.Substring(0, a.Length - 4) : a;
            if (n.Length > 0 && !monitor.Names.Contains(n))
                monitor.Names.Add(n);
        }

        string error;
        if (!monitor.InitDisplay(out error))
        {
            Console.WriteLine(error);
            return 1;
        }
        if (!monitor.Hdr.Supported)
            Console.WriteLine("警告: 主显示器不支持 HDR，将仅监控进程而不切换 HDR。");
        Console.WriteLine("当前主显示器 HDR: " + (monitor.CurrentHdrEnabled ? "开" : "关"));
        Console.WriteLine("正在监控: " + string.Join(", ", monitor.Names.ToArray()));
        Console.WriteLine("（不带参数运行则为托盘图形界面；从 HDRAutoSwitcher.ini 读取进程名）");

        Console.CancelKeyPress += delegate { Console.WriteLine("正在退出..."); monitor.RestoreHdr(); };
        AppDomain.CurrentDomain.ProcessExit += delegate { monitor.RestoreHdr(); };

        while (true)
        {
            Thread.Sleep(1000);
            monitor.Tick();
        }
    }
}

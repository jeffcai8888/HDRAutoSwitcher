using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
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

// 多语言字符串表。支持: 简体中文, 繁體中文, English, 日本語, Français, Español, Português, Deutsch
static class Lang
{
    public const string Default = "en";
    public static string Current = Default;

    public static readonly string[] Codes = { "zh-CN", "zh-TW", "en", "ja", "fr", "es", "pt", "de" };

    public static string DisplayName(string code)
    {
        switch (code)
        {
            case "zh-CN": return "简体中文";
            case "zh-TW": return "繁體中文";
            case "ja": return "日本語";
            case "fr": return "Français";
            case "es": return "Español";
            case "pt": return "Português";
            case "de": return "Deutsch";
            default: return "English";
        }
    }

    // 数组顺序与 Codes 一致: zh-CN, zh-TW, en, ja, fr, es, pt, de
    static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        { "ON", new[]{ "开", "開", "On", "オン", "Activé", "Activado", "Ativado", "Ein" } },
        { "OFF", new[]{ "关", "關", "Off", "オフ", "Désactivé", "Desactivado", "Desativado", "Aus" } },
        { "HDR_STATUS", new[]{ "主显示器 HDR: {0}", "主顯示器 HDR: {0}", "Primary display HDR: {0}", "メインディスプレイ HDR: {0}", "HDR de l'écran principal : {0}", "HDR de la pantalla principal: {0}", "HDR do monitor principal: {0}", "HDR des Hauptbildschirms: {0}" } },
        { "HDR_UNSUPPORTED_SUFFIX", new[]{ "（显示器不支持 HDR）", "（顯示器不支援 HDR）", " (HDR not supported)", "（HDR 非対応）", " (HDR non pris en charge)", " (HDR no compatible)", " (HDR não suportado)", " (HDR nicht unterstützt)" } },
        { "GRP_PROCESSES", new[]{ "监控的进程", "監控的處理程序", "Watched processes", "監視するプロセス", "Processus surveillés", "Procesos vigilados", "Processos monitorizados", "Überwachte Prozesse" } },
        { "BTN_ADD", new[]{ "添加 exe...", "新增 exe...", "Add exe...", "exe を追加...", "Ajouter un exe...", "Añadir exe...", "Adicionar exe...", "exe hinzufügen..." } },
        { "BTN_REMOVE", new[]{ "删除选中", "刪除選取", "Remove selected", "選択を削除", "Supprimer la sélection", "Eliminar selección", "Remover selecionado", "Auswahl entfernen" } },
        { "BTN_SAVE", new[]{ "保存配置", "儲存設定", "Save config", "設定を保存", "Enregistrer", "Guardar config.", "Guardar configuração", "Speichern" } },
        { "BTN_OPEN_CONFIG", new[]{ "打开配置文件", "開啟設定檔", "Open config file", "設定ファイルを開く", "Ouvrir la config", "Abrir configuración", "Abrir configuração", "Konfiguration öffnen" } },
        { "BTN_HIDE", new[]{ "隐藏到托盘", "隱藏到系統匣", "Hide to tray", "トレイに最小化", "Réduire dans la zone de notification", "Ocultar a la bandeja", "Ocultar para a bandeja", "In Infobereich minimieren" } },
        { "MENU_LANG", new[]{ "语言", "語言", "Language", "言語", "Langue", "Idioma", "Idioma", "Sprache" } },
        { "TRAY_SHOW", new[]{ "显示窗口", "顯示視窗", "Show window", "ウィンドウを表示", "Afficher la fenêtre", "Mostrar ventana", "Mostrar janela", "Fenster anzeigen" } },
        { "TRAY_EXIT", new[]{ "退出", "結束", "Exit", "終了", "Quitter", "Salir", "Sair", "Beenden" } },
        { "BALLOON_HIDE", new[]{ "已隐藏到托盘，双击图标可重新打开窗口。", "已隱藏到系統匣，雙擊圖示可重新開啟視窗。", "Minimized to tray. Double-click the icon to reopen the window.", "トレイに最小化しました。アイコンのダブルクリックで再表示します。", "Réduit dans la zone de notification. Double-cliquez sur l'icône pour rouvrir.", "Oculto a la bandeja. Doble clic en el icono para reabrir.", "Oculto na bandeja. Clique duas vezes no ícone para reabrir.", "In den Infobereich minimiert. Doppelklick auf das Symbol zum erneuten Öffnen." } },
        { "DLG_ADD_TITLE", new[]{ "选择要监控的程序（可多选）", "選擇要監控的程式（可多選）", "Select programs to watch (multi-select)", "監視するプログラムを選択（複数選択可）", "Sélectionnez les programmes à surveiller (multi-sélection)", "Selecciona los programas a vigilar (selección múltiple)", "Selecione os programas a monitorizar (seleção múltipla)", "Programme zum Überwachen auswählen (Mehrfachauswahl)" } },
        { "DLG_ADD_FILTER", new[]{ "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*", "執行檔 (*.exe)|*.exe|所有檔案 (*.*)|*.*", "Executable (*.exe)|*.exe|All files (*.*)|*.*", "実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*", "Exécutable (*.exe)|*.exe|Tous les fichiers (*.*)|*.*", "Ejecutable (*.exe)|*.exe|Todos los archivos (*.*)|*.*", "Executável (*.exe)|*.exe|Todos os ficheiros (*.*)|*.*", "Ausführbare Datei (*.exe)|*.exe|Alle Dateien (*.*)|*.*" } },
        { "LOG_LOADED", new[]{ "已加载配置，监控: {0}", "已載入設定，監控: {0}", "Config loaded, watching: {0}", "設定を読み込みました。監視中: {0}", "Config chargée, surveillance : {0}", "Configuración cargada, vigilando: {0}", "Configuração carregada, monitorizando: {0}", "Konfiguration geladen, überwacht: {0}" } },
        { "LOG_NO_NAMES", new[]{ "配置文件中没有有效的进程名（每行一个，# 或 ; 开头为注释）", "設定檔中沒有有效的處理程序名稱（每行一個，# 或 ; 開頭為註解）", "No valid process names in the config file (one per line; # or ; starts a comment)", "設定ファイルに有効なプロセス名がありません（1 行に 1 つ。# または ; で始まる行はコメント）", "Aucun nom de processus valide dans le fichier de config (un par ligne ; # ou ; pour les commentaires)", "No hay nombres de proceso válidos en el archivo (uno por línea; # o ; para comentarios)", "Nenhum nome de processo válido no ficheiro (um por linha; # ou ; para comentários)", "Keine gültigen Prozessnamen in der Konfigurationsdatei (einer pro Zeile; # oder ; für Kommentare)" } },
        { "LOG_NO_NAMES_HINT", new[]{ "请点击“添加 exe...”选择要监控的程序，然后保存配置。", "請點擊「新增 exe...」選擇要監控的程式，然後儲存設定。", "Click \"Add exe...\" to choose programs to watch, then save the config.", "「exe を追加...」で監視するプログラムを選び、設定を保存してください。", "Cliquez sur \"Ajouter un exe...\" pour choisir les programmes, puis enregistrez.", "Haz clic en \"Añadir exe...\" para elegir los programas y luego guarda.", "Clique em \"Adicionar exe...\" para escolher os programas e depois guarde.", "Klicken Sie auf „exe hinzufügen...“, um Programme auszuwählen, und speichern Sie." } },
        { "LOG_ADDED", new[]{ "已添加 {0} 个进程（尚未保存，点击“保存配置”生效）。", "已新增 {0} 個處理程序（尚未儲存，點擊「儲存設定」生效）。", "Added {0} process(es) (not saved yet — click \"Save config\").", "{0} 個のプロセスを追加しました（未保存。「設定を保存」をクリック）。", "{0} processus ajouté(s) (non enregistré — cliquez sur \"Enregistrer\").", "Se añadieron {0} proceso(s) (sin guardar — haz clic en \"Guardar\").", "{0} processo(s) adicionado(s) (ainda não guardado — clique em \"Guardar\").", "{0} Prozess(e) hinzugefügt (noch nicht gespeichert — „Speichern“ klicken)." } },
        { "LOG_REMOVED", new[]{ "已移除 {0}（尚未保存，点击“保存配置”生效）。", "已移除 {0}（尚未儲存，點擊「儲存設定」生效）。", "Removed {0} (not saved yet — click \"Save config\").", "{0} を削除しました（未保存。「設定を保存」をクリック）。", "{0} supprimé (non enregistré — cliquez sur \"Enregistrer\").", "{0} eliminado (sin guardar — haz clic en \"Guardar\").", "{0} removido (ainda não guardado — clique em \"Guardar\").", "{0} entfernt (noch nicht gespeichert — „Speichern“ klicken)." } },
        { "LOG_SAVED", new[]{ "配置已保存到 {0} 并立即生效。", "設定已儲存到 {0} 並立即生效。", "Config saved to {0} and applied immediately.", "設定を {0} に保存し、ただちに適用しました。", "Config enregistrée dans {0} et appliquée immédiatement.", "Configuración guardada en {0} y aplicada de inmediato.", "Configuração guardada em {0} e aplicada de imediato.", "Konfiguration nach {0} gespeichert und sofort angewendet." } },
        { "LOG_SAVE_FAIL", new[]{ "保存配置失败: {0}", "儲存設定失敗: {0}", "Failed to save config: {0}", "設定の保存に失敗: {0}", "Échec de l'enregistrement : {0}", "Error al guardar la configuración: {0}", "Falha ao guardar a configuração: {0}", "Speichern der Konfiguration fehlgeschlagen: {0}" } },
        { "LOG_OPEN_FAIL", new[]{ "打开配置文件失败: {0}", "開啟設定檔失敗: {0}", "Failed to open config file: {0}", "設定ファイルを開けませんでした: {0}", "Impossible d'ouvrir le fichier : {0}", "Error al abrir el archivo: {0}", "Falha ao abrir o ficheiro: {0}", "Konfigurationsdatei konnte nicht geöffnet werden: {0}" } },
        { "WARN_UNSUPPORTED", new[]{ "警告: 主显示器不支持 HDR，将仅监控进程而不切换 HDR。", "警告: 主顯示器不支援 HDR，僅監控處理程序，不切換 HDR。", "Warning: the primary display does not support HDR; processes will be watched without toggling HDR.", "警告: メインディスプレイは HDR 非対応です。プロセスの監視のみ行い、HDR は切り替えません。", "Avertissement : l'écran principal ne prend pas en charge le HDR ; surveillance sans bascule HDR.", "Aviso: la pantalla principal no admite HDR; solo se vigilarán los procesos sin cambiar HDR.", "Aviso: o monitor principal não suporta HDR; apenas os processos serão monitorizados.", "Warnung: Der Hauptbildschirm unterstützt kein HDR; Prozesse werden nur überwacht." } },
        { "LOG_DISPLAY_FAIL", new[]{ "无法读取主显示器状态: {0}", "無法讀取主顯示器狀態: {0}", "Cannot read primary display state: {0}", "メインディスプレイの状態を読み取れません: {0}", "Impossible de lire l'état de l'écran principal : {0}", "No se puede leer el estado de la pantalla: {0}", "Não é possível ler o estado do monitor: {0}", "Status des Hauptbildschirms nicht lesbar: {0}" } },
        { "LOG_DETECTED", new[]{ "检测到目标进程启动。原 HDR 状态: {0}", "偵測到目標處理程序啟動。原 HDR 狀態: {0}", "Watched process started. Previous HDR state: {0}", "対象プロセスの起動を検出しました。元の HDR 状態: {0}", "Processus surveillé démarré. État HDR précédent : {0}", "Proceso vigilado iniciado. Estado HDR anterior: {0}", "Processo monitorizado iniciado. Estado HDR anterior: {0}", "Überwachter Prozess gestartet. Vorheriger HDR-Status: {0}" } },
        { "LOG_HDR_ON", new[]{ "已打开主显示器 HDR。", "已開啟主顯示器 HDR。", "HDR enabled on the primary display.", "メインディスプレイの HDR をオンにしました。", "HDR activé sur l'écran principal.", "HDR activado en la pantalla principal.", "HDR ativado no monitor principal.", "HDR auf dem Hauptbildschirm aktiviert." } },
        { "LOG_HDR_ON_FAIL", new[]{ "打开 HDR 失败: {0}", "開啟 HDR 失敗: {0}", "Failed to enable HDR: {0}", "HDR の有効化に失敗: {0}", "Échec de l'activation du HDR : {0}", "Error al activar HDR: {0}", "Falha ao ativar HDR: {0}", "HDR-Aktivierung fehlgeschlagen: {0}" } },
        { "LOG_HDR_KEPT", new[]{ "HDR 已开启，保持不变。", "HDR 已開啟，保持不變。", "HDR already on; left unchanged.", "HDR はすでにオンです。変更しません。", "HDR déjà activé ; inchangé.", "HDR ya estaba activado; sin cambios.", "HDR já estava ativado; sem alterações.", "HDR bereits aktiv; unverändert." } },
        { "LOG_ALL_EXITED", new[]{ "目标进程已全部退出。", "目標處理程序已全部結束。", "All watched processes have exited.", "対象プロセスがすべて終了しました。", "Tous les processus surveillés sont terminés.", "Todos los procesos vigilados han finalizado.", "Todos os processos monitorizados terminaram.", "Alle überwachten Prozesse wurden beendet." } },
        { "LOG_RESTORED", new[]{ "已恢复 HDR 状态: {0}", "已恢復 HDR 狀態: {0}", "HDR state restored: {0}", "HDR 状態を復元しました: {0}", "État HDR restauré : {0}", "Estado HDR restaurado: {0}", "Estado HDR restaurado: {0}", "HDR-Status wiederhergestellt: {0}" } },
        { "LOG_RESTORE_FAIL", new[]{ "恢复 HDR 失败: {0}", "恢復 HDR 失敗: {0}", "Failed to restore HDR: {0}", "HDR の復元に失敗: {0}", "Échec de la restauration du HDR : {0}", "Error al restaurar HDR: {0}", "Falha ao restaurar HDR: {0}", "HDR-Wiederherstellung fehlgeschlagen: {0}" } },
    };

    public static string T(string key)
    {
        string[] vals;
        if (Table.TryGetValue(key, out vals))
        {
            int idx = Array.IndexOf(Codes, Current);
            if (idx >= 0 && idx < vals.Length && vals[idx] != null)
                return vals[idx];
            return vals[2]; // 回退到英语
        }
        return key;
    }

    public static string T(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }

    public static string On { get { return T("ON"); } }
    public static string Off { get { return T("OFF"); } }

    public static string SavePath
    {
        get { return Path.Combine(Path.GetDirectoryName(HdrMonitor.ConfigPath), "language.cfg"); }
    }

    // 优先级: 用户保存的选择 > 系统语言 > 英语
    public static void Init()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                string saved = File.ReadAllText(SavePath).Trim();
                if (Array.IndexOf(Codes, saved) >= 0)
                {
                    Current = saved;
                    return;
                }
            }
        }
        catch { }
        Current = Detect();
    }

    public static void Set(string code)
    {
        if (Array.IndexOf(Codes, code) < 0)
            return;
        Current = code;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            File.WriteAllText(SavePath, code);
        }
        catch { }
    }

    static string Detect()
    {
        string name = CultureInfo.CurrentUICulture.Name; // 如 zh-CN, ja-JP
        if (name.StartsWith("zh"))
        {
            return (name.IndexOf("Hant") >= 0 || name.IndexOf("TW") >= 0
                || name.IndexOf("HK") >= 0 || name.IndexOf("MO") >= 0) ? "zh-TW" : "zh-CN";
        }
        string two = name.Length >= 2 ? name.Substring(0, 2).ToLowerInvariant() : "";
        switch (two)
        {
            case "ja": return "ja";
            case "fr": return "fr";
            case "es": return "es";
            case "pt": return "pt";
            case "de": return "de";
            default: return Default; // 不在支持范围内显示英语
        }
    }
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
        get
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HDRAutoSwitcher", "HDRAutoSwitcher.ini");
        }
    }

    private const string ConfigTemplate =
        "# HDRAutoSwitcher 配置文件\r\n" +
        "# 每行一个要监控的进程名（.exe 后缀可写可不写），# 或 ; 开头的行是注释。\r\n" +
        "# 任一进程启动后自动打开主显示器 HDR，全部退出后恢复之前的 HDR 状态。\r\n" +
        "# 也可以在主窗口中通过“添加 exe...”选择程序，点“保存配置”后立即生效。\r\n" +
        "\r\n" +
        "# 示例：\r\n" +
        "# game.exe\r\n" +
        "# Video Player.exe\r\n";

    // 确保配置文件存在：不存在则新建；兼容旧版本 exe 同目录的 ini，存在则迁移
    public static void EnsureConfigExists()
    {
        if (File.Exists(ConfigPath))
            return;
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
        string legacy = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HDRAutoSwitcher.ini");
        if (File.Exists(legacy))
        {
            File.Copy(legacy, ConfigPath);
            return;
        }
        File.WriteAllText(ConfigPath, ConfigTemplate);
    }

    // 返回 null 表示成功，否则为错误消息
    public string LoadConfig()
    {
        EnsureConfigExists();

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
            return Lang.T("LOG_NO_NAMES");

        Names.Clear();
        Names.AddRange(loaded);
        _anyRunning = AnyProcessRunning();
        return null;
    }

    // 保存当前 Names 到配置文件，保留原有注释行
    public void SaveConfig()
    {
        var lines = new List<string>();
        if (File.Exists(ConfigPath))
        {
            foreach (var line in File.ReadAllLines(ConfigPath))
            {
                var t = line.Trim();
                if (t.StartsWith("#") || t.StartsWith(";"))
                    lines.Add(line);
            }
        }
        if (lines.Count > 0)
            lines.Add("");
        lines.AddRange(Names);
        File.WriteAllLines(ConfigPath, lines.ToArray());
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
                OnLog(Lang.T("LOG_DETECTED", enabled ? Lang.On : Lang.Off));
                if (!enabled)
                {
                    string error;
                    if (Hdr.SetHdr(true, out error))
                    {
                        CurrentHdrEnabled = true;
                        OnLog(Lang.T("LOG_HDR_ON"));
                        OnHdrStateChanged(true);
                    }
                    else
                    {
                        OnLog(Lang.T("LOG_HDR_ON_FAIL", error));
                    }
                }
                else
                {
                    OnLog(Lang.T("LOG_HDR_KEPT"));
                }
            }
        }
        else if (!nowRunning && _anyRunning)
        {
            OnLog(Lang.T("LOG_ALL_EXITED"));
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
            OnLog(Lang.T("LOG_RESTORED", _originalHdrEnabled ? Lang.On : Lang.Off));
            OnHdrStateChanged(_originalHdrEnabled);
        }
        else
        {
            OnLog(Lang.T("LOG_RESTORE_FAIL", error));
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
    private readonly Label _lblHdr;
    private readonly GroupBox _grpConfig;
    private readonly ListBox _lstNames;
    private readonly Button _btnAdd;
    private readonly Button _btnRemove;
    private readonly Button _btnSave;
    private readonly Button _btnOpenConfig;
    private readonly Button _btnHide;
    private readonly ToolStripMenuItem _menuLang;
    private readonly ToolStripMenuItem _trayShow;
    private readonly ToolStripMenuItem _trayExit;
    private readonly TextBox _txtLog;
    private Icon _iconOn;
    private Icon _iconOff;
    private bool _reallyExit;

    public MainForm()
    {
        Lang.Init();

        Text = "HDR Auto Switcher";
        ClientSize = new Size(560, 524);
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = true;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;

        // 语言选择菜单
        _menuLang = new ToolStripMenuItem();
        foreach (var code in Lang.Codes)
        {
            var item = new ToolStripMenuItem(Lang.DisplayName(code));
            item.Tag = code;
            item.Click += LanguageClicked;
            _menuLang.DropDownItems.Add(item);
        }
        var menuStrip = new MenuStrip();
        menuStrip.Items.Add(_menuLang);
        MainMenuStrip = menuStrip;
        Controls.Add(menuStrip);

        _lblHdr = new Label();
        _lblHdr.SetBounds(12, 34, 536, 20);
        _lblHdr.Font = new Font(Font.FontFamily, 10, FontStyle.Bold);

        _grpConfig = new GroupBox();
        _grpConfig.SetBounds(12, 60, 536, 158);

        _lstNames = new ListBox();
        _lstNames.SetBounds(10, 20, 380, 128);

        _btnAdd = new Button();
        _btnAdd.SetBounds(400, 20, 126, 34);
        _btnAdd.Click += delegate { AddExe(); };

        _btnRemove = new Button();
        _btnRemove.SetBounds(400, 62, 126, 34);
        _btnRemove.Click += delegate { RemoveSelected(); };

        _btnSave = new Button();
        _btnSave.SetBounds(400, 104, 126, 44);
        _btnSave.Click += delegate { SaveConfig(); };

        _grpConfig.Controls.Add(_lstNames);
        _grpConfig.Controls.Add(_btnAdd);
        _grpConfig.Controls.Add(_btnRemove);
        _grpConfig.Controls.Add(_btnSave);

        _txtLog = new TextBox();
        _txtLog.SetBounds(12, 228, 536, 246);
        _txtLog.Multiline = true;
        _txtLog.ReadOnly = true;
        _txtLog.ScrollBars = ScrollBars.Vertical;
        _txtLog.BackColor = Color.White;

        _btnOpenConfig = new Button();
        _btnOpenConfig.SetBounds(12, 484, 150, 30);
        _btnOpenConfig.Click += delegate { OpenConfigFile(); };

        _btnHide = new Button();
        _btnHide.SetBounds(398, 484, 150, 30);
        _btnHide.Click += delegate { HideToTray(); };

        Controls.Add(_lblHdr);
        Controls.Add(_grpConfig);
        Controls.Add(_txtLog);
        Controls.Add(_btnOpenConfig);
        Controls.Add(_btnHide);

        _iconOff = CreateIcon(false);
        _iconOn = CreateIcon(true);
        Icon = _iconOff;

        var trayMenu = new ContextMenuStrip();
        _trayShow = new ToolStripMenuItem(null, null, delegate { ShowWindow(); });
        _trayExit = new ToolStripMenuItem(null, null, delegate { ExitApp(); });
        trayMenu.Items.Add(_trayShow);
        trayMenu.Items.Add(_trayExit);
        _tray = new NotifyIcon();
        _tray.Text = "HDR Auto Switcher";
        _tray.Icon = _iconOff;
        _tray.ContextMenuStrip = trayMenu;
        _tray.Visible = true;
        _tray.DoubleClick += delegate { ShowWindow(); };

        _monitor.OnLog = Log;
        _monitor.OnHdrStateChanged = OnHdrStateChanged;

        string error;
        if (!_monitor.InitDisplay(out error))
        {
            Log(Lang.T("LOG_DISPLAY_FAIL", error));
        }
        else if (!_monitor.Hdr.Supported)
        {
            Log(Lang.T("WARN_UNSUPPORTED"));
        }

        ApplyLanguage();
        LoadConfig();

        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = 1000;
        _timer.Tick += delegate { _monitor.Tick(); UpdateHdrLabel(); };
        _timer.Start();
    }

    private void LanguageClicked(object sender, EventArgs e)
    {
        var item = (ToolStripMenuItem)sender;
        Lang.Set((string)item.Tag);
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = "HDR Auto Switcher";
        _menuLang.Text = Lang.T("MENU_LANG");
        foreach (ToolStripMenuItem item in _menuLang.DropDownItems)
            item.Checked = (string)item.Tag == Lang.Current;
        _grpConfig.Text = Lang.T("GRP_PROCESSES");
        _btnAdd.Text = Lang.T("BTN_ADD");
        _btnRemove.Text = Lang.T("BTN_REMOVE");
        _btnSave.Text = Lang.T("BTN_SAVE");
        _btnOpenConfig.Text = Lang.T("BTN_OPEN_CONFIG");
        _btnHide.Text = Lang.T("BTN_HIDE");
        _trayShow.Text = Lang.T("TRAY_SHOW");
        _trayExit.Text = Lang.T("TRAY_EXIT");
        UpdateHdrLabel();
    }

    private void LoadConfig()
    {
        string err = _monitor.LoadConfig();
        if (err != null)
        {
            Log(err + "。" + Lang.T("LOG_NO_NAMES_HINT"));
        }
        else
        {
            Log(Lang.T("LOG_LOADED", string.Join(", ", _monitor.Names.ToArray())));
        }
        RefreshNameList();
    }

    private void RefreshNameList()
    {
        _lstNames.Items.Clear();
        foreach (var n in _monitor.Names)
            _lstNames.Items.Add(n);
    }

    private void AddExe()
    {
        using (var dlg = new OpenFileDialog())
        {
            dlg.Title = Lang.T("DLG_ADD_TITLE");
            dlg.Filter = Lang.T("DLG_ADD_FILTER");
            dlg.Multiselect = true;
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;
            int added = 0;
            foreach (var file in dlg.FileNames)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (name.Length > 0 && !_monitor.Names.Contains(name))
                {
                    _monitor.Names.Add(name);
                    added++;
                }
            }
            RefreshNameList();
            if (added > 0)
                Log(Lang.T("LOG_ADDED", added));
        }
    }

    private void RemoveSelected()
    {
        if (_lstNames.SelectedItem == null)
            return;
        var name = (string)_lstNames.SelectedItem;
        _monitor.Names.Remove(name);
        RefreshNameList();
        Log(Lang.T("LOG_REMOVED", name));
    }

    private void OpenConfigFile()
    {
        try
        {
            HdrMonitor.EnsureConfigExists();
            // 打开所在目录并选中 ini 文件
            Process.Start("explorer.exe", "/select,\"" + HdrMonitor.ConfigPath + "\"");
        }
        catch (Exception ex)
        {
            Log(Lang.T("LOG_OPEN_FAIL", ex.Message));
        }
    }

    private void SaveConfig()
    {
        try
        {
            _monitor.SaveConfig();
            LoadConfig();
            Log(Lang.T("LOG_SAVED", HdrMonitor.ConfigPath));
        }
        catch (Exception ex)
        {
            Log(Lang.T("LOG_SAVE_FAIL", ex.Message));
        }
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
        _lblHdr.Text = Lang.T("HDR_STATUS", _monitor.CurrentHdrEnabled ? Lang.On : Lang.Off)
            + (_monitor.Hdr.Supported ? "" : Lang.T("HDR_UNSUPPORTED_SUFFIX"));
        _lblHdr.ForeColor = _monitor.CurrentHdrEnabled ? Color.DarkOrange : Color.DimGray;
    }

    private void HideToTray()
    {
        Hide();
        _tray.ShowBalloonTip(2000, "HDR Auto Switcher", Lang.T("BALLOON_HIDE"), ToolTipIcon.Info);
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
        Lang.Init();

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

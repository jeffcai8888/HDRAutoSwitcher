@echo off
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /optimize+ /target:winexe /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:HDRAutoSwitcher.exe HDRAutoSwitcher.cs
if %errorlevel%==0 (echo 编译成功: HDRAutoSwitcher.exe) else (echo 编译失败)

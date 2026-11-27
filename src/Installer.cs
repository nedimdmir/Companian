using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Companian
{
    /// <summary>
    /// Per-user install: everything goes under the user's own profile and HKEY_CURRENT_USER,
    /// which is why no administrator (UAC) prompt is ever needed.
    /// </summary>
    internal static class Installer
    {
        private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Companian";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValue = "Companian";

        public static string InstallDir
        {
            get
            {
                return Path.Combine(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"), "Companian");
            }
        }

        public static string InstalledExe
        {
            get { return Path.Combine(InstallDir, "Companian.exe"); }
        }

        private static string StartMenuShortcut
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Companian.lnk"); }
        }

        private static string DesktopShortcut
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Companian.lnk"); }
        }

        public static bool IsInstalled
        {
            get { return File.Exists(InstalledExe); }
        }

        public static bool IsRunningInstalled
        {
            get
            {
                return string.Equals(Path.GetFullPath(Application.ExecutablePath),
                    Path.GetFullPath(InstalledExe), StringComparison.OrdinalIgnoreCase);
            }
        }

        public static void Install(bool desktopShortcut)
        {
            Directory.CreateDirectory(InstallDir);
            if (!IsRunningInstalled)
                File.Copy(Application.ExecutablePath, InstalledExe, true);

            CreateShortcut(StartMenuShortcut, InstalledExe);
            if (desktopShortcut)
                CreateShortcut(DesktopShortcut, InstalledExe);
            else
                TryDeleteFile(DesktopShortcut);

            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(UninstallKey))
            {
                k.SetValue("DisplayName", AppInfo.Name);
                k.SetValue("DisplayVersion", AppInfo.Version);
                k.SetValue("Publisher", AppInfo.Publisher);
                k.SetValue("DisplayIcon", InstalledExe + ",0");
                k.SetValue("InstallLocation", InstallDir);
                k.SetValue("UninstallString", "\"" + InstalledExe + "\" --uninstall");
                k.SetValue("QuietUninstallString", "\"" + InstalledExe + "\" --uninstall --quiet");
                k.SetValue("URLInfoAbout", AppInfo.RepoUrl);
                k.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                k.SetValue("EstimatedSize", (int)Math.Max(1, new FileInfo(InstalledExe).Length / 1024), RegistryValueKind.DWord);
            }

            // Keep "start with Windows" pointing at the installed copy.
            if (IsAutostart) SetAutostart(true, InstalledExe);
        }

        /// <summary>Removes shortcuts, registry entries, settings and the installed files.</summary>
        public static void Uninstall()
        {
            SetAutostart(false, null);
            TryDeleteFile(StartMenuShortcut);
            TryDeleteFile(DesktopShortcut);
            try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey); }
            catch (ArgumentException) { }
            TryDeleteDir(Settings.Dir);

            if (!Directory.Exists(InstallDir)) return;
            if (IsRunningInstalled)
            {
                // The running exe cannot delete itself: a hidden shell retries removing the
                // folder once a second (up to a minute) until this process has exited.
                string dir = "\"" + InstallDir + "\"";
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe",
                    "/c for /l %i in (1,1,60) do @(ping 127.0.0.1 -n 2 >nul & rmdir /s /q " + dir +
                    " 2>nul & if not exist " + dir + " exit)");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(psi);
            }
            else
            {
                TryDeleteDir(InstallDir);
            }
        }

        public static bool IsAutostart
        {
            get
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey))
                    return k != null && k.GetValue(RunValue) != null;
            }
        }

        public static void SetAutostart(bool on, string exePath)
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (on) k.SetValue(RunValue, "\"" + exePath + "\" --tray");
                else k.DeleteValue(RunValue, false);
            }
        }

        private static void CreateShortcut(string lnkPath, string target)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            object lnk = null;
            try
            {
                lnk = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                Type t = lnk.GetType();
                t.InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { target });
                t.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { Path.GetDirectoryName(target) });
                t.InvokeMember("IconLocation", BindingFlags.SetProperty, null, lnk, new object[] { target + ",0" });
                t.InvokeMember("Description", BindingFlags.SetProperty, null, lnk, new object[] { AppInfo.Description });
                t.InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
            }
            finally
            {
                if (lnk != null) Marshal.FinalReleaseComObject(lnk);
                Marshal.FinalReleaseComObject(shell);
            }
        }

        private static void TryDeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void TryDeleteDir(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}

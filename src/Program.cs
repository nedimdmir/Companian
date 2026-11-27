using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Companian")]
[assembly: AssemblyDescription("Auto clicker, shutdown timer, keep-awake and reminders for Windows")]
[assembly: AssemblyCompany("nedimdmir")]
[assembly: AssemblyProduct("Companian")]
[assembly: AssemblyCopyright("Copyright © 2026 nedimdmir")]
[assembly: AssemblyVersion("1.4.0.0")]
[assembly: AssemblyFileVersion("1.4.0.0")]
[assembly: AssemblyInformationalVersion("1.4.0")]

namespace Companian
{
    internal static class AppInfo
    {
        public const string Name = "Companian";
        public const string Version = "1.4.0";
        public const string Publisher = "nedimdmir";
        public const string RepoUrl = "https://github.com/nedimdmir/Companian";
        public const string Description = "Auto clicker, shutdown timer, keep-awake and reminders for Windows";
    }

    internal static class Program
    {
        /// <summary>Broadcast by a second launch so the running instance shows its window.</summary>
        public static readonly uint ShowMessage = Native.RegisterWindowMessage("Companian.ShowWindow");
        /// <summary>Broadcast by the uninstaller so a running instance exits and releases its files.</summary>
        public static readonly uint ExitMessage = Native.RegisterWindowMessage("Companian.Exit");

        [STAThread]
        private static void Main(string[] args)
        {
            bool startInTray = HasArg(args, "--tray");
            bool uninstall = HasArg(args, "--uninstall");
            bool quiet = HasArg(args, "--quiet");
            bool elevatedTaskRun = HasArg(args, "--elevated-task-run");

            if (HasArg(args, "--setup-elevated-shortcut"))
            {
                ElevatedShortcut.Setup();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Settings settings = Settings.Load();
            string language = settings.Get("language", null);
            if (string.IsNullOrWhiteSpace(language))
            {
                language = LanguagePrompt.Choose();
                settings.Set("language", language);
                settings.Save();
            }
            Lang.Set(language);

            if (uninstall)
            {
                if (quiet || MessageBox.Show(Lang.T("st_uninstall_confirm"), AppInfo.Name,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Native.PostMessage(Native.HWND_BROADCAST, ExitMessage, IntPtr.Zero, IntPtr.Zero);
                    Thread.Sleep(1000);
                    Installer.Uninstall();
                    if (!quiet)
                        MessageBox.Show(Lang.T("st_uninstall_ok"), AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            bool created;
            Mutex mutex = new Mutex(true, elevatedTaskRun ? @"Local\Companian.ElevatedSingleInstance" : @"Local\Companian.SingleInstance", out created);
            if (!created)
            {
                bool acquired = false;
                if (HasArg(args, "--after-install"))
                {
                    // The portable copy that launched us is about to exit; wait for it.
                    try { acquired = mutex.WaitOne(8000); }
                    catch (AbandonedMutexException) { acquired = true; }
                }
                if (!acquired)
                {
                    Native.PostMessage(Native.HWND_BROADCAST, ShowMessage, IntPtr.Zero, IntPtr.Zero);
                    mutex.Dispose();
                    return;
                }
            }

            try
            {
                Application.Run(new MainForm(settings, startInTray));
            }
            finally
            {
                try { mutex.ReleaseMutex(); }
                catch (ApplicationException) { }
                mutex.Dispose();
            }
        }

        private static bool HasArg(string[] args, string name)
        {
            foreach (string a in args)
                if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}

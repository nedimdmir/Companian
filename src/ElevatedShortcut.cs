using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;

namespace Companian
{
    /// <summary>Creates a per-user, on-demand elevated task and its desktop launcher.</summary>
    internal static class ElevatedShortcut
    {
        private const string TaskName = "OtomasyonAraci";

        public static void Request()
        {
            try
            {
                if (IsAdministrator())
                {
                    Setup();
                    return;
                }
                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = "--setup-elevated-shortcut",
                    Verb = "runas",
                    UseShellExecute = true
                };
                Process.Start(start);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                if (ex.NativeErrorCode != 1223) MessageBox.Show(Lang.F("uac_setup_fail", ex.Message), AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.F("uac_setup_fail", ex.Message), AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void Setup()
        {
            try
            {
                if (!IsAdministrator()) throw new InvalidOperationException(Lang.T("uac_setup_not_admin"));

                string exe = Application.ExecutablePath;
                string user = Environment.UserDomainName + "\\" + Environment.UserName;
                DateTime scheduleDate = DateTime.Today.AddDays(1);
                string date = scheduleDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                string taskCommand = Quote(exe) + " --elevated-task-run";
                string taskArgs = "/create /f /tn " + Quote(TaskName) + " /tr " + Quote(taskCommand) +
                    " /sc ONCE /sd " + date + " /st 00:00 /ru " + Quote(user) + " /it /rl HIGHEST";
                RunSchtasks(taskArgs);

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcut = Path.Combine(desktop, "OtomasyonAraci.lnk");
                string script = "Set s = CreateObject(\"WScript.Shell\")\r\n" +
                    "Set l = s.CreateShortcut(" + VbsString(shortcut) + ")\r\n" +
                    "l.TargetPath = " + VbsString(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe")) + "\r\n" +
                    "l.Arguments = \"/run /tn " + TaskName + "\"\r\n" +
                    "l.WorkingDirectory = " + VbsString(Path.GetDirectoryName(exe)) + "\r\n" +
                    "l.Description = " + VbsString(AppInfo.Name + " — elevated task") + "\r\n" +
                    "l.Save\r\n";
                string temp = Path.Combine(Path.GetTempPath(), "CompanianShortcut-" + Guid.NewGuid().ToString("N") + ".vbs");
                try
                {
                    File.WriteAllText(temp, script, System.Text.Encoding.Unicode);
                    ProcessStartInfo wscript = new ProcessStartInfo("wscript.exe", "//B //Nologo " + Quote(temp))
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (Process p = Process.Start(wscript))
                    {
                        if (!p.WaitForExit(10000) || p.ExitCode != 0)
                            throw new InvalidOperationException("Windows could not create the desktop shortcut.");
                    }
                    if (!File.Exists(shortcut)) throw new InvalidOperationException("Windows did not create the desktop shortcut.");
                }
                finally { try { File.Delete(temp); } catch { } }

                MessageBox.Show(Lang.T("uac_setup_ok"), AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.F("uac_setup_fail", ex.Message), AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RunSchtasks(string args)
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe"),
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process p = Process.Start(info))
            {
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0) throw new InvalidOperationException((error + " " + output).Trim());
            }
        }

        private static string Quote(string value)
        {
            value = value ?? "";
            System.Text.StringBuilder result = new System.Text.StringBuilder("\"");
            int slashes = 0;
            foreach (char c in value)
            {
                if (c == '\\') { slashes++; continue; }
                if (c == '"') result.Append('\\', slashes * 2 + 1);
                else result.Append('\\', slashes);
                result.Append(c);
                slashes = 0;
            }
            result.Append('\\', slashes * 2).Append('"');
            return result.ToString();
        }

        private static string VbsString(string value)
        {
            return "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        }
    }
}

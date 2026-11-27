using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace Companian
{
    internal enum SetupStatus
    {
        None, Waiting, Running, Installed, InstalledRestart, AlreadyInstalled,
        NeedsAdmin, NotFound, FileMissing, InUse, NoNetwork, Failed, TimedOut, Skipped
    }

    /// <summary>One program in the bulk-install list: either a winget package or a local installer file.</summary>
    internal sealed class SetupItem
    {
        public string Name;
        /// <summary>winget package id (for example Google.Chrome); null for installer files.</summary>
        public string WingetId;
        /// <summary>Path of a local .exe / .msi / .msix installer; null for winget packages.</summary>
        public string FilePath;
        /// <summary>Command-line arguments for installer files (detected automatically, can be edited).</summary>
        public string Args;
        public bool Builtin;
        public bool Checked;
        public SetupStatus Status;
        public string Detail;

        public bool IsFile
        {
            get { return FilePath != null; }
        }

        public string Source
        {
            get { return IsFile ? Path.GetFileName(FilePath) : WingetId; }
        }

        public bool Done
        {
            get
            {
                return Status == SetupStatus.Installed || Status == SetupStatus.InstalledRestart ||
                       Status == SetupStatus.AlreadyInstalled;
            }
        }
    }

    internal static class SetupCatalog
    {
        /// <summary>Popular programs (name, winget id). Every id was checked against the winget source.</summary>
        public static readonly string[,] Programs =
        {
            { "Google Chrome", "Google.Chrome" },
            { "Mozilla Firefox", "Mozilla.Firefox" },
            { "Brave", "Brave.Brave" },
            { "Opera", "Opera.Opera" },
            { "VLC media player", "VideoLAN.VLC" },
            { "WinRAR", "RARLab.WinRAR" },
            { "7-Zip", "7zip.7zip" },
            { "Notepad++", "Notepad++.Notepad++" },
            { "Visual Studio Code", "Microsoft.VisualStudioCode" },
            { "Discord", "Discord.Discord" },
            { "Spotify", "Spotify.Spotify" },
            { "Steam", "Valve.Steam" },
            { "Zoom", "Zoom.Zoom" },
            { "Telegram", "Telegram.TelegramDesktop" },
            { "Microsoft Teams", "Microsoft.Teams" },
            { "Adobe Acrobat Reader", "Adobe.Acrobat.Reader.64-bit" },
            { "Foxit PDF Reader", "Foxit.FoxitReader" },
            { "SumatraPDF", "SumatraPDF.SumatraPDF" },
            { "LibreOffice", "TheDocumentFoundation.LibreOffice" },
            { "OBS Studio", "OBSProject.OBSStudio" },
            { "Audacity", "Audacity.Audacity" },
            { "HandBrake", "HandBrake.HandBrake" },
            { "GIMP", "GIMP.GIMP.3" },
            { "Krita", "KDE.Krita" },
            { "Paint.NET", "dotPDN.PaintDotNet" },
            { "ShareX", "ShareX.ShareX" },
            { "Greenshot", "Greenshot.Greenshot" },
            { "qBittorrent", "qBittorrent.qBittorrent" },
            { "Everything", "voidtools.Everything" },
            { "PowerToys", "Microsoft.PowerToys" },
            { "Windows Terminal", "Microsoft.WindowsTerminal" },
            { "AnyDesk", "AnyDesk.AnyDesk" },
            { "TeamViewer", "TeamViewer.TeamViewer" },
            { "Malwarebytes", "Malwarebytes.Malwarebytes" },
            { "CPU-Z", "CPUID.CPU-Z" },
            { "Rufus", "Rufus.Rufus" },
            { "Git", "Git.Git" },
            { "Python 3.13", "Python.Python.3.13" },
            { "Node.js LTS", "OpenJS.NodeJS.LTS" },
            { "Postman", "Postman.Postman" },
        };

        public static List<SetupItem> Create()
        {
            List<SetupItem> list = new List<SetupItem>();
            for (int i = 0; i < Programs.GetLength(0); i++)
                list.Add(new SetupItem { Name = Programs[i, 0], WingetId = Programs[i, 1], Builtin = true });
            return list;
        }

        /// <summary>Creates a list entry for a local installer file, with silent arguments when the installer type is known.</summary>
        public static SetupItem FromFile(string path, bool userOnly)
        {
            return new SetupItem
            {
                Name = Path.GetFileNameWithoutExtension(path),
                FilePath = path,
                Args = DetectArgs(path, userOnly),
                Checked = true
            };
        }

        /// <summary>
        /// Guesses silent-install arguments: MSI (msiexec, per-user), Inno Setup and NSIS are detected.
        /// Unknown .exe installers get no arguments and simply open their own window.
        /// </summary>
        public static string DetectArgs(string path, bool userOnly)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".msi")
                return userOnly ? "/qn /norestart ALLUSERS=2 MSIINSTALLPERUSER=1" : "/qn /norestart";
            if (ext != ".exe") return "";

            string kind = SniffExe(path);
            if (kind == "inno")
                return "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-" + (userOnly ? " /CURRENTUSER" : "");
            if (kind == "nsis")
                return "/S" + (userOnly ? " /CURRENTUSER" : "");
            return "";
        }

        private static string SniffExe(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] buf = new byte[Math.Min(fs.Length, 4 * 1024 * 1024)];
                    int n = 0, r;
                    while (n < buf.Length && (r = fs.Read(buf, n, buf.Length - n)) > 0) n += r;
                    string text = Encoding.ASCII.GetString(buf, 0, n);
                    if (text.IndexOf("Inno Setup", StringComparison.Ordinal) >= 0) return "inno";
                    if (text.IndexOf("Nullsoft", StringComparison.Ordinal) >= 0 ||
                        text.IndexOf("NSIS Error", StringComparison.Ordinal) >= 0) return "nsis";
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return null;
        }
    }

    /// <summary>
    /// Saves/loads the install list. File format:
    /// {"app":"Companian","type":"setup-list","version":1,"items":[
    ///   {"name":"Google Chrome","winget":"Google.Chrome","checked":true},
    ///   {"name":"MyApp","file":"D:\\Setups\\myapp.exe","args":"/S","checked":true}]}
    /// </summary>
    internal static class SetupList
    {
        public static string ToJson(IList<SetupItem> items, bool pretty)
        {
            string nl = pretty ? "\r\n" : "";
            string ind = pretty ? "    " : "";
            StringBuilder sb = new StringBuilder();
            sb.Append('{').Append(nl);
            sb.Append(pretty ? "  " : "").Append("\"app\": \"Companian\", \"type\": \"setup-list\", \"version\": 1,").Append(nl);
            sb.Append(pretty ? "  " : "").Append("\"items\": [");
            bool first = true;
            foreach (SetupItem it in items)
            {
                // Built-in programs are only worth saving when they are ticked.
                if (it.Builtin && !it.Checked) continue;
                sb.Append(first ? nl : "," + nl).Append(ind);
                first = false;
                sb.Append("{ \"name\": ").Append(Str(it.Name));
                if (it.IsFile)
                {
                    sb.Append(", \"file\": ").Append(Str(it.FilePath));
                    sb.Append(", \"args\": ").Append(Str(it.Args ?? ""));
                }
                else
                {
                    sb.Append(", \"winget\": ").Append(Str(it.WingetId));
                }
                sb.Append(", \"checked\": ").Append(it.Checked ? "true" : "false").Append(" }");
            }
            sb.Append(first ? "]" : nl + (pretty ? "  " : "") + "]").Append(nl);
            sb.Append('}').Append(nl);
            return sb.ToString();
        }

        /// <summary>Parses a saved list. Throws <see cref="FormatException"/> with a translated message when invalid.</summary>
        public static List<SetupItem> FromJson(string json)
        {
            object root;
            try { root = new JavaScriptSerializer().DeserializeObject(json); }
            catch (ArgumentException) { throw new FormatException(Lang.T("seq_err_json")); }
            catch (InvalidOperationException) { throw new FormatException(Lang.T("seq_err_json")); }

            IDictionary<string, object> obj = root as IDictionary<string, object>;
            object raw = null;
            if (obj == null || !obj.TryGetValue("items", out raw) || !(raw is IList) || raw is string)
                throw new FormatException(Lang.T("su_err_noitems"));

            List<SetupItem> items = new List<SetupItem>();
            IList list = (IList)raw;
            for (int i = 0; i < list.Count; i++)
            {
                IDictionary<string, object> e = list[i] as IDictionary<string, object>;
                string winget = e == null ? null : Text(e, "winget");
                string file = e == null ? null : Text(e, "file");
                if ((winget == null) == (file == null)) throw new FormatException(Lang.F("su_err_item", i + 1));
                object c;
                SetupItem it = new SetupItem
                {
                    WingetId = winget,
                    FilePath = file,
                    Args = file != null ? (Text(e, "args") ?? "") : null,
                    Name = Text(e, "name") ?? (winget ?? Path.GetFileNameWithoutExtension(file)),
                    Checked = !e.TryGetValue("checked", out c) || !(c is bool) || (bool)c
                };
                items.Add(it);
            }
            return items;
        }

        /// <summary>Merges loaded entries into the list: built-in programs are ticked, everything else is appended.</summary>
        public static void Merge(List<SetupItem> target, IEnumerable<SetupItem> loaded)
        {
            foreach (SetupItem it in loaded)
            {
                SetupItem existing = target.Find(delegate(SetupItem t)
                {
                    return it.IsFile
                        ? t.IsFile && string.Equals(t.FilePath, it.FilePath, StringComparison.OrdinalIgnoreCase)
                        : !t.IsFile && string.Equals(t.WingetId, it.WingetId, StringComparison.OrdinalIgnoreCase);
                });
                if (existing != null)
                {
                    existing.Checked = it.Checked;
                    if (it.IsFile) existing.Args = it.Args;
                }
                else
                {
                    target.Add(it);
                }
            }
        }

        private static string Text(IDictionary<string, object> e, string key)
        {
            object o;
            string s = e.TryGetValue(key, out o) ? o as string : null;
            return string.IsNullOrEmpty(s) ? null : s.Trim();
        }

        private static string Str(string s)
        {
            StringBuilder sb = new StringBuilder("\"");
            foreach (char ch in s ?? "")
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:x4}", (int)ch);
                        else sb.Append(ch);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }

    /// <summary>A winget search hit.</summary>
    internal sealed class WingetHit
    {
        public string Name, Id, Version;
    }

    /// <summary>Installs the ticked programs one after another on a background thread.</summary>
    internal sealed class SetupRunner
    {
        // winget return codes (see "winget error <code>")
        private const int NoApplicableInstaller = unchecked((int)0x8A150010);
        private const int NoPackagesFound = unchecked((int)0x8A150014);
        private const int UpdateNotApplicable = unchecked((int)0x8A15002B);
        private const int AlreadyInstalled = unchecked((int)0x8A150061);
        private const int RebootToFinish = unchecked((int)0x8A150109);
        private const int PackageInUse = unchecked((int)0x8A150101);
        private const int NoNetwork = unchecked((int)0x8A150107);

        private const int ElevationRequired = 740;   // ERROR_ELEVATION_REQUIRED
        private const int AccessDenied = 5;
        private const int MsiNeedsAdmin = 1925;      // ERROR_INSTALL_PERMISSION
        private const int MsiRestart = 3010;
        private const int MsiRestartStarted = 1641;

        private const int SilentTimeoutMs = 30 * 60 * 1000;

        private volatile bool stopRequested;
        private Thread worker;

        /// <summary>Raised when an item's Status or Detail changes.</summary>
        public event Action<SetupItem> ItemChanged;
        public event Action<string> Log;
        public event Action Finished;

        public bool IsRunning
        {
            get { Thread t = worker; return t != null && t.IsAlive; }
        }

        public bool StopRequested
        {
            get { return stopRequested; }
        }

        public void Start(List<SetupItem> items, bool userOnly)
        {
            if (IsRunning) return;
            stopRequested = false;
            foreach (SetupItem it in items)
            {
                it.Status = SetupStatus.Waiting;
                it.Detail = null;
            }
            worker = new Thread(delegate() { Run(items, userOnly); });
            worker.IsBackground = true;
            worker.Name = "Companian setup";
            worker.Start();
        }

        /// <summary>Stops after the program that is being installed right now.</summary>
        public void Stop()
        {
            stopRequested = true;
        }

        public static bool WingetAvailable()
        {
            try
            {
                using (Process p = Process.Start(Hidden("winget", "--version")))
                {
                    p.StandardOutput.ReadToEnd();
                    p.WaitForExit(15000);
                    return p.HasExited && p.ExitCode == 0;
                }
            }
            catch (Win32Exception) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        /// <summary>Runs "winget search" and parses its table. Blocking; call from a worker thread.</summary>
        public static List<WingetHit> Search(string query)
        {
            List<WingetHit> hits = new List<WingetHit>();
            ProcessStartInfo psi = Hidden("winget",
                "search " + Quote(query) + " --source winget --accept-source-agreements --disable-interactivity");
            string output;
            using (Process p = Process.Start(psi))
            {
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }

            string[] lines = output.Replace("\r\n", "\n").Split('\n');
            int header = -1;
            for (int i = 0; i + 1 < lines.Length; i++)
            {
                string next = lines[i + 1].Trim();
                // The header text is localized, so find it by the dashed line under it.
                if (next.Length > 10 && next.Trim('-').Length == 0 && lines[i].Trim().Length > 0)
                {
                    header = i;
                    break;
                }
            }
            if (header < 0) return hits;

            // Columns are aligned with spaces; take their start positions from the header line.
            string h = LastSegment(lines[header]);
            List<int> starts = new List<int> { 0 };
            for (int i = 1; i < h.Length; i++)
                if (h[i] != ' ' && h[i - 1] == ' ') starts.Add(i);
            if (starts.Count < 3) return hits;

            for (int i = header + 2; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                if (line.Length <= starts[1]) continue;
                string name = Cut(line, starts[0], starts[1]);
                string id = Cut(line, starts[1], starts[2]);
                string version = Cut(line, starts[2], starts.Count > 3 ? starts[3] : line.Length);
                if (id.Length == 0 || id.IndexOf(' ') >= 0) continue;
                hits.Add(new WingetHit { Name = name, Id = id, Version = version });
            }
            return hits;
        }

        private static string LastSegment(string line)
        {
            int cr = line.LastIndexOf('\r');
            return cr >= 0 ? line.Substring(cr + 1) : line;
        }

        private static string Cut(string line, int from, int to)
        {
            if (from >= line.Length) return "";
            return line.Substring(from, Math.Min(to, line.Length) - from).Trim();
        }

        private void Run(List<SetupItem> items, bool userOnly)
        {
            try
            {
                foreach (SetupItem it in items)
                {
                    if (stopRequested)
                    {
                        Set(it, SetupStatus.Skipped, null);
                        continue;
                    }
                    Set(it, SetupStatus.Running, null);
                    try
                    {
                        if (it.IsFile) InstallFile(it, userOnly);
                        else InstallWinget(it, userOnly);
                    }
                    catch (Exception ex)
                    {
                        if (!(ex is Win32Exception || ex is IOException || ex is InvalidOperationException ||
                              ex is UnauthorizedAccessException)) throw;
                        Set(it, SetupStatus.Failed, ex.Message);
                        Write("  ! " + ex.Message);
                    }
                }
            }
            finally
            {
                Action h = Finished;
                if (h != null) h();
            }
        }

        private void InstallWinget(SetupItem it, bool userOnly)
        {
            string args = "install --id " + Quote(it.WingetId) + " --exact --source winget --silent" +
                          " --accept-package-agreements --accept-source-agreements --disable-interactivity" +
                          (userOnly ? " --scope user" : "");
            Write("> winget " + args);

            ProcessStartInfo psi = Hidden("winget", args);
            if (userOnly) psi.EnvironmentVariables["__COMPAT_LAYER"] = "RunAsInvoker";
            int code = RunCaptured(it, psi, SilentTimeoutMs);
            if (code == int.MinValue)
            {
                Set(it, SetupStatus.TimedOut, null);
                return;
            }

            SetupStatus status;
            switch (code)
            {
                case 0: status = SetupStatus.Installed; break;
                case RebootToFinish: status = SetupStatus.InstalledRestart; break;
                case UpdateNotApplicable:
                case AlreadyInstalled: status = SetupStatus.AlreadyInstalled; break;
                case NoApplicableInstaller: status = userOnly ? SetupStatus.NeedsAdmin : SetupStatus.Failed; break;
                case NoPackagesFound: status = SetupStatus.NotFound; break;
                case PackageInUse: status = SetupStatus.InUse; break;
                case NoNetwork: status = SetupStatus.NoNetwork; break;
                case ElevationRequired:
                case AccessDenied: status = SetupStatus.NeedsAdmin; break;
                default:
                    status = SetupStatus.Failed;
                    break;
            }
            // Installers launched without elevation often fail with "access denied" style codes.
            string last = it.Detail;
            if (status == SetupStatus.Failed && userOnly && last != null &&
                (last.EndsWith(" 1925", StringComparison.Ordinal) || last.EndsWith(" 740", StringComparison.Ordinal) ||
                 last.EndsWith(" 5", StringComparison.Ordinal)))
                status = SetupStatus.NeedsAdmin;
            Set(it, status, status == SetupStatus.Failed ? (last != null ? last + " · " : "") + Hex(code) : null);
        }

        private void InstallFile(SetupItem it, bool userOnly)
        {
            if (!File.Exists(it.FilePath))
            {
                Set(it, SetupStatus.FileMissing, null);
                Write("  ! " + it.FilePath);
                return;
            }

            string ext = Path.GetExtension(it.FilePath).ToLowerInvariant();
            string exe, args;
            if (ext == ".msi")
            {
                exe = Path.Combine(Environment.SystemDirectory, "msiexec.exe");
                args = "/i " + Quote(it.FilePath) + " " + (it.Args ?? "");
            }
            else if (ext == ".msix" || ext == ".msixbundle" || ext == ".appx" || ext == ".appxbundle")
            {
                // App packages always install for the current user only.
                exe = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
                args = "-NoProfile -NonInteractive -Command \"Add-AppxPackage -Path '" + it.FilePath.Replace("'", "''") + "'\"";
            }
            else
            {
                exe = it.FilePath;
                args = it.Args ?? "";
            }
            Write("> " + Quote(exe) + " " + args);

            bool silent = ext != ".exe" || !string.IsNullOrEmpty(it.Args);
            int code;
            if (userOnly || ext != ".exe")
            {
                ProcessStartInfo psi = Hidden(exe, args);
                psi.WorkingDirectory = Path.GetDirectoryName(it.FilePath);
                // Run "requireAdministrator" installers as the normal user instead of asking for UAC.
                if (userOnly) psi.EnvironmentVariables["__COMPAT_LAYER"] = "RunAsInvoker";
                if (!silent) psi.CreateNoWindow = false;
                code = RunCaptured(it, psi, silent ? SilentTimeoutMs : -1);
            }
            else
            {
                // "Only for me" is off: let Windows show UAC if the installer asks for it.
                ProcessStartInfo psi = new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(it.FilePath)
                };
                using (Process p = Process.Start(psi))
                {
                    if (!WaitInterruptible(p, silent ? SilentTimeoutMs : -1))
                    {
                        Set(it, SetupStatus.TimedOut, null);
                        return;
                    }
                    code = p.ExitCode;
                }
            }

            if (code == int.MinValue) { Set(it, SetupStatus.TimedOut, null); return; }
            SetupStatus status;
            switch (code)
            {
                case 0: status = SetupStatus.Installed; break;
                case MsiRestart:
                case MsiRestartStarted: status = SetupStatus.InstalledRestart; break;
                case ElevationRequired:
                case AccessDenied:
                case MsiNeedsAdmin: status = SetupStatus.NeedsAdmin; break;
                default: status = SetupStatus.Failed; break;
            }
            Set(it, status, status == SetupStatus.Failed ? Hex(code) : null);
        }

        /// <summary>Runs a process, streaming its output to the log. Returns the exit code, or int.MinValue on timeout.</summary>
        private int RunCaptured(SetupItem it, ProcessStartInfo psi, int timeoutMs)
        {
            using (Process p = new Process { StartInfo = psi })
            {
                p.Start();
                Thread reader = null;
                if (psi.RedirectStandardOutput)
                {
                    reader = new Thread(delegate() { Pump(it, p.StandardOutput); }) { IsBackground = true };
                    reader.Start();
                }
                bool exited = WaitInterruptible(p, timeoutMs);
                if (!exited)
                {
                    try { p.Kill(); } catch (InvalidOperationException) { } catch (Win32Exception) { }
                    Write("  ! timeout");
                    return int.MinValue;
                }
                if (reader != null) reader.Join(3000);
                Write("  = " + Hex(p.ExitCode));
                return p.ExitCode;
            }
        }

        private static bool WaitInterruptible(Process p, int timeoutMs)
        {
            return timeoutMs < 0 ? WaitForever(p) : p.WaitForExit(timeoutMs);
        }

        private static bool WaitForever(Process p)
        {
            p.WaitForExit();
            return true;
        }

        /// <summary>Reads winget output. Progress bars redraw with '\r', so both '\r' and '\n' end a line.</summary>
        private void Pump(SetupItem it, StreamReader r)
        {
            StringBuilder line = new StringBuilder();
            char[] buf = new char[512];
            long lastDetail = 0;
            Stopwatch sw = Stopwatch.StartNew();
            int n;
            try
            {
                while ((n = r.Read(buf, 0, buf.Length)) > 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        char ch = buf[i];
                        if (ch != '\r' && ch != '\n') { line.Append(ch); continue; }
                        string text = line.ToString().Trim();
                        line.Length = 0;
                        if (text.Length <= 2) continue; // spinner frames
                        bool progress = text.IndexOf('█') >= 0 || text.IndexOf('▒') >= 0;
                        if (!progress) Write("  " + text);
                        it.Detail = CleanProgress(text);
                        if (sw.ElapsedMilliseconds - lastDetail > 150)
                        {
                            lastDetail = sw.ElapsedMilliseconds;
                            Raise(it);
                        }
                    }
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
        }

        private static string CleanProgress(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in text)
                if (ch != '█' && ch != '▒') sb.Append(ch);
            string s = sb.ToString().Trim();
            return s.Length > 90 ? s.Substring(0, 90) + "…" : s;
        }

        private static ProcessStartInfo Hidden(string exe, string args)
        {
            return new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8
            };
        }

        private static string Quote(string s)
        {
            return "\"" + (s ?? "").Replace("\"", "") + "\"";
        }

        private static string Hex(int code)
        {
            return code == 0 ? "0" : "0x" + code.ToString("X8", CultureInfo.InvariantCulture) + " (" + code.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private void Set(SetupItem it, SetupStatus status, string detail)
        {
            it.Status = status;
            it.Detail = detail;
            Raise(it);
        }

        private void Raise(SetupItem it)
        {
            Action<SetupItem> h = ItemChanged;
            if (h != null) h(it);
        }

        private void Write(string text)
        {
            Action<string> h = Log;
            if (h != null) h(text);
        }
    }
}

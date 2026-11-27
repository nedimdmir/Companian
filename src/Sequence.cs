using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace Companian
{
    /// <summary>One entry of a click sequence: where to click, how, and how long to wait afterwards.</summary>
    internal sealed class ClickStep
    {
        public const int MaxDelayMs = 24 * 60 * 60 * 1000;

        public int X;
        public int Y;
        public ClickButton Button;
        public bool DoubleClick;
        /// <summary>Time to wait after this step's click, before the next step.</summary>
        public int DelayMs;

        public ClickStep Clone()
        {
            return (ClickStep)MemberwiseClone();
        }
    }

    /// <summary>
    /// Saves and loads click sequences. Files are JSON:
    /// {"app":"Companian","type":"click-sequence","version":1,
    ///  "steps":[{"x":500,"y":300,"button":"left","clicks":1,"delay_ms":250}, ...]}
    /// </summary>
    internal static class Sequence
    {
        public const int FileVersion = 1;
        private static readonly string[] ButtonNames = { "left", "right", "middle" };

        // ------------------------------------------------------------------ JSON files

        public static void SaveFile(string path, IList<ClickStep> steps)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\r\n");
            sb.Append("  \"app\": \"Companian\",\r\n");
            sb.Append("  \"type\": \"click-sequence\",\r\n");
            sb.Append("  \"version\": ").Append(FileVersion).Append(",\r\n");
            sb.Append("  \"steps\": [");
            for (int i = 0; i < steps.Count; i++)
            {
                ClickStep s = steps[i];
                sb.Append(i == 0 ? "\r\n" : ",\r\n");
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "    {{ \"x\": {0}, \"y\": {1}, \"button\": \"{2}\", \"clicks\": {3}, \"delay_ms\": {4} }}",
                    s.X, s.Y, ButtonNames[(int)s.Button], s.DoubleClick ? 2 : 1, s.DelayMs);
            }
            sb.Append(steps.Count > 0 ? "\r\n  ]\r\n" : "]\r\n");
            sb.Append("}\r\n");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        /// <summary>Reads a sequence file. Throws <see cref="FormatException"/> with a translated message if it is invalid.</summary>
        public static List<ClickStep> LoadFile(string path)
        {
            string text = File.ReadAllText(path, Encoding.UTF8);
            object root;
            try
            {
                root = new JavaScriptSerializer().DeserializeObject(text);
            }
            catch (ArgumentException)
            {
                throw new FormatException(Lang.T("seq_err_json"));
            }
            catch (InvalidOperationException)
            {
                throw new FormatException(Lang.T("seq_err_json"));
            }

            // Accept both {"steps":[...]} and a bare [...] list.
            object rawSteps = root;
            IDictionary<string, object> obj = root as IDictionary<string, object>;
            if (obj != null && !obj.TryGetValue("steps", out rawSteps)) rawSteps = null;
            IList list = rawSteps as IList;
            if (list == null || rawSteps is string) throw new FormatException(Lang.T("seq_err_nosteps"));

            List<ClickStep> steps = new List<ClickStep>();
            for (int i = 0; i < list.Count; i++)
            {
                IDictionary<string, object> raw = list[i] as IDictionary<string, object>;
                ClickStep s = raw == null ? null : ParseStep(raw);
                if (s == null) throw new FormatException(Lang.F("seq_err_step", i + 1));
                steps.Add(s);
            }
            return steps;
        }

        private static ClickStep ParseStep(IDictionary<string, object> raw)
        {
            int x, y, clicks = 1, delay = 500;
            if (!TryInt(raw, "x", out x) || !TryInt(raw, "y", out y)) return null;
            if (raw.ContainsKey("clicks") && !TryInt(raw, "clicks", out clicks)) return null;
            if (raw.ContainsKey("delay_ms") && !TryInt(raw, "delay_ms", out delay)) return null;
            if (clicks != 1 && clicks != 2) return null;
            if (delay < 0 || delay > ClickStep.MaxDelayMs) return null;

            int button = 0;
            object b;
            if (raw.TryGetValue("button", out b) && b != null)
            {
                button = Array.IndexOf(ButtonNames, Convert.ToString(b, CultureInfo.InvariantCulture).ToLowerInvariant());
                if (button < 0) return null;
            }
            return new ClickStep { X = x, Y = y, Button = (ClickButton)button, DoubleClick = clicks == 2, DelayMs = delay };
        }

        private static bool TryInt(IDictionary<string, object> raw, string key, out int value)
        {
            value = 0;
            object o;
            if (!raw.TryGetValue(key, out o) || o == null || o is string || o is bool) return false;
            try
            {
                decimal d = Convert.ToDecimal(o, CultureInfo.InvariantCulture);
                if (d != Math.Truncate(d) || d < int.MinValue || d > int.MaxValue) return false;
                value = (int)d;
                return true;
            }
            catch (FormatException) { return false; }
            catch (InvalidCastException) { return false; }
            catch (OverflowException) { return false; }
        }

        // ------------------------------------------------------------------ settings.ini

        /// <summary>Compact one-line form for settings.ini: "x,y,button,clicks,delay;..."</summary>
        public static string ToSettings(IList<ClickStep> steps)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ClickStep s in steps)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4}",
                    s.X, s.Y, (int)s.Button, s.DoubleClick ? 2 : 1, s.DelayMs);
            }
            return sb.ToString();
        }

        public static List<ClickStep> FromSettings(string value)
        {
            List<ClickStep> steps = new List<ClickStep>();
            if (string.IsNullOrEmpty(value)) return steps;
            foreach (string part in value.Split(';'))
            {
                string[] f = part.Split(',');
                if (f.Length != 5) continue;
                int[] n = new int[5];
                bool ok = true;
                for (int i = 0; i < 5 && ok; i++)
                    ok = int.TryParse(f[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out n[i]);
                if (!ok || n[2] < 0 || n[2] > 2 || (n[3] != 1 && n[3] != 2) || n[4] < 0 || n[4] > ClickStep.MaxDelayMs) continue;
                steps.Add(new ClickStep { X = n[0], Y = n[1], Button = (ClickButton)n[2], DoubleClick = n[3] == 2, DelayMs = n[4] });
            }
            return steps;
        }
    }
}

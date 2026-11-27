using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Companian
{
    /// <summary>Simple key=value settings stored in %APPDATA%\Companian\settings.ini.</summary>
    internal sealed class Settings
    {
        private readonly Dictionary<string, string> values =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string Dir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Companian"); }
        }

        private static string FilePath
        {
            get { return Path.Combine(Dir, "settings.ini"); }
        }

        public static Settings Load()
        {
            Settings s = new Settings();
            try
            {
                if (File.Exists(FilePath))
                {
                    foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
                    {
                        int i = line.IndexOf('=');
                        if (i > 0) s.values[line.Substring(0, i).Trim()] = line.Substring(i + 1);
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return s;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, string> kv in values)
                    sb.Append(kv.Key).Append('=').Append(kv.Value.Replace("\r", " ").Replace("\n", " ")).Append("\r\n");
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public string Get(string key, string def)
        {
            string v;
            return values.TryGetValue(key, out v) ? v : def;
        }

        public int GetInt(string key, int def)
        {
            int v;
            return int.TryParse(Get(key, null), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : def;
        }

        public bool GetBool(string key, bool def)
        {
            string v = Get(key, null);
            return v == null ? def : v == "1";
        }

        public void Set(string key, string value)
        {
            values[key] = value ?? "";
        }

        public void Set(string key, int value)
        {
            values[key] = value.ToString(CultureInfo.InvariantCulture);
        }

        public void Set(string key, decimal value)
        {
            values[key] = ((int)value).ToString(CultureInfo.InvariantCulture);
        }

        public void Set(string key, bool value)
        {
            values[key] = value ? "1" : "0";
        }
    }
}

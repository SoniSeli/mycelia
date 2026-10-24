using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// Appends each event as one JSON line to <c>events.jsonl</c>, for pulling off a device after a playtest; nothing
    /// leaves the phone. When the file reaches its cap it moves to <c>events.old.jsonl</c> (replacing the older one)
    /// and a new file starts. Development builds also echo every line to the log (logcat on Android).
    /// </summary>
    public sealed class FileAnalytics : IAnalytics
    {
        public const string FileName = "events.jsonl";
        public const string OldFileName = "events.old.jsonl";
        public const long DefaultCapBytes = 256 * 1024;

        readonly string directory;
        readonly string path;
        readonly string oldPath;
        readonly string session;
        readonly Func<DateTime> utcNow;
        readonly Action<string> echo;
        readonly long capBytes;

        /// <param name="session">A random id for this launch, so one session's events can be told apart.</param>
        /// <param name="echo">Receives each line (prefixed); null to echo nothing.</param>
        public FileAnalytics(string directory, string session, Func<DateTime> utcNow, Action<string> echo, long capBytes = DefaultCapBytes)
        {
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            path = Path.Combine(directory, FileName);
            oldPath = Path.Combine(directory, OldFileName);
            this.session = session;
            this.utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            this.echo = echo;
            this.capBytes = capBytes;
        }

        public void Log(AnalyticsEvent analyticsEvent)
        {
            string line = ToJson(analyticsEvent);
            echo?.Invoke("[Analytics] " + line);
            try
            {
                Directory.CreateDirectory(directory);
                byte[] bytes = Encoding.UTF8.GetBytes(line + "\n");
                if (File.Exists(path) && new FileInfo(path).Length + bytes.Length > capBytes)
                {
                    if (File.Exists(oldPath)) File.Delete(oldPath);
                    File.Move(path, oldPath);
                }
                using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                    stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Mycelia] An analytics event could not be written: {e.Message}");
            }
        }

        string ToJson(AnalyticsEvent analyticsEvent)
        {
            var json = new StringBuilder(128);
            json.Append("{\"t\":");
            AppendString(json, utcNow().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            json.Append(",\"session\":");
            AppendString(json, session);
            json.Append(",\"event\":");
            AppendString(json, analyticsEvent.Name);
            foreach (KeyValuePair<string, object> field in analyticsEvent.Fields)
            {
                json.Append(',');
                AppendString(json, field.Key);
                json.Append(':');
                AppendValue(json, field.Value);
            }
            return json.Append('}').ToString();
        }

        static void AppendValue(StringBuilder json, object value)
        {
            switch (value)
            {
                case null: json.Append("null"); break;
                case bool flag: json.Append(flag ? "true" : "false"); break;
                case int number: json.Append(number.ToString(CultureInfo.InvariantCulture)); break;
                case long number: json.Append(number.ToString(CultureInfo.InvariantCulture)); break;
                default: AppendString(json, Convert.ToString(value, CultureInfo.InvariantCulture)); break;
            }
        }

        static void AppendString(StringBuilder json, string text)
        {
            json.Append('"');
            foreach (char c in text ?? "")
            {
                switch (c)
                {
                    case '"': json.Append("\\\""); break;
                    case '\\': json.Append("\\\\"); break;
                    case '\n': json.Append("\\n"); break;
                    case '\r': json.Append("\\r"); break;
                    case '\t': json.Append("\\t"); break;
                    default:
                        if (c < ' ') json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else json.Append(c);
                        break;
                }
            }
            json.Append('"');
        }
    }
}

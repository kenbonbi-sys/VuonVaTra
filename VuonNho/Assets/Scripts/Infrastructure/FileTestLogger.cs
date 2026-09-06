using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VuonNho.Core;

namespace VuonNho.Infrastructure
{
    /// <summary>
    /// Log local mot dong mot event. Khong gui du lieu ra ngoai, khong thu ten hay email.
    /// Nguoi thu bam "Xuat du lieu test" de tu gui file nay.
    /// </summary>
    public sealed class FileTestLogger : ITestLogger
    {
        readonly string _path;
        readonly string _sessionId;
        readonly string _buildId;
        readonly string _balanceVersion;
        readonly List<string> _pending = new List<string>();
        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        long _eventCounter;

        /// <summary>Cho phep ghi kem thoi gian mo phong ma khong buoc logger biet ve GameState.</summary>
        public Func<long> SimulationTimeProvider;

        public string Path { get { return _path; } }
        public string SessionId { get { return _sessionId; } }

        public FileTestLogger(string directory, string buildId, string balanceVersion)
        {
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            _sessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
            _buildId = buildId ?? "dev";
            _balanceVersion = balanceVersion ?? "unknown";
            _path = System.IO.Path.Combine(directory, "vuon-nho-playtest.log");
        }

        public void Log(string eventName, params string[] fields)
        {
            _eventCounter++;
            var entry = JsonValue.NewObject();
            entry.Set("eventId", _sessionId + "-" + _eventCounter);
            entry.Set("sessionId", _sessionId);
            entry.Set("event", eventName);
            entry.Set("utcMs", UtcNowMs());
            entry.Set("simulationTimeMs", SimulationTimeProvider != null ? SimulationTimeProvider() : -1);
            entry.Set("buildId", _buildId);
            entry.Set("balanceVersion", _balanceVersion);

            if (fields != null)
            {
                for (int i = 0; i + 1 < fields.Length; i += 2)
                    entry.Set(fields[i], fields[i + 1]);
            }

            _pending.Add(entry.ToJson(false));
            if (_pending.Count >= 20) Flush();
        }

        public void Flush()
        {
            if (_pending.Count == 0) return;
            try
            {
                File.AppendAllText(_path, string.Join("\n", _pending.ToArray()) + "\n", Utf8NoBom);
                _pending.Clear();
            }
            catch (Exception)
            {
                // Log khong duoc lam hong luong choi. Bo qua va thu lai o lan flush sau.
            }
        }

        /// <summary>Sao chep log ra duong dan nguoi thu chon de gui lai.</summary>
        public bool TryExport(string destinationPath, out string error)
        {
            error = null;
            try
            {
                Flush();
                if (!File.Exists(_path))
                {
                    error = "Chua co dong log nao.";
                    return false;
                }
                File.Copy(_path, destinationPath, true);
                return true;
            }
            catch (Exception failure)
            {
                error = failure.Message;
                return false;
            }
        }

        static long UtcNowMs()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }
}

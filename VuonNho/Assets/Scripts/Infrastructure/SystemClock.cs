using System;
using System.Diagnostics;
using VuonNho.Core;

namespace VuonNho.Infrastructure
{
    /// <summary>
    /// Trong phien dung dong ho monotonic; quang nghi offline dung UTC de khong bi anh huong
    /// boi mui gio hoac DST.
    /// </summary>
    public sealed class SystemClock : IClock
    {
        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public long UtcNowMs
        {
            get { return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds; }
        }

        public long MonotonicMs
        {
            get { return _stopwatch.ElapsedMilliseconds; }
        }
    }
}

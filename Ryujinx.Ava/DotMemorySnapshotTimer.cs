using JetBrains.Profiler.Api;
using System;
using System.Diagnostics;
using System.Threading;

namespace Ryujinx.Ava
{
    internal class DotMemorySnapshotTimer
    {
        readonly string _snapshotNamePrefix;
        readonly TimeSpan _period;
        readonly int _stopAfterCount;
        readonly Action? _stopAction;

        Timer _timer;
        Stopwatch _stopwatch;
        int _snapshotCount;

        public DotMemorySnapshotTimer(string snapshotNamePrefix, TimeSpan period, int stopAfterCount, Action? stopAction)
        {
            _snapshotNamePrefix = snapshotNamePrefix;
            _period = period;
            _stopAfterCount = stopAfterCount;
            _stopAction = stopAction;
        }

        public void StartTimer()
        {
            _timer?.Dispose();

            _snapshotCount = 0;
            _timer = new Timer(TimerCallback, state: null, TimeSpan.Zero, _period);
            _stopwatch = Stopwatch.StartNew();
        }

        public void StopTimer()
        {
            _timer?.Dispose();
            _stopAction?.Invoke();
        }

        private void TimerCallback(object _)
        {
            int seconds = (int)Math.Round(_stopwatch.Elapsed.TotalSeconds, 0, MidpointRounding.AwayFromZero);
            string name = $"{_snapshotNamePrefix}-{seconds}s";
            MemoryProfiler.GetSnapshot(name);
            _snapshotCount++;
            if (_snapshotCount == _stopAfterCount)
            {
                StopTimer();
            }
        }
    }
}

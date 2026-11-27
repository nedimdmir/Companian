using System;
using System.Diagnostics;
using System.Threading;

namespace Companian
{
    internal enum ClickButton { Left, Right, Middle }

    internal enum RepeatMode { Count, Duration, Infinite }

    internal sealed class ClickerOptions
    {
        public bool UseFixedPosition;
        public int X;
        public int Y;
        public ClickButton Button;
        public bool DoubleClick;
        public int IntervalMs;
        public RepeatMode Mode;
        public int Count;
        public int DurationSec;
        public int DelaySec;
        /// <summary>Clicks that would land on this window are skipped, so the
        /// clicker can never press its own Start/Stop button.</summary>
        public IntPtr OwnWindow;
    }

    /// <summary>Runs mouse clicks on a background thread with millisecond timing.</summary>
    internal sealed class Clicker
    {
        private volatile bool stopRequested;
        private Thread worker;
        private int clicks;

        /// <summary>Seconds left before clicking starts (raised once per second during the start delay).</summary>
        public event Action<int> Countdown;
        /// <summary>Click count so far (raised at most ~10 times per second).</summary>
        public event Action<int> Progress;
        /// <summary>Total clicks when the run ends, for any reason.</summary>
        public event Action<int> Finished;

        public bool IsRunning
        {
            get { Thread t = worker; return t != null && t.IsAlive; }
        }

        public void Start(ClickerOptions options)
        {
            if (IsRunning) return;
            stopRequested = false;
            clicks = 0;
            worker = new Thread(delegate() { Run(options); });
            worker.IsBackground = true;
            worker.Name = "Companian clicker";
            worker.Start();
        }

        public void Stop()
        {
            stopRequested = true;
        }

        private void Run(ClickerOptions o)
        {
            Native.timeBeginPeriod(1);
            try
            {
                for (int s = o.DelaySec; s > 0 && !stopRequested; s--)
                {
                    Raise(Countdown, s);
                    if (!SleepInterruptible(1000)) return;
                }

                Stopwatch sw = Stopwatch.StartNew();
                long durationMs = (long)o.DurationSec * 1000L;
                long next = 0;
                long lastReport = -1000;

                while (!stopRequested)
                {
                    if (o.Mode == RepeatMode.Count && clicks >= o.Count) break;
                    if (o.Mode == RepeatMode.Duration && sw.ElapsedMilliseconds >= durationMs) break;

                    if (ClickOnce(o)) Interlocked.Increment(ref clicks);

                    long now = sw.ElapsedMilliseconds;
                    if (now - lastReport >= 100)
                    {
                        lastReport = now;
                        Raise(Progress, clicks);
                    }

                    next += o.IntervalMs;
                    long wait = next - sw.ElapsedMilliseconds;
                    if (o.Mode == RepeatMode.Duration)
                        wait = Math.Min(wait, durationMs - sw.ElapsedMilliseconds);

                    if (wait < -250)
                        next = sw.ElapsedMilliseconds; // fell far behind (e.g. system busy): resync
                    else if (wait > 0)
                        SleepInterruptible(wait);
                }
            }
            finally
            {
                Native.timeEndPeriod(1);
                Raise(Finished, clicks);
            }
        }

        private static bool ClickOnce(ClickerOptions o)
        {
            if (o.UseFixedPosition)
                Native.SetCursorPos(o.X, o.Y);

            if (o.OwnWindow != IntPtr.Zero)
            {
                Native.POINT p;
                if (Native.GetCursorPos(out p))
                {
                    IntPtr root = Native.GetAncestor(Native.WindowFromPoint(p), Native.GA_ROOT);
                    if (root == o.OwnWindow) return false;
                }
            }

            uint down, up;
            switch (o.Button)
            {
                case ClickButton.Right:
                    down = Native.MOUSEEVENTF_RIGHTDOWN; up = Native.MOUSEEVENTF_RIGHTUP; break;
                case ClickButton.Middle:
                    down = Native.MOUSEEVENTF_MIDDLEDOWN; up = Native.MOUSEEVENTF_MIDDLEUP; break;
                default:
                    down = Native.MOUSEEVENTF_LEFTDOWN; up = Native.MOUSEEVENTF_LEFTUP; break;
            }

            if (o.DoubleClick)
                Native.SendMouse(down, up, down, up);
            else
                Native.SendMouse(down, up);
            return true;
        }

        private bool SleepInterruptible(long ms)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (!stopRequested)
            {
                long left = ms - sw.ElapsedMilliseconds;
                if (left <= 0) return true;
                Thread.Sleep((int)Math.Min(left, 20));
            }
            return false;
        }

        private static void Raise(Action<int> handler, int value)
        {
            if (handler != null) handler(value);
        }
    }
}

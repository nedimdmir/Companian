using System;
using System.Collections.Generic;
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
        /// <summary>When set, the clicker walks through these steps instead of clicking one spot.
        /// Position, button, click type and interval then come from each step, and
        /// <see cref="Count"/> means "rounds through the list" instead of clicks.</summary>
        public List<ClickStep> Steps;
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
        private volatile int currentStep = -1;
        private volatile int currentRound;

        /// <summary>Index of the step being run in sequence mode, or -1.</summary>
        public int CurrentStep
        {
            get { return currentStep; }
        }

        /// <summary>1-based round through the step list in sequence mode.</summary>
        public int CurrentRound
        {
            get { return currentRound; }
        }

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
            currentStep = -1;
            currentRound = 0;
            if (options.Steps != null)
            {
                // Work on a private copy so editing the list in the UI cannot affect a running sequence.
                List<ClickStep> copy = new List<ClickStep>();
                foreach (ClickStep s in options.Steps) copy.Add(s.Clone());
                options.Steps = copy;
            }
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

                if (o.Steps != null)
                {
                    RunSequence(o);
                    return;
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

        /// <summary>Clicks every step in order, waiting each step's own delay after its click.</summary>
        private void RunSequence(ClickerOptions o)
        {
            List<ClickStep> steps = o.Steps;
            Stopwatch sw = Stopwatch.StartNew();
            long durationMs = (long)o.DurationSec * 1000L;
            long lastReport = -1000;

            for (int round = 1; !stopRequested; round++)
            {
                if (o.Mode == RepeatMode.Count && round > o.Count) break;
                currentRound = round;

                for (int i = 0; i < steps.Count && !stopRequested; i++)
                {
                    if (o.Mode == RepeatMode.Duration && sw.ElapsedMilliseconds >= durationMs) return;

                    ClickStep s = steps[i];
                    currentStep = i;
                    if (ClickAt(true, s.X, s.Y, s.Button, s.DoubleClick, o.OwnWindow))
                        Interlocked.Increment(ref clicks);

                    long now = sw.ElapsedMilliseconds;
                    if (now - lastReport >= 100)
                    {
                        lastReport = now;
                        Raise(Progress, clicks);
                    }

                    long wait = s.DelayMs;
                    if (o.Mode == RepeatMode.Duration)
                        wait = Math.Min(wait, durationMs - sw.ElapsedMilliseconds);
                    if (wait > 0 && !SleepInterruptible(wait)) return;
                }
            }
        }

        private static bool ClickOnce(ClickerOptions o)
        {
            return ClickAt(o.UseFixedPosition, o.X, o.Y, o.Button, o.DoubleClick, o.OwnWindow);
        }

        private static bool ClickAt(bool move, int x, int y, ClickButton button, bool doubleClick, IntPtr ownWindow)
        {
            if (move)
                Native.SetCursorPos(x, y);

            if (ownWindow != IntPtr.Zero)
            {
                Native.POINT p;
                if (Native.GetCursorPos(out p))
                {
                    IntPtr root = Native.GetAncestor(Native.WindowFromPoint(p), Native.GA_ROOT);
                    if (root == ownWindow) return false;
                }
            }

            uint down, up;
            switch (button)
            {
                case ClickButton.Right:
                    down = Native.MOUSEEVENTF_RIGHTDOWN; up = Native.MOUSEEVENTF_RIGHTUP; break;
                case ClickButton.Middle:
                    down = Native.MOUSEEVENTF_MIDDLEDOWN; up = Native.MOUSEEVENTF_MIDDLEUP; break;
                default:
                    down = Native.MOUSEEVENTF_LEFTDOWN; up = Native.MOUSEEVENTF_LEFTUP; break;
            }

            if (doubleClick)
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

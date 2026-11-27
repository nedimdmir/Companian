using System;
using System.Diagnostics;
using System.IO;

namespace Companian
{
    internal enum PowerAction { Shutdown, Restart, Sleep, Hibernate, Lock, LogOff }

    /// <summary>Power actions. None of these need administrator rights for the signed-in user.</summary>
    internal static class Power
    {
        public static readonly string[] ActionKeys =
        {
            "act_shutdown", "act_restart", "act_sleep", "act_hibernate", "act_lock", "act_logoff"
        };

        public static void Execute(PowerAction action, bool force)
        {
            string f = force ? " /f" : "";
            switch (action)
            {
                case PowerAction.Shutdown: RunShutdownExe("/s /t 0" + f); break;
                case PowerAction.Restart: RunShutdownExe("/r /t 0" + f); break;
                case PowerAction.LogOff: RunShutdownExe("/l" + f); break;
                case PowerAction.Hibernate: RunShutdownExe("/h"); break;
                case PowerAction.Sleep: Native.SetSuspendState(false, false, false); break;
                case PowerAction.Lock: Native.LockWorkStation(); break;
            }
        }

        private static void RunShutdownExe(string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "shutdown.exe"), args);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }
    }

    /// <summary>Keeps the PC (and optionally the display) awake while active.
    /// Must be used from the UI thread, because the execution state belongs to the calling thread.</summary>
    internal sealed class KeepAwake
    {
        private bool keepDisplay;

        public bool Active { get; private set; }

        public void Enable(bool display)
        {
            keepDisplay = display;
            Active = true;
            Apply();
        }

        public void Refresh()
        {
            if (Active) Apply();
        }

        public void Disable()
        {
            Native.SetThreadExecutionState(Native.ES_CONTINUOUS);
            Active = false;
        }

        private void Apply()
        {
            uint flags = Native.ES_CONTINUOUS | Native.ES_SYSTEM_REQUIRED;
            if (keepDisplay) flags |= Native.ES_DISPLAY_REQUIRED;
            Native.SetThreadExecutionState(flags);
        }

        /// <summary>Moves the pointer 1 px and back, for apps that measure idle time from input.</summary>
        public static void NudgeMouse()
        {
            Native.MoveMouseRelative(1, 0);
            Native.MoveMouseRelative(-1, 0);
        }
    }
}

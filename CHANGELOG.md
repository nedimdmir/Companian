# Changelog

## 1.2.0 — 2026-11-27

- Settings: add a one-time administrator setup that registers an on-demand, highest-privilege
  Task Scheduler task and creates a desktop shortcut to run it without another UAC prompt.

## 1.1.0 — 2026-11-27

- New **Bulk Install Manager** page. Tick programs from a catalog of 40 popular apps, search winget
  for any other package, or add your own .exe / .msi / .msix setup files; they install one after
  another in the background with per-program status and a live log. "Install for me only" uses
  winget's user scope and never lets an installer open a UAC prompt; programs that need admin
  rights are skipped and marked. MSI, Inno Setup and NSIS installers are detected and run silently.
  Save / load the list as `.json` to reuse it after reinstalling Windows.
- List columns now scale with the display DPI.
- Auto clicker: new "list of positions" mode. Queue any number of positions, each with its own
  button, single/double click and wait (ms) after the click; reorder, edit and remove steps.
  F8 adds the cursor position as a new step. Repeat the list a set number of rounds, for a
  duration, or forever. Save and load the list as a `.json` file; the list is also remembered
  between runs.

## 1.0.0 — 2026-11-27

First release.

- Auto clicker: cursor or fixed position, left/right/middle, single/double click,
  h/m/s/ms interval, count / duration / infinite modes, start delay, F6 / F8 global hotkeys.
- Shutdown timer: shut down, restart, sleep, hibernate, lock, sign out; countdown or a set time;
  quick picks; cancel window in the last minute; optional force-close.
- Keep awake: prevents sleep and screen-off, optional time limit and 1 px mouse nudge.
- Reminders: one-off or repeating on-screen notifications.
- Languages: Turkish, English, German, Spanish (switch at runtime).
- Dark and light themes, system tray, start with Windows.
- Per-user install and uninstall without administrator rights; portable use without installing.

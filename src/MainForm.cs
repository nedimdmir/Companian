using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Media;
using System.Windows.Forms;

namespace Companian
{
    internal sealed class Reminder
    {
        public string Message;
        public int Minutes;
        public bool Repeat;
        public bool Sound;
        public DateTime Next;
    }

    internal sealed class MainForm : Form
    {
        private const int HotkeyClicker = 1;
        private const int HotkeyCapture = 2;
        private const int CardWidth = 590;

        private enum ClickerState { Idle, Waiting, Running, Done }

        private readonly Settings settings;
        private readonly Clicker clicker = new Clicker();
        private readonly KeepAwake awake = new KeepAwake();
        private readonly List<KeyValuePair<Control, string>> texts = new List<KeyValuePair<Control, string>>();
        private readonly List<FlowLayoutPanel> pages = new List<FlowLayoutPanel>();
        private readonly List<NavItem> navItems = new List<NavItem>();
        private readonly List<string> failedHotkeys = new List<string>();
        private readonly List<Reminder> reminders = new List<Reminder>();
        private readonly Icon appIcon;

        private static readonly string[] NavKeys = { "nav_clicker", "nav_shutdown", "nav_awake", "nav_reminder", "nav_setup", "nav_settings" };
        private static readonly string[] NavGlyphs = { "", "", "", "", "\uE896", "" };
        private static readonly int[] QuickMinutes = { 15, 30, 60, 120 };

        private Theme theme;
        private bool allowShow;
        private bool loading;
        private bool exiting;
        private bool cleanedUp;
        private bool skipSave;
        private bool trayHintShown;

        private Panel content;
        private NotifyIcon tray;
        private ToolStripMenuItem trayShow, trayClicker, trayExit;
        private Timer secondTimer, fastTimer, pickTimer;

        // Auto clicker
        private Button btnClicker, btnPick;
        private Label lblClickerStatus, lblLivePos, lblRate;
        private RadioButton rbCursor, rbFixed, rbCount, rbDuration, rbInfinite;
        private NumericUpDown nX, nY, nH, nM, nS, nMs, nCount, nDuration, nDelay;
        private ComboBox cbButton, cbClickType;
        private ClickerState clickerState = ClickerState.Idle;
        private int clickerValue;
        private string clickerErrorKey;
        private int pickLeft;
        private Button pickButton;

        // Click sequence (list of positions, each with its own wait)
        private readonly List<ClickStep> steps = new List<ClickStep>();
        private RadioButton rbSequence;
        private FlowLayoutPanel cardClick, cardSeq;
        private ListView lvSeq;
        private NumericUpDown sqX, sqY, sqDelay;
        private ComboBox sqButton, sqType;
        private Button sqPick;
        private Label lblSeqInfo;
        private string seqFile;
        private bool sequenceRun;

        // Shutdown timer
        private Label lblSdBig, lblSdInfo;
        private Button btnSdStart, btnSdCancel;
        private readonly List<Button> quickButtons = new List<Button>();
        private ComboBox cbAction;
        private RadioButton rbAfter, rbAt;
        private NumericUpDown sdH, sdM, sdS, atH, atM;
        private CheckBox chkForce, chkWarn;
        private DateTime? sdDue;
        private PowerAction sdAction;
        private bool sdForce;
        private bool warnShown;
        private WarningForm warnForm;

        // Keep awake
        private Label lblAwBig, lblAwInfo;
        private Button btnAwake;
        private CheckBox chkDisplay, chkJiggle;
        private RadioButton rbAwForever, rbAwFor;
        private NumericUpDown nAwMin;
        private DateTime? awUntil;
        private int jiggleTick;

        // Reminders
        private TextBox txtRm;

        // Bulk install (winget packages and installer files)
        private readonly SetupRunner setup = new SetupRunner();
        private readonly List<SetupItem> setupItems = new List<SetupItem>();
        private readonly List<Control> setupEditors = new List<Control>();
        private ListView lvSetup, lvSearch;
        private Button btnSetup, btnSearch;
        private Label lblSetupStatus;
        private TextBox txtSearch, txtArgs, txtLog;
        private CheckBox chkUserOnly;
        private bool setupFilling;
        private bool setupRan;
        private NumericUpDown nRmMin;
        private CheckBox chkRmRepeat, chkRmSound;
        private ListView lvRm;

        // Settings
        private ComboBox cbLang, cbTheme;
        private CheckBox chkTop, chkTray, chkAuto, chkDesk;
        private Button btnInstall, btnUninstall;
        private Button btnElevatedShortcut;
        private Label lblInstallState, lblAbout;
        private LinkLabel lnkGit;

        public MainForm(Settings settings, bool startInTray)
        {
            this.settings = settings;
            allowShow = !startInTray;

            SuspendLayout();
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = AppInfo.Name;
            Font = Fonts.Body;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(860, 640);
            appIcon = LoadAppIcon();
            Icon = appIcon;

            BuildShell();
            BuildClickerPage();
            BuildShutdownPage();
            BuildAwakePage();
            BuildReminderPage();
            BuildSetupPage();
            BuildSettingsPage();
            BuildTray();

            ResumeLayout(false);
            FixNumericMargins(this, CurrentAutoScaleDimensions.Width / 96f);
            ScaleListColumns(this, CurrentAutoScaleDimensions.Width / 96f);
            PerformLayout();
            MinimumSize = new Size(Width, Math.Min(Height, 480));

            loading = true;
            ApplyTexts();
            LoadSettings();
            loading = false;
            ApplyTheme(settings.Get("theme", "dark") != "light");
            SelectPage(Math.Max(0, Math.Min(pages.Count - 1, settings.GetInt("page", 0))));

            clicker.Countdown += delegate(int s) { Ui(delegate { SetClickerState(ClickerState.Waiting, s); }); };
            clicker.Progress += delegate(int n) { Ui(delegate { SetClickerState(ClickerState.Running, n); }); };
            clicker.Finished += delegate(int n) { Ui(delegate { SetClickerState(ClickerState.Done, n); }); };
            setup.ItemChanged += delegate(SetupItem it) { Ui(delegate { UpdateSetupRow(it); UpdateSetupUi(); }); };
            setup.Log += delegate(string line) { Ui(delegate { AppendSetupLog(line); }); };
            setup.Finished += delegate { Ui(delegate { UpdateSetupUi(); }); };

            secondTimer = new Timer { Interval = 1000 };
            secondTimer.Tick += OnSecondTick;
            secondTimer.Start();
            fastTimer = new Timer { Interval = 150 };
            fastTimer.Tick += delegate { UpdateLivePosition(); };
            fastTimer.Start();
            pickTimer = new Timer { Interval = 1000 };
            pickTimer.Tick += OnPickTick;

            Lang.Changed += OnLanguageChanged;
        }

        /// <summary>
        /// WinForms inflates the right margin of NumericUpDown controls when it rescales a form
        /// for high DPI, which leaves large gaps in flow layouts. Put the intended margins back.
        /// </summary>
        private static void FixNumericMargins(Control parent, float scale)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is NumericUpDown)
                    c.Margin = new Padding(0, (int)Math.Round(2 * scale), (int)Math.Round(4 * scale), (int)Math.Round(2 * scale));
                else
                    FixNumericMargins(c, scale);
            }
        }

        /// <summary>DPI scaling resizes list views but not their columns; widen the columns to match.</summary>
        private static void ScaleListColumns(Control parent, float scale)
        {
            if (scale <= 1.01f) return;
            foreach (Control c in parent.Controls)
            {
                ListView lv = c as ListView;
                if (lv != null)
                {
                    foreach (ColumnHeader col in lv.Columns)
                        col.Width = (int)Math.Round(col.Width * scale);
                }
                else
                {
                    ScaleListColumns(c, scale);
                }
            }
        }

        // ------------------------------------------------------------------ layout helpers

        private static Icon LoadAppIcon()
        {
            using (System.IO.Stream s = typeof(MainForm).Assembly.GetManifestResourceStream("Companian.app.ico"))
            {
                if (s != null) return new Icon(s);
            }
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }

        private T Bind<T>(T control, string key) where T : Control
        {
            if (key != null) texts.Add(new KeyValuePair<Control, string>(control, key));
            return control;
        }

        private Label Lbl(string key)
        {
            return Bind(new Label { AutoSize = true, Margin = new Padding(0, 6, 6, 0) }, key);
        }

        private Label Unit(string key)
        {
            return Bind(new Label { AutoSize = true, Margin = new Padding(0, 6, 12, 0), Tag = "muted" }, key);
        }

        private Label Muted(string key)
        {
            return Bind(new Label { AutoSize = true, Margin = new Padding(0, 4, 0, 2), Tag = "muted", MaximumSize = new Size(CardWidth - 30, 0) }, key);
        }

        private static NumericUpDown Num(int min, int max, int value, int width)
        {
            return new NumericUpDown
            {
                Minimum = min, Maximum = max, Value = value, Width = width,
                BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 2, 4, 2), TextAlign = HorizontalAlignment.Right
            };
        }

        private static ComboBox Combo(int width)
        {
            return new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width, Margin = new Padding(0, 2, 12, 2) };
        }

        private CheckBox Chk(string key)
        {
            return Bind(new CheckBox { AutoSize = true, Margin = new Padding(0, 4, 12, 3) }, key);
        }

        private RadioButton Radio(string key)
        {
            return Bind(new RadioButton { AutoSize = true, Margin = new Padding(0, 4, 8, 3) }, key);
        }

        private Button Btn(string key, string role)
        {
            return Bind(new Button
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(110, 34),
                Padding = new Padding(10, 0, 10, 0), Margin = new Padding(0, 4, 8, 2), Tag = role
            }, key);
        }

        private static FlowLayoutPanel Row(params Control[] controls)
        {
            FlowLayoutPanel p = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 1, 0, 1)
            };
            p.Controls.AddRange(controls);
            return p;
        }

        private FlowLayoutPanel Card(string titleKey, params Control[] rows)
        {
            FlowLayoutPanel p = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
                FlowDirection = FlowDirection.TopDown, Padding = new Padding(14, 10, 14, 12),
                Margin = new Padding(0, 0, 0, 10), MinimumSize = new Size(CardWidth, 0), Tag = "card"
            };
            if (titleKey != null)
                p.Controls.Add(Bind(new Label { AutoSize = true, Font = Fonts.Bold, Margin = new Padding(0, 0, 0, 6) }, titleKey));
            p.Controls.AddRange(rows);
            return p;
        }

        private FlowLayoutPanel NewPage(string titleKey, string subtitleKey)
        {
            FlowLayoutPanel p = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoScroll = true, Padding = new Padding(26, 20, 16, 16), Visible = false
            };
            p.Controls.Add(Bind(new Label { AutoSize = true, Font = Fonts.Header, Margin = new Padding(0, 0, 0, 2) }, titleKey));
            p.Controls.Add(Bind(new Label { AutoSize = true, Tag = "muted", Margin = new Padding(2, 0, 0, 12), MaximumSize = new Size(CardWidth, 0) }, subtitleKey));
            content.Controls.Add(p);
            pages.Add(p);
            return p;
        }

        /// <summary>Makes radio buttons that live in different containers behave as one group.</summary>
        private static void Group(params RadioButton[] buttons)
        {
            foreach (RadioButton rb in buttons)
            {
                RadioButton self = rb;
                self.CheckedChanged += delegate
                {
                    if (!self.Checked) return;
                    foreach (RadioButton other in buttons)
                        if (other != self) other.Checked = false;
                };
            }
        }

        private static void SetCombo(ComboBox cb, params string[] keys)
        {
            int index = cb.SelectedIndex;
            cb.BeginUpdate();
            cb.Items.Clear();
            foreach (string k in keys) cb.Items.Add(Lang.T(k));
            cb.SelectedIndex = index < 0 ? 0 : Math.Min(index, keys.Length - 1);
            cb.EndUpdate();
        }

        private static decimal Clamp(NumericUpDown n, int value)
        {
            return Math.Max(n.Minimum, Math.Min(n.Maximum, value));
        }

        private void Ui(MethodInvoker action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke(action); }
            catch (InvalidOperationException) { }
        }

        // ------------------------------------------------------------------ shell

        private void BuildShell()
        {
            content = new Panel { Dock = DockStyle.Fill };
            Panel nav = new Panel { Dock = DockStyle.Left, Width = 210, Tag = "nav" };

            PictureBox logo = new PictureBox
            {
                Image = new Icon(appIcon, 32, 32).ToBitmap(), SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(18, 22), Size = new Size(32, 32)
            };
            Label brand = new Label { Text = AppInfo.Name, Font = Fonts.Brand, AutoSize = true, Location = new Point(56, 20) };
            Label version = new Label { Text = "v" + AppInfo.Version, Font = Fonts.Small, AutoSize = true, Location = new Point(58, 46), Tag = "muted" };

            FlowLayoutPanel list = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                Location = new Point(0, 86), Size = new Size(210, NavKeys.Length * 46 + 4), Tag = "nav"
            };
            for (int i = 0; i < NavKeys.Length; i++)
            {
                int index = i;
                NavItem item = new NavItem { Width = 210, Glyph = NavGlyphs[i] };
                item.Click += delegate { SelectPage(index); };
                navItems.Add(item);
                list.Controls.Add(item);
            }

            nav.Controls.AddRange(new Control[] { logo, brand, version, list });
            Controls.Add(content);
            Controls.Add(nav);
        }

        private void SelectPage(int index)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].Visible = i == index;
                navItems[i].Selected = i == index;
            }
            ActiveControl = null;
            settings.Set("page", index);
        }

        private void BuildTray()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            trayShow = new ToolStripMenuItem();
            trayShow.Font = new Font(menu.Font, FontStyle.Bold);
            trayShow.Click += delegate { ShowFromTray(); };
            trayClicker = new ToolStripMenuItem();
            trayClicker.Click += delegate { ToggleClicker(); };
            trayExit = new ToolStripMenuItem();
            trayExit.Click += delegate { ExitApp(true); };
            menu.Items.AddRange(new ToolStripItem[] { trayShow, trayClicker, new ToolStripSeparator(), trayExit });

            tray = new NotifyIcon
            {
                Icon = new Icon(appIcon, SystemInformation.SmallIconSize),
                Text = AppInfo.Name, ContextMenuStrip = menu, Visible = true
            };
            tray.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) ShowFromTray(); };
        }

        // ------------------------------------------------------------------ pages

        private void BuildClickerPage()
        {
            FlowLayoutPanel page = NewPage("nav_clicker", "clk_sub");

            btnClicker = Btn(null, "accent");
            btnClicker.MinimumSize = new Size(170, 42);
            btnClicker.Font = Fonts.Bold;
            btnClicker.Click += delegate { ToggleClicker(); };
            lblClickerStatus = new Label { AutoSize = true, Margin = new Padding(8, 15, 0, 0), MaximumSize = new Size(380, 0) };
            page.Controls.Add(Card(null, Row(btnClicker, lblClickerStatus)));

            rbCursor = Radio("clk_pos_cursor");
            rbFixed = Radio("clk_pos_fixed");
            rbSequence = Radio("clk_pos_sequence");
            rbSequence.CheckedChanged += delegate { UpdateSequenceMode(); };
            nX = Num(-100000, 100000, 0, 72);
            nY = Num(-100000, 100000, 0, 72);
            nX.ValueChanged += delegate { if (!loading) rbFixed.Checked = true; };
            nY.ValueChanged += delegate { if (!loading) rbFixed.Checked = true; };
            btnPick = Btn("clk_pick", null);
            btnPick.Click += delegate { StartPick(btnPick); };
            lblLivePos = new Label { AutoSize = true, Margin = new Padding(0, 4, 0, 0), Tag = "muted" };
            Group(rbCursor, rbFixed, rbSequence);
            page.Controls.Add(Card("clk_card_pos",
                Row(rbCursor),
                Row(rbFixed, new Label { Text = "X", AutoSize = true, Margin = new Padding(0, 6, 4, 0) }, nX,
                    new Label { Text = "Y", AutoSize = true, Margin = new Padding(8, 6, 4, 0) }, nY, btnPick),
                Row(rbSequence),
                Row(lblLivePos),
                Muted("clk_pick_hint")));

            BuildSequenceCard(page);

            cbButton = Combo(110);
            cbClickType = Combo(130);
            nH = Num(0, 23, 0, 50);
            nM = Num(0, 59, 0, 50);
            nS = Num(0, 59, 0, 50);
            nMs = Num(0, 999, 100, 60);
            foreach (NumericUpDown n in new[] { nH, nM, nS, nMs })
                n.ValueChanged += delegate { UpdateRate(); };
            lblRate = new Label { AutoSize = true, Margin = new Padding(0, 4, 0, 0), Tag = "accenttext" };
            Label typeLabel = Lbl("clk_type");
            typeLabel.Margin = new Padding(8, 6, 6, 0);
            cardClick = Card("clk_card_click",
                Row(Lbl("clk_button"), cbButton, typeLabel, cbClickType),
                Row(Lbl("clk_interval"), nH, Unit("unit_h"), nM, Unit("unit_m"), nS, Unit("unit_s"), nMs, Unit("unit_ms")),
                Row(lblRate));
            page.Controls.Add(cardClick);

            rbCount = Radio("clk_rep_count");
            rbDuration = Radio("clk_rep_duration");
            rbInfinite = Radio("clk_rep_infinite");
            nCount = Num(1, 100000000, 100, 100);
            nDuration = Num(1, 604800, 60, 100);
            nDelay = Num(0, 3600, 2, 64);
            nCount.ValueChanged += delegate { if (!loading) rbCount.Checked = true; };
            nDuration.ValueChanged += delegate { if (!loading) rbDuration.Checked = true; };
            Group(rbCount, rbDuration, rbInfinite);
            page.Controls.Add(Card("clk_card_repeat",
                Row(rbCount, nCount),
                Row(rbDuration, nDuration),
                Row(rbInfinite),
                Row(Lbl("clk_delay"), nDelay)));
        }

        /// <summary>The list of positions used by the "click a list of positions" mode.</summary>
        private void BuildSequenceCard(FlowLayoutPanel page)
        {
            lvSeq = new ListView
            {
                View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable, BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(CardWidth - 28, 190), Margin = new Padding(0, 2, 0, 4)
            };
            foreach (int width in new[] { 40, 80, 80, 100, 110, 120 })
                lvSeq.Columns.Add("", width);
            lvSeq.SelectedIndexChanged += delegate { OnSequenceSelected(); };
            lvSeq.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) RemoveStep(); };

            Button up = SmallBtn("seq_up");
            up.Click += delegate { MoveStep(-1); };
            Button down = SmallBtn("seq_down");
            down.Click += delegate { MoveStep(1); };
            Button remove = SmallBtn("seq_remove");
            remove.Click += delegate { RemoveStep(); };
            Button clear = SmallBtn("seq_clear");
            clear.Click += delegate { ClearSteps(); };

            sqX = Num(-100000, 100000, 0, 72);
            sqY = Num(-100000, 100000, 0, 72);
            sqPick = Btn("clk_pick", null);
            sqPick.Click += delegate { StartPick(sqPick); };
            sqButton = Combo(110);
            sqType = Combo(130);
            sqDelay = Num(0, ClickStep.MaxDelayMs, 500, 100);
            Label typeLabel = Lbl("clk_type");
            typeLabel.Margin = new Padding(8, 6, 6, 0);

            Button add = Btn("seq_add", "accent");
            add.Click += delegate { AddStep(); };
            Button update = Btn("seq_update", null);
            update.Click += delegate { UpdateStep(); };
            Button save = Btn("seq_save", null);
            save.Click += delegate { SaveSequenceFile(); };
            Button load = Btn("seq_load", null);
            load.Click += delegate { LoadSequenceFile(); };

            lblSeqInfo = new Label { AutoSize = true, Tag = "muted", Margin = new Padding(0, 2, 0, 4), MaximumSize = new Size(CardWidth - 30, 0) };

            cardSeq = Card("seq_card",
                lvSeq,
                lblSeqInfo,
                Row(up, down, remove, clear),
                Row(new Label { Text = "X", AutoSize = true, Margin = new Padding(0, 6, 4, 0) }, sqX,
                    new Label { Text = "Y", AutoSize = true, Margin = new Padding(8, 6, 4, 0) }, sqY, sqPick),
                Row(Lbl("clk_button"), sqButton, typeLabel, sqType),
                Row(Lbl("seq_delay"), sqDelay, Unit("unit_ms")),
                Row(add, update),
                Row(save, load),
                Muted("seq_hint"));
            cardSeq.Visible = false;
            page.Controls.Add(cardSeq);
        }

        private Button SmallBtn(string key)
        {
            Button b = Btn(key, null);
            b.MinimumSize = new Size(80, 30);
            return b;
        }

        private void BuildShutdownPage()
        {
            FlowLayoutPanel page = NewPage("nav_shutdown", "sd_sub");

            lblSdBig = new Label { AutoSize = true, Font = Fonts.Big, Margin = new Padding(0, 0, 0, 0) };
            lblSdInfo = new Label { AutoSize = true, Tag = "muted", Margin = new Padding(3, 0, 0, 6) };
            btnSdStart = Btn("sd_start", "accent");
            btnSdStart.Click += delegate { StartShutdownTimer(); };
            btnSdCancel = Btn("sd_cancel", "danger");
            btnSdCancel.Click += delegate { CancelShutdownTimer(); };
            page.Controls.Add(Card(null, Row(lblSdBig), Row(lblSdInfo), Row(btnSdStart, btnSdCancel)));

            cbAction = Combo(240);
            page.Controls.Add(Card("sd_card_action", Row(cbAction)));

            rbAfter = Radio("sd_after");
            rbAt = Radio("sd_at");
            sdH = Num(0, 999, 1, 58);
            sdM = Num(0, 59, 0, 50);
            sdS = Num(0, 59, 0, 50);
            atH = Num(0, 23, 23, 50);
            atM = Num(0, 59, 0, 50);
            foreach (NumericUpDown n in new[] { sdH, sdM, sdS })
                n.ValueChanged += delegate { if (!loading) rbAfter.Checked = true; };
            foreach (NumericUpDown n in new[] { atH, atM })
                n.ValueChanged += delegate { if (!loading) rbAt.Checked = true; };
            Group(rbAfter, rbAt);

            FlowLayoutPanel quickRow = Row(Lbl("sd_quick"));
            foreach (int minutes in QuickMinutes)
            {
                int m = minutes;
                Button b = Btn(null, null);
                b.MinimumSize = new Size(64, 30);
                b.Click += delegate
                {
                    loading = true;
                    sdH.Value = m / 60;
                    sdM.Value = m % 60;
                    sdS.Value = 0;
                    loading = false;
                    rbAfter.Checked = true;
                };
                quickButtons.Add(b);
                quickRow.Controls.Add(b);
            }

            page.Controls.Add(Card("sd_card_when",
                Row(rbAfter, sdH, Unit("unit_h"), sdM, Unit("unit_m"), sdS, Unit("unit_s")),
                quickRow,
                Row(rbAt, atH, new Label { Text = ":", AutoSize = true, Margin = new Padding(0, 5, 4, 0) }, atM)));

            chkForce = Chk("sd_force");
            chkWarn = Chk("sd_warn");
            page.Controls.Add(Card("aw_card_options", Row(chkWarn), Row(chkForce)));
        }

        private void BuildAwakePage()
        {
            FlowLayoutPanel page = NewPage("nav_awake", "aw_sub");

            lblAwBig = new Label { AutoSize = true, Font = Fonts.Header, Margin = new Padding(0, 4, 0, 2) };
            lblAwInfo = new Label { AutoSize = true, Tag = "muted", Margin = new Padding(2, 0, 0, 6) };
            btnAwake = Btn(null, "accent");
            btnAwake.Click += delegate { if (awake.Active) StopAwake(); else StartAwake(); };
            page.Controls.Add(Card(null, Row(lblAwBig), Row(lblAwInfo), Row(btnAwake)));

            chkDisplay = Chk("aw_display");
            chkDisplay.CheckedChanged += delegate { if (awake.Active) awake.Enable(chkDisplay.Checked); };
            chkJiggle = Chk("aw_jiggle");
            rbAwForever = Radio("aw_forever");
            rbAwFor = Radio("aw_for");
            nAwMin = Num(1, 10080, 60, 76);
            nAwMin.ValueChanged += delegate { if (!loading) rbAwFor.Checked = true; };
            Group(rbAwForever, rbAwFor);
            page.Controls.Add(Card("aw_card_options", Row(chkDisplay), Row(chkJiggle), Row(rbAwForever), Row(rbAwFor, nAwMin)));
        }

        private void BuildReminderPage()
        {
            FlowLayoutPanel page = NewPage("nav_reminder", "rm_sub");

            txtRm = new TextBox { Width = 400, Margin = new Padding(0, 3, 8, 2), BorderStyle = BorderStyle.FixedSingle, MaxLength = 200 };
            nRmMin = Num(1, 10080, 30, 76);
            chkRmRepeat = Chk("rm_repeat");
            chkRmRepeat.Margin = new Padding(8, 4, 12, 3);
            chkRmSound = Chk("rm_sound");
            Button add = Btn("rm_add", "accent");
            add.Click += delegate { AddReminder(); };
            txtRm.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { AddReminder(); e.SuppressKeyPress = true; } };
            page.Controls.Add(Card("rm_card_new",
                Row(Lbl("rm_message"), txtRm),
                Row(Lbl("rm_after"), nRmMin, chkRmRepeat, chkRmSound),
                Row(add)));

            lvRm = new ListView
            {
                View = View.Details, FullRowSelect = true, HideSelection = false, HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.FixedSingle, Size = new Size(CardWidth - 28, 170), Margin = new Padding(0, 2, 0, 4)
            };
            lvRm.Columns.Add("", 300);
            lvRm.Columns.Add("", 110);
            lvRm.Columns.Add("", 140);
            Button remove = Btn("rm_remove", null);
            remove.Click += delegate { RemoveSelectedReminders(); };
            lvRm.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) RemoveSelectedReminders(); };
            page.Controls.Add(Card("rm_card_list", lvRm, Row(remove)));
        }

        private void BuildSetupPage()
        {
            FlowLayoutPanel page = NewPage("nav_setup_title", "su_sub");

            btnSetup = Btn(null, "accent");
            btnSetup.MinimumSize = new Size(170, 42);
            btnSetup.Font = Fonts.Bold;
            btnSetup.Click += delegate { ToggleSetup(); };
            lblSetupStatus = new Label { AutoSize = true, Margin = new Padding(8, 15, 0, 0), MaximumSize = new Size(380, 0) };
            page.Controls.Add(Card(null, Row(btnSetup, lblSetupStatus)));

            // --- the list to install ---
            lvSetup = new ListView
            {
                View = View.Details, CheckBoxes = true, FullRowSelect = true, HideSelection = false, MultiSelect = true,
                ShowItemToolTips = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable, BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(CardWidth - 28, 300), Margin = new Padding(0, 2, 0, 4)
            };
            lvSetup.Columns.Add("", 175);
            lvSetup.Columns.Add("", 175);
            lvSetup.Columns.Add("", 185);
            lvSetup.ItemCheck += delegate(object s, ItemCheckEventArgs e) { if (setup.IsRunning && !setupFilling) e.NewValue = e.CurrentValue; };
            lvSetup.ItemChecked += delegate(object s, ItemCheckedEventArgs e)
            {
                if (setupFilling || e.Item.Index >= setupItems.Count) return;
                setupItems[e.Item.Index].Checked = e.Item.Checked;
                UpdateSetupUi();
            };
            lvSetup.SelectedIndexChanged += delegate { OnSetupSelected(); };
            lvSetup.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Delete && !setup.IsRunning) RemoveSetupItems(); };

            Button all = SmallBtn("su_all");
            all.Click += delegate { CheckAllSetup(true); };
            Button none = SmallBtn("su_none_btn");
            none.Click += delegate { CheckAllSetup(false); };
            Button remove = SmallBtn("seq_remove");
            remove.Click += delegate { RemoveSetupItems(); };
            Button save = Btn("su_save", null);
            save.Click += delegate { SaveSetupList(); };
            Button load = Btn("su_load", null);
            load.Click += delegate { LoadSetupList(); };
            setupEditors.AddRange(new Control[] { all, none, remove, load });
            page.Controls.Add(Card("su_card_list", lvSetup, Row(all, none, remove), Row(save, load), Muted("su_list_hint")));

            // --- adding programs: winget search or an installer file ---
            txtSearch = new TextBox { Width = 300, Margin = new Padding(0, 3, 8, 2), BorderStyle = BorderStyle.FixedSingle };
            btnSearch = Btn("su_search", null);
            btnSearch.Click += delegate { SearchWinget(); };
            txtSearch.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { SearchWinget(); e.SuppressKeyPress = true; } };
            lvSearch = new ListView
            {
                View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable, BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(CardWidth - 28, 140), Margin = new Padding(0, 2, 0, 4)
            };
            lvSearch.Columns.Add("", 225);
            lvSearch.Columns.Add("", 215);
            lvSearch.Columns.Add("", 95);
            lvSearch.DoubleClick += delegate { AddWingetFromSearch(); };
            Button addWinget = Btn("su_add_winget", "accent");
            addWinget.Click += delegate { AddWingetFromSearch(); };
            Button addFile = Btn("su_add_file", null);
            addFile.Click += delegate { AddSetupFiles(); };

            txtArgs = new TextBox { Width = 330, Margin = new Padding(0, 3, 8, 2), BorderStyle = BorderStyle.FixedSingle, Enabled = false };
            Button applyArgs = SmallBtn("su_args_apply");
            applyArgs.Click += delegate { ApplySetupArgs(); };
            txtArgs.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { ApplySetupArgs(); e.SuppressKeyPress = true; } };
            setupEditors.AddRange(new Control[] { txtSearch, btnSearch, addWinget, addFile, applyArgs });

            page.Controls.Add(Card("su_card_add",
                Row(Lbl("su_search_label"), txtSearch, btnSearch),
                lvSearch,
                Row(addWinget),
                Muted("su_search_hint"),
                Row(addFile),
                Row(Lbl("su_args"), txtArgs, applyArgs),
                Muted("su_file_hint")));

            // --- options ---
            chkUserOnly = Chk("su_user_only");
            chkUserOnly.CheckedChanged += delegate { if (!loading) OnUserOnlyChanged(); };
            setupEditors.Add(chkUserOnly);
            page.Controls.Add(Card("aw_card_options", Row(chkUserOnly), Muted("su_user_hint")));

            // --- log ---
            txtLog = new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, WordWrap = false,
                Size = new Size(CardWidth - 28, 160), Margin = new Padding(0, 2, 0, 2),
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 8.5f)
            };
            Button clearLog = SmallBtn("seq_clear");
            clearLog.Click += delegate { txtLog.Clear(); };
            page.Controls.Add(Card("su_card_log", txtLog, Row(clearLog)));
        }

        private void BuildSettingsPage()
        {
            FlowLayoutPanel page = NewPage("nav_settings", "st_sub");

            cbLang = Combo(170);
            cbLang.Items.AddRange(Lang.Names);
            cbLang.SelectedIndexChanged += delegate
            {
                if (loading || cbLang.SelectedIndex < 0) return;
                settings.Set("language", Lang.Codes[cbLang.SelectedIndex]);
                Lang.Set(Lang.Codes[cbLang.SelectedIndex]);
            };
            cbTheme = Combo(170);
            cbTheme.SelectedIndexChanged += delegate { if (!loading) ApplyTheme(cbTheme.SelectedIndex == 0); };
            chkTop = Chk("st_ontop");
            chkTop.CheckedChanged += delegate { TopMost = chkTop.Checked; };
            chkTray = Chk("st_tray");
            chkAuto = Chk("st_autostart");
            chkAuto.CheckedChanged += delegate
            {
                if (loading) return;
                try { Installer.SetAutostart(chkAuto.Checked, Installer.IsInstalled ? Installer.InstalledExe : Application.ExecutablePath); }
                catch (Exception ex) { ShowError(ex); }
            };
            Label langLabel = Lbl("st_language");
            langLabel.MinimumSize = new Size(70, 0);
            Label themeLabel = Lbl("st_theme");
            themeLabel.MinimumSize = new Size(70, 0);
            page.Controls.Add(Card("st_card_general",
                Row(langLabel, cbLang),
                Row(themeLabel, cbTheme),
                Row(chkTop), Row(chkTray), Row(chkAuto),
                Muted("st_hotkeys")));

            chkDesk = Chk("st_desktop");
            btnInstall = Btn(null, "accent");
            btnInstall.Click += delegate { DoInstall(); };
            btnUninstall = Btn("st_uninstall", "danger");
            btnUninstall.Click += delegate { DoUninstall(); };
            lblInstallState = new Label { AutoSize = true, Tag = "muted", Margin = new Padding(0, 6, 0, 0), MaximumSize = new Size(CardWidth - 30, 0) };
            Label desc = Muted("st_install_desc");
            desc.Margin = new Padding(0, 0, 0, 6);
            page.Controls.Add(Card("st_card_install", desc, Row(chkDesk), Row(btnInstall, btnUninstall), lblInstallState));

            btnElevatedShortcut = Btn("uac_setup_button", "accent");
            btnElevatedShortcut.Click += delegate { ElevatedShortcut.Request(); };
            page.Controls.Add(Card("uac_setup_card", Muted("uac_setup_desc"), Row(btnElevatedShortcut)));

            lblAbout = new Label { AutoSize = true, Margin = new Padding(0, 2, 0, 4) };
            lnkGit = new LinkLabel { AutoSize = true, Margin = new Padding(0, 0, 0, 2) };
            lnkGit.LinkClicked += delegate { OpenUrl(AppInfo.RepoUrl); };
            page.Controls.Add(Card("st_card_about", lblAbout, lnkGit));
        }

        // ------------------------------------------------------------------ language & theme

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            bool was = loading;
            loading = true;
            ApplyTexts();
            loading = was;
        }

        private void ApplyTexts()
        {
            foreach (KeyValuePair<Control, string> kv in texts)
                kv.Key.Text = Lang.T(kv.Value);
            for (int i = 0; i < navItems.Count; i++)
                navItems[i].Text = Lang.T(NavKeys[i]);

            SetCombo(cbButton, "btn_left", "btn_right", "btn_middle");
            SetCombo(cbClickType, "clk_single", "clk_double");
            SetCombo(sqButton, "btn_left", "btn_right", "btn_middle");
            SetCombo(sqType, "clk_single", "clk_double");
            SetCombo(cbAction, Power.ActionKeys);
            SetCombo(cbTheme, "th_dark", "th_light");

            lvRm.Columns[0].Text = Lang.T("rm_col_msg");
            lvRm.Columns[1].Text = Lang.T("rm_col_next");
            lvRm.Columns[2].Text = Lang.T("rm_col_repeat");

            string[] suCols = { "su_col_name", "su_col_source", "su_col_status" };
            for (int i = 0; i < suCols.Length; i++)
                lvSetup.Columns[i].Text = Lang.T(suCols[i]);
            string[] srCols = { "su_col_name", "su_col_id", "su_col_version" };
            for (int i = 0; i < srCols.Length; i++)
                lvSearch.Columns[i].Text = Lang.T(srCols[i]);
            RefreshSetupList();

            string[] seqCols = { "seq_col_no", "seq_col_x", "seq_col_y", "seq_col_button", "seq_col_type", "seq_col_delay" };
            for (int i = 0; i < seqCols.Length; i++)
                lvSeq.Columns[i].Text = Lang.T(seqCols[i]);
            RefreshSequenceList(-1);
            UpdateSequenceMode();

            for (int i = 0; i < quickButtons.Count; i++)
            {
                int m = QuickMinutes[i];
                quickButtons[i].Text = m < 60 ? m + " " + Lang.T("unit_m") : (m / 60) + " " + Lang.T("unit_h");
            }

            lblAbout.Text = Lang.F("st_about", AppInfo.Version);
            lnkGit.Text = Lang.T("st_github");
            trayShow.Text = Lang.T("tr_show");
            trayExit.Text = Lang.T("tr_exit");

            UpdateRate();
            UpdateClickerUi();
            UpdateShutdownView();
            UpdateAwakeView();
            RefreshReminderList();
            UpdateInstallView();
        }

        private void ApplyTheme(bool dark)
        {
            theme = Theme.Get(dark);
            theme.Apply(this);
            if (IsHandleCreated) Native.SetDarkTitleBar(Handle, dark);
            UpdateClickerUi();
            UpdateAwakeView();
            settings.Set("theme", dark ? "dark" : "light");
        }

        private void Repaint(Control c)
        {
            if (theme != null) theme.Apply(c);
        }

        // ------------------------------------------------------------------ auto clicker

        private int IntervalMs()
        {
            return (((int)nH.Value * 60 + (int)nM.Value) * 60 + (int)nS.Value) * 1000 + (int)nMs.Value;
        }

        private void UpdateRate()
        {
            int interval = IntervalMs();
            if (interval < 1)
            {
                lblRate.Text = Lang.T("clk_bad_interval");
                return;
            }
            double rate = 1000.0 / interval;
            string format = rate >= 10 ? "0" : rate >= 1 ? "0.#" : "0.###";
            lblRate.Text = Lang.F("clk_rate", rate.ToString(format, CultureInfo.CurrentCulture));
        }

        private void ToggleClicker()
        {
            if (clickerState == ClickerState.Waiting || clickerState == ClickerState.Running || clicker.IsRunning)
            {
                clicker.Stop();
                return;
            }

            bool sequence = rbSequence.Checked;
            RepeatMode mode = rbCount.Checked ? RepeatMode.Count : rbDuration.Checked ? RepeatMode.Duration : RepeatMode.Infinite;
            int interval = IntervalMs();
            string error = null;
            if (sequence)
            {
                long roundMs = 0;
                foreach (ClickStep s in steps) roundMs += s.DelayMs;
                if (steps.Count == 0) error = "seq_empty";
                // A list with no waits at all would click at full speed forever.
                else if (roundMs < 1 && mode != RepeatMode.Count) error = "seq_zero_delay";
            }
            else if (interval < 1)
            {
                error = "clk_bad_interval";
            }
            clickerErrorKey = error;
            if (error != null)
            {
                SetClickerState(ClickerState.Idle, 0);
                return;
            }

            sequenceRun = sequence;
            ClickerOptions o = new ClickerOptions
            {
                Steps = sequence ? steps : null,
                UseFixedPosition = rbFixed.Checked,
                X = (int)nX.Value,
                Y = (int)nY.Value,
                Button = (ClickButton)Math.Max(0, cbButton.SelectedIndex),
                DoubleClick = cbClickType.SelectedIndex == 1,
                IntervalMs = interval,
                Mode = mode,
                Count = (int)nCount.Value,
                DurationSec = (int)nDuration.Value,
                DelaySec = (int)nDelay.Value,
                OwnWindow = Handle
            };
            SetClickerState(o.DelaySec > 0 ? ClickerState.Waiting : ClickerState.Running, o.DelaySec);
            clicker.Start(o);
        }

        private void SetClickerState(ClickerState state, int value)
        {
            clickerState = state;
            clickerValue = value;
            UpdateClickerUi();
        }

        private void UpdateClickerUi()
        {
            if (btnClicker == null || trayClicker == null) return;
            bool active = clickerState == ClickerState.Waiting || clickerState == ClickerState.Running;
            btnClicker.Text = Lang.T(active ? "clk_stop" : "clk_start");
            btnClicker.Tag = active ? "danger" : "accent";
            Repaint(btnClicker);
            trayClicker.Text = btnClicker.Text;

            string status;
            switch (clickerState)
            {
                case ClickerState.Waiting: status = Lang.F("clk_waiting", clickerValue); break;
                case ClickerState.Running:
                    int step = clicker.CurrentStep;
                    if (sequenceRun && step >= 0)
                    {
                        status = Lang.F("seq_running", clicker.CurrentRound, step + 1, steps.Count,
                            clickerValue.ToString("N0", CultureInfo.CurrentCulture));
                        MarkActiveStep(step);
                    }
                    else
                    {
                        status = Lang.F("clk_running", clickerValue.ToString("N0", CultureInfo.CurrentCulture));
                    }
                    break;
                case ClickerState.Done: status = Lang.F("clk_done", clickerValue.ToString("N0", CultureInfo.CurrentCulture)); break;
                default:
                    if (clickerErrorKey != null) status = Lang.T(clickerErrorKey);
                    else if (failedHotkeys.Count > 0) status = Lang.F("clk_hotkey_fail", string.Join(", ", failedHotkeys.ToArray()));
                    else status = Lang.T("clk_idle");
                    break;
            }
            lblClickerStatus.Text = status;
        }

        private void StartPick(Button source)
        {
            if (pickButton != null && pickButton != source) pickButton.Text = Lang.T("clk_pick");
            pickButton = source;
            pickLeft = 3;
            source.Text = Lang.F("clk_picking", pickLeft);
            pickTimer.Stop();
            pickTimer.Start();
        }

        private void OnPickTick(object sender, EventArgs e)
        {
            pickLeft--;
            if (pickLeft > 0)
            {
                pickButton.Text = Lang.F("clk_picking", pickLeft);
                return;
            }
            pickTimer.Stop();
            pickButton.Text = Lang.T("clk_pick");
            Native.POINT p;
            if (!Native.GetCursorPos(out p)) return;
            if (pickButton == sqPick)
            {
                // The 3-second pick in the list card only fills the X/Y boxes, so the
                // wait and button can still be adjusted before pressing "Add".
                sqX.Value = Clamp(sqX, p.X);
                sqY.Value = Clamp(sqY, p.Y);
                SystemSounds.Asterisk.Play();
            }
            else
            {
                CaptureCursor();
            }
        }

        /// <summary>F8: in list mode adds the cursor position as a new step, otherwise sets the fixed position.</summary>
        private void CaptureCursor()
        {
            Native.POINT p;
            if (!Native.GetCursorPos(out p)) return;
            if (rbSequence.Checked)
            {
                sqX.Value = Clamp(sqX, p.X);
                sqY.Value = Clamp(sqY, p.Y);
                AddStep();
                SystemSounds.Asterisk.Play();
                return;
            }
            loading = true;
            nX.Value = Clamp(nX, p.X);
            nY.Value = Clamp(nY, p.Y);
            loading = false;
            rbFixed.Checked = true;
            SystemSounds.Asterisk.Play();
        }

        // ------------------------------------------------------------------ click sequence

        private void UpdateSequenceMode()
        {
            if (cardSeq == null || cardClick == null) return;
            bool seq = rbSequence.Checked;
            cardSeq.Visible = seq;
            cardClick.Visible = !seq;
            rbCount.Text = Lang.T(seq ? "seq_rep_rounds" : "clk_rep_count");
        }

        private ClickStep StepFromEditor()
        {
            return new ClickStep
            {
                X = (int)sqX.Value,
                Y = (int)sqY.Value,
                Button = (ClickButton)Math.Max(0, sqButton.SelectedIndex),
                DoubleClick = sqType.SelectedIndex == 1,
                DelayMs = (int)sqDelay.Value
            };
        }

        private int SelectedStep()
        {
            return lvSeq.SelectedIndices.Count > 0 ? lvSeq.SelectedIndices[0] : -1;
        }

        private void RefreshSequenceList(int select)
        {
            if (lvSeq == null) return;
            string[] buttons = { Lang.T("btn_left"), Lang.T("btn_right"), Lang.T("btn_middle") };
            if (select < 0) select = SelectedStep();

            lvSeq.BeginUpdate();
            lvSeq.Items.Clear();
            long roundMs = 0;
            for (int i = 0; i < steps.Count; i++)
            {
                ClickStep s = steps[i];
                roundMs += s.DelayMs;
                lvSeq.Items.Add(new ListViewItem(new[]
                {
                    (i + 1).ToString(CultureInfo.CurrentCulture),
                    s.X.ToString(CultureInfo.CurrentCulture),
                    s.Y.ToString(CultureInfo.CurrentCulture),
                    buttons[(int)s.Button],
                    Lang.T(s.DoubleClick ? "clk_double" : "clk_single"),
                    s.DelayMs.ToString("N0", CultureInfo.CurrentCulture) + " " + Lang.T("unit_ms")
                }));
            }
            if (select >= 0 && select < steps.Count)
                SelectStepRow(select);
            lvSeq.EndUpdate();

            string info = steps.Count == 0
                ? Lang.T("seq_info_empty")
                : Lang.F("seq_info", steps.Count, FormatDuration(roundMs));
            if (seqFile != null) info += " · " + System.IO.Path.GetFileName(seqFile);
            lblSeqInfo.Text = info;
        }

        private static string FormatDuration(long ms)
        {
            if (ms < 1000) return ms.ToString(CultureInfo.CurrentCulture) + " " + Lang.T("unit_ms");
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            if (t.TotalMinutes < 1) return t.TotalSeconds.ToString("0.#", CultureInfo.CurrentCulture) + " " + Lang.T("unit_s");
            return FormatSpan(t);
        }

        private void OnSequenceSelected()
        {
            int i = SelectedStep();
            if (loading || i < 0 || i >= steps.Count) return;
            ClickStep s = steps[i];
            sqX.Value = Clamp(sqX, s.X);
            sqY.Value = Clamp(sqY, s.Y);
            sqButton.SelectedIndex = (int)s.Button;
            sqType.SelectedIndex = s.DoubleClick ? 1 : 0;
            sqDelay.Value = Clamp(sqDelay, s.DelayMs);
        }

        private void MarkActiveStep(int index)
        {
            if (lvSeq == null || index < 0 || index >= lvSeq.Items.Count || SelectedStep() == index) return;
            SelectStepRow(index);
        }

        /// <summary>Selects a row without copying it into the editor boxes.</summary>
        private void SelectStepRow(int index)
        {
            bool was = loading;
            loading = true;
            lvSeq.Items[index].Selected = true;
            lvSeq.Items[index].Focused = true;
            loading = was;
            lvSeq.EnsureVisible(index);
        }

        private void AddStep()
        {
            steps.Add(StepFromEditor());
            RefreshSequenceList(steps.Count - 1);
        }

        private void UpdateStep()
        {
            int i = SelectedStep();
            if (i < 0) return;
            steps[i] = StepFromEditor();
            RefreshSequenceList(i);
        }

        private void RemoveStep()
        {
            int i = SelectedStep();
            if (i < 0) return;
            steps.RemoveAt(i);
            RefreshSequenceList(Math.Min(i, steps.Count - 1));
        }

        private void MoveStep(int direction)
        {
            int i = SelectedStep();
            int j = i + direction;
            if (i < 0 || j < 0 || j >= steps.Count) return;
            ClickStep tmp = steps[i];
            steps[i] = steps[j];
            steps[j] = tmp;
            RefreshSequenceList(j);
        }

        private void ClearSteps()
        {
            if (steps.Count == 0) return;
            if (MessageBox.Show(this, Lang.T("seq_clear_confirm"), AppInfo.Name,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            steps.Clear();
            seqFile = null;
            RefreshSequenceList(-1);
        }

        private const string SeqFilter = "JSON (*.json)|*.json|*.*|*.*";

        private void SaveSequenceFile()
        {
            if (steps.Count == 0)
            {
                MessageBox.Show(this, Lang.T("seq_empty"), AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (SaveFileDialog d = new SaveFileDialog())
            {
                d.Title = Lang.T("seq_save_title");
                d.Filter = SeqFilter;
                d.DefaultExt = "json";
                d.AddExtension = true;
                d.FileName = seqFile != null ? System.IO.Path.GetFileName(seqFile) : "clicks.json";
                if (seqFile != null) d.InitialDirectory = System.IO.Path.GetDirectoryName(seqFile);
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    Sequence.SaveFile(d.FileName, steps);
                }
                catch (Exception ex)
                {
                    if (!(ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException)) throw;
                    ShowError(ex);
                    return;
                }
                seqFile = d.FileName;
                RefreshSequenceList(-1);
            }
        }

        private void LoadSequenceFile()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = Lang.T("seq_load_title");
                d.Filter = SeqFilter;
                if (seqFile != null) d.InitialDirectory = System.IO.Path.GetDirectoryName(seqFile);
                if (d.ShowDialog(this) != DialogResult.OK) return;

                List<ClickStep> loaded;
                try
                {
                    loaded = Sequence.LoadFile(d.FileName);
                }
                catch (Exception ex)
                {
                    if (!(ex is FormatException || ex is System.IO.IOException || ex is UnauthorizedAccessException ||
                          ex is System.Security.SecurityException)) throw;
                    MessageBox.Show(this, Lang.F("seq_load_fail", ex.Message), Lang.T("err"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                steps.Clear();
                steps.AddRange(loaded);
                seqFile = d.FileName;
                rbSequence.Checked = true;
                RefreshSequenceList(steps.Count > 0 ? 0 : -1);
                OnSequenceSelected();
            }
        }

        private void UpdateLivePosition()
        {
            if (!Visible || WindowState == FormWindowState.Minimized || !pages[0].Visible) return;
            Native.POINT p;
            if (Native.GetCursorPos(out p))
                lblLivePos.Text = Lang.F("clk_live", p.X, p.Y);
        }

        // ------------------------------------------------------------------ shutdown timer

        private static string FormatSpan(TimeSpan t)
        {
            if (t < TimeSpan.Zero) t = TimeSpan.Zero;
            return string.Format("{0:00}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds);
        }

        private string ActionName(PowerAction a)
        {
            return Lang.T(Power.ActionKeys[(int)a]);
        }

        private void StartShutdownTimer()
        {
            DateTime due;
            if (rbAt.Checked)
            {
                due = DateTime.Today.AddHours((int)atH.Value).AddMinutes((int)atM.Value);
                if (due <= DateTime.Now) due = due.AddDays(1);
            }
            else
            {
                TimeSpan span = new TimeSpan((int)sdH.Value, (int)sdM.Value, (int)sdS.Value);
                if (span.TotalSeconds < 1)
                {
                    MessageBox.Show(this, Lang.T("sd_zero"), AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                due = DateTime.Now + span;
            }

            sdDue = due;
            sdAction = (PowerAction)Math.Max(0, cbAction.SelectedIndex);
            sdForce = chkForce.Checked;
            warnShown = false;
            UpdateShutdownView();
            SaveSettings();
        }

        private void CancelShutdownTimer()
        {
            sdDue = null;
            CloseWarning();
            UpdateShutdownView();
        }

        private void FireShutdown()
        {
            PowerAction action = sdAction;
            bool force = sdForce;
            sdDue = null;
            CloseWarning();
            UpdateShutdownView();
            SaveSettings();
            try { Power.Execute(action, force); }
            catch (Exception ex) { ShowError(ex); }
        }

        private void UpdateShutdownView()
        {
            if (lblSdBig == null || tray == null) return;
            if (sdDue.HasValue)
            {
                DateTime due = sdDue.Value;
                lblSdBig.Text = FormatSpan(due - DateTime.Now);
                string when = due.ToString(due.Date == DateTime.Today ? "HH:mm:ss" : "dd.MM HH:mm", CultureInfo.CurrentCulture);
                lblSdInfo.Text = Lang.F("sd_scheduled", ActionName(sdAction), when);
                string tip = AppInfo.Name + " · " + lblSdInfo.Text;
                tray.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip;
            }
            else
            {
                lblSdBig.Text = "00:00:00";
                lblSdInfo.Text = Lang.T("sd_idle");
                tray.Text = AppInfo.Name;
            }
            btnSdStart.Visible = !sdDue.HasValue;
            btnSdCancel.Visible = sdDue.HasValue;
        }

        private void ShowWarning(int secondsLeft)
        {
            if (warnForm == null && !warnShown)
            {
                warnShown = true;
                warnForm = new WarningForm(theme, appIcon);
                warnForm.CancelClicked += delegate { CancelShutdownTimer(); };
                warnForm.NowClicked += delegate { FireShutdown(); };
                warnForm.FormClosed += delegate { warnForm = null; };
                warnForm.ShowState(ActionName(sdAction), secondsLeft);
                warnForm.Show();
                SystemSounds.Exclamation.Play();
            }
            else if (warnForm != null)
            {
                warnForm.ShowState(ActionName(sdAction), secondsLeft);
            }
        }

        private void CloseWarning()
        {
            if (warnForm != null)
            {
                WarningForm f = warnForm;
                warnForm = null;
                f.Close();
            }
        }

        // ------------------------------------------------------------------ keep awake

        private void StartAwake()
        {
            awake.Enable(chkDisplay.Checked);
            awUntil = rbAwFor.Checked ? DateTime.Now.AddMinutes((int)nAwMin.Value) : (DateTime?)null;
            jiggleTick = 0;
            UpdateAwakeView();
        }

        private void StopAwake()
        {
            awake.Disable();
            awUntil = null;
            UpdateAwakeView();
        }

        private void UpdateAwakeView()
        {
            if (lblAwBig == null) return;
            lblAwBig.Text = Lang.T(awake.Active ? "aw_on" : "aw_off");
            lblAwBig.Tag = awake.Active ? "accenttext" : null;
            Repaint(lblAwBig);
            if (awake.Active)
                lblAwInfo.Text = awUntil.HasValue
                    ? Lang.F("aw_until", awUntil.Value.ToString("HH:mm", CultureInfo.CurrentCulture))
                    : Lang.T("aw_forever");
            else
                lblAwInfo.Text = "";
            btnAwake.Text = Lang.T(awake.Active ? "aw_disable" : "aw_enable");
            btnAwake.Tag = awake.Active ? "danger" : "accent";
            Repaint(btnAwake);
        }

        // ------------------------------------------------------------------ reminders

        private void AddReminder()
        {
            string message = txtRm.Text.Trim();
            if (message.Length == 0) message = Lang.T("rm_default");
            reminders.Add(new Reminder
            {
                Message = message,
                Minutes = (int)nRmMin.Value,
                Repeat = chkRmRepeat.Checked,
                Sound = chkRmSound.Checked,
                Next = DateTime.Now.AddMinutes((int)nRmMin.Value)
            });
            txtRm.Clear();
            RefreshReminderList();
        }

        private void RemoveSelectedReminders()
        {
            foreach (ListViewItem item in lvRm.SelectedItems)
                reminders.Remove((Reminder)item.Tag);
            RefreshReminderList();
        }

        private void CheckReminders()
        {
            DateTime now = DateTime.Now;
            bool changed = false;
            for (int i = reminders.Count - 1; i >= 0; i--)
            {
                Reminder r = reminders[i];
                if (now < r.Next) continue;
                new ToastForm(theme, Lang.T("rm_title"), r.Message).Show();
                if (r.Sound) SystemSounds.Asterisk.Play();
                if (r.Repeat) r.Next = now.AddMinutes(r.Minutes);
                else reminders.RemoveAt(i);
                changed = true;
            }
            if (changed) RefreshReminderList();
        }

        private void RefreshReminderList()
        {
            if (lvRm == null) return;
            lvRm.BeginUpdate();
            lvRm.Items.Clear();
            foreach (Reminder r in reminders)
            {
                ListViewItem item = new ListViewItem(r.Message);
                item.SubItems.Add(r.Next.ToString("HH:mm:ss", CultureInfo.CurrentCulture));
                item.SubItems.Add(r.Repeat ? Lang.F("rm_every", r.Minutes) : Lang.T("rm_once"));
                item.Tag = r;
                lvRm.Items.Add(item);
            }
            lvRm.EndUpdate();
        }

        // ------------------------------------------------------------------ bulk install

        private string SetupStatusText(SetupItem it)
        {
            if (it.Status == SetupStatus.None) return "";
            string text = Lang.T("su_st_" + it.Status.ToString().ToLowerInvariant());
            return string.IsNullOrEmpty(it.Detail) ? text : text + " · " + it.Detail;
        }

        private void RefreshSetupList()
        {
            if (lvSetup == null) return;
            setupFilling = true;
            lvSetup.BeginUpdate();
            lvSetup.Items.Clear();
            foreach (SetupItem it in setupItems)
            {
                ListViewItem row = new ListViewItem(new[] { it.Name, it.Source, SetupStatusText(it) });
                row.Checked = it.Checked;
                row.ToolTipText = it.IsFile ? it.FilePath + "  " + it.Args : it.WingetId;
                lvSetup.Items.Add(row);
            }
            lvSetup.EndUpdate();
            setupFilling = false;
            UpdateSetupUi();
        }

        private void UpdateSetupRow(SetupItem it)
        {
            int i = setupItems.IndexOf(it);
            if (i < 0 || i >= lvSetup.Items.Count) return;
            lvSetup.Items[i].SubItems[2].Text = SetupStatusText(it);
            if (it.Status == SetupStatus.Running) lvSetup.EnsureVisible(i);
        }

        private void UpdateSetupUi()
        {
            if (btnSetup == null || lblSetupStatus == null) return;
            bool running = setup.IsRunning;
            btnSetup.Text = Lang.T(running ? "su_stop" : "su_start");
            btnSetup.Tag = running ? "danger" : "accent";
            Repaint(btnSetup);
            foreach (Control c in setupEditors) c.Enabled = !running;
            txtArgs.Enabled = !running && SelectedSetupFile() != null;

            int checkedCount = 0, total = 0, finished = 0, ok = 0, already = 0, failed = 0;
            SetupItem current = null;
            foreach (SetupItem it in setupItems)
            {
                if (it.Checked) checkedCount++;
                if (it.Status == SetupStatus.None) continue;
                total++;
                if (it.Status == SetupStatus.Running) current = it;
                else if (it.Status != SetupStatus.Waiting) finished++;
                if (it.Status == SetupStatus.Installed || it.Status == SetupStatus.InstalledRestart) ok++;
                else if (it.Status == SetupStatus.AlreadyInstalled) already++;
                else if (it.Status != SetupStatus.Running && it.Status != SetupStatus.Waiting) failed++;
            }

            if (running)
            {
                string name = current != null ? current.Name : "";
                lblSetupStatus.Text = setup.StopRequested
                    ? Lang.F("su_stopping", name)
                    : Lang.F("su_running", finished + 1, total, name);
            }
            else if (setupRan && total > 0)
            {
                lblSetupStatus.Text = Lang.F("su_summary", ok, already, failed);
            }
            else
            {
                lblSetupStatus.Text = Lang.F("su_idle", checkedCount);
            }
        }

        private void ToggleSetup()
        {
            if (setup.IsRunning)
            {
                setup.Stop();
                UpdateSetupUi();
                return;
            }

            List<SetupItem> queue = setupItems.FindAll(delegate(SetupItem it) { return it.Checked; });
            if (queue.Count == 0)
            {
                MessageBox.Show(this, Lang.T("su_nothing"), AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            bool needsWinget = queue.Exists(delegate(SetupItem it) { return !it.IsFile; });
            if (needsWinget)
            {
                Cursor = Cursors.WaitCursor;
                bool ok = SetupRunner.WingetAvailable();
                Cursor = Cursors.Default;
                if (!ok)
                {
                    if (MessageBox.Show(this, Lang.T("su_no_winget"), AppInfo.Name,
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        OpenUrl("ms-windows-store://pdp/?productid=9NBLGGH4NNS1");
                    return;
                }
            }

            foreach (SetupItem it in setupItems)
            {
                it.Status = SetupStatus.None;
                it.Detail = null;
            }
            setupRan = true;
            AppendSetupLog("=== " + DateTime.Now.ToString("g", CultureInfo.CurrentCulture) + " · " +
                           Lang.F("su_log_start", queue.Count) + (chkUserOnly.Checked ? " · " + Lang.T("su_user_only") : ""));
            setup.Start(queue, chkUserOnly.Checked);
            RefreshSetupList();
        }

        private void AppendSetupLog(string line)
        {
            if (txtLog.TextLength > 300000) txtLog.Text = txtLog.Text.Substring(150000);
            txtLog.AppendText(line + "\r\n");
        }

        private SetupItem SelectedSetupFile()
        {
            if (lvSetup.SelectedIndices.Count != 1) return null;
            int i = lvSetup.SelectedIndices[0];
            return i < setupItems.Count && setupItems[i].IsFile ? setupItems[i] : null;
        }

        private void OnSetupSelected()
        {
            SetupItem it = SelectedSetupFile();
            txtArgs.Text = it != null ? it.Args ?? "" : "";
            txtArgs.Enabled = it != null && !setup.IsRunning;
        }

        private void ApplySetupArgs()
        {
            SetupItem it = SelectedSetupFile();
            if (it == null) return;
            it.Args = txtArgs.Text.Trim();
            int i = setupItems.IndexOf(it);
            lvSetup.Items[i].ToolTipText = it.FilePath + "  " + it.Args;
        }

        private void CheckAllSetup(bool value)
        {
            foreach (SetupItem it in setupItems) it.Checked = value;
            RefreshSetupList();
        }

        private void RemoveSetupItems()
        {
            List<SetupItem> remove = new List<SetupItem>();
            foreach (int i in lvSetup.SelectedIndices)
                if (i < setupItems.Count) remove.Add(setupItems[i]);
            if (remove.Count == 0) return;
            // Built-in programs stay in the list; removing them just unticks them.
            foreach (SetupItem it in remove)
            {
                if (it.Builtin) it.Checked = false;
                else setupItems.Remove(it);
            }
            RefreshSetupList();
        }

        private void AddSetupItem(SetupItem item)
        {
            SetupList.Merge(setupItems, new[] { item });
            RefreshSetupList();
            int i = setupItems.FindIndex(delegate(SetupItem t)
            {
                return item.IsFile
                    ? t.IsFile && string.Equals(t.FilePath, item.FilePath, StringComparison.OrdinalIgnoreCase)
                    : !t.IsFile && string.Equals(t.WingetId, item.WingetId, StringComparison.OrdinalIgnoreCase);
            });
            if (i >= 0)
            {
                lvSetup.SelectedIndices.Clear();
                lvSetup.Items[i].Selected = true;
                lvSetup.EnsureVisible(i);
            }
        }

        private void SearchWinget()
        {
            string query = txtSearch.Text.Trim();
            if (query.Length < 2 || !btnSearch.Enabled) return;
            btnSearch.Enabled = false;
            btnSearch.Text = Lang.T("su_searching");
            lvSearch.Items.Clear();

            System.Threading.Thread t = new System.Threading.Thread(delegate()
            {
                List<WingetHit> hits = null;
                string error = null;
                try { hits = SetupRunner.Search(query); }
                catch (System.ComponentModel.Win32Exception) { error = Lang.T("su_no_winget_short"); }
                catch (InvalidOperationException ex) { error = ex.Message; }
                Ui(delegate
                {
                    btnSearch.Text = Lang.T("su_search");
                    btnSearch.Enabled = !setup.IsRunning;
                    if (error != null)
                    {
                        MessageBox.Show(this, error, Lang.T("err"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    lvSearch.BeginUpdate();
                    foreach (WingetHit h in hits)
                        lvSearch.Items.Add(new ListViewItem(new[] { h.Name, h.Id, h.Version }));
                    if (hits.Count == 0)
                        lvSearch.Items.Add(new ListViewItem(new[] { Lang.T("su_no_results"), "", "" }));
                    lvSearch.EndUpdate();
                });
            });
            t.IsBackground = true;
            t.Start();
        }

        private void AddWingetFromSearch()
        {
            bool added = false;
            foreach (ListViewItem row in lvSearch.SelectedItems)
            {
                string id = row.SubItems[1].Text;
                if (id.Length == 0) continue;
                AddSetupItem(new SetupItem { Name = row.SubItems[0].Text, WingetId = id, Checked = true });
                added = true;
            }
            // A typed package id (for example "Google.Chrome") can be added without searching.
            string typed = txtSearch.Text.Trim();
            if (!added && typed.IndexOf('.') > 0 && typed.IndexOf(' ') < 0)
                AddSetupItem(new SetupItem { Name = typed, WingetId = typed, Checked = true });
        }

        private const string InstallerFilter =
            "Setup (*.exe;*.msi;*.msix;*.msixbundle;*.appx;*.appxbundle)|*.exe;*.msi;*.msix;*.msixbundle;*.appx;*.appxbundle|*.*|*.*";

        private void AddSetupFiles()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = Lang.T("su_add_file_title");
                d.Filter = InstallerFilter;
                d.Multiselect = true;
                if (d.ShowDialog(this) != DialogResult.OK) return;
                Cursor = Cursors.WaitCursor;
                foreach (string path in d.FileNames)
                    AddSetupItem(SetupCatalog.FromFile(path, chkUserOnly.Checked));
                Cursor = Cursors.Default;
                OnSetupSelected();
            }
        }

        /// <summary>Switches detected installer arguments between per-user and machine-wide forms.</summary>
        private void OnUserOnlyChanged()
        {
            bool now = chkUserOnly.Checked;
            foreach (SetupItem it in setupItems)
                if (it.IsFile && it.Args == SetupCatalog.DetectArgs(it.FilePath, !now))
                    it.Args = SetupCatalog.DetectArgs(it.FilePath, now);
            OnSetupSelected();
        }

        private void SaveSetupList()
        {
            using (SaveFileDialog d = new SaveFileDialog())
            {
                d.Title = Lang.T("su_save_title");
                d.Filter = SeqFilter;
                d.DefaultExt = "json";
                d.AddExtension = true;
                d.FileName = Lang.T("su_default_file");
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    System.IO.File.WriteAllText(d.FileName, SetupList.ToJson(setupItems, true), new System.Text.UTF8Encoding(false));
                }
                catch (Exception ex)
                {
                    if (!(ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException)) throw;
                    ShowError(ex);
                }
            }
        }

        private void LoadSetupList()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = Lang.T("su_load_title");
                d.Filter = SeqFilter;
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    List<SetupItem> loaded = SetupList.FromJson(System.IO.File.ReadAllText(d.FileName, System.Text.Encoding.UTF8));
                    // A loaded list replaces the current selection.
                    foreach (SetupItem it in setupItems) it.Checked = false;
                    SetupList.Merge(setupItems, loaded);
                }
                catch (Exception ex)
                {
                    if (!(ex is FormatException || ex is System.IO.IOException || ex is UnauthorizedAccessException ||
                          ex is System.Security.SecurityException)) throw;
                    MessageBox.Show(this, Lang.F("seq_load_fail", ex.Message), Lang.T("err"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                RefreshSetupList();
            }
        }

        // ------------------------------------------------------------------ install

        private void UpdateInstallView()
        {
            if (btnInstall == null) return;
            bool installed = Installer.IsInstalled;
            btnInstall.Text = Lang.T(installed ? "st_reinstall" : "st_install");
            btnUninstall.Visible = installed;
            lblInstallState.Text = installed ? Lang.F("st_installed", Installer.InstallDir) : Lang.T("st_portable");
        }

        private void DoInstall()
        {
            try { Installer.Install(chkDesk.Checked); }
            catch (Exception ex) { ShowError(ex); return; }
            UpdateInstallView();

            if (Installer.IsRunningInstalled)
            {
                MessageBox.Show(this, Lang.T("st_install_done"), AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(this, Lang.T("st_install_ok"), AppInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SaveSettings();
                Process.Start(new ProcessStartInfo(Installer.InstalledExe, "--after-install"));
                ExitApp(true);
            }
        }

        private void DoUninstall()
        {
            if (MessageBox.Show(this, Lang.T("st_uninstall_confirm"), AppInfo.Name,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            bool runningInstalled = Installer.IsRunningInstalled;
            try { Installer.Uninstall(); }
            catch (Exception ex) { ShowError(ex); return; }

            loading = true;
            chkAuto.Checked = false;
            loading = false;

            if (runningInstalled)
            {
                skipSave = true;
                ExitApp(false);
                return;
            }
            UpdateInstallView();
            MessageBox.Show(this, Lang.T("st_uninstall_ok"), AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ------------------------------------------------------------------ timers, window & tray

        private void OnSecondTick(object sender, EventArgs e)
        {
            if (sdDue.HasValue)
            {
                int left = (int)Math.Ceiling((sdDue.Value - DateTime.Now).TotalSeconds);
                if (left <= 0)
                {
                    FireShutdown();
                }
                else
                {
                    if (chkWarn.Checked && left <= 60) ShowWarning(left);
                    UpdateShutdownView();
                }
            }

            if (awake.Active)
            {
                if (awUntil.HasValue && DateTime.Now >= awUntil.Value)
                {
                    StopAwake();
                }
                else
                {
                    awake.Refresh();
                    if (chkJiggle.Checked && ++jiggleTick >= 60)
                    {
                        jiggleTick = 0;
                        KeepAwake.NudgeMouse();
                    }
                }
            }

            CheckReminders();
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!allowShow)
            {
                value = false;
                if (!IsHandleCreated) CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            failedHotkeys.Clear();
            if (!Native.RegisterHotKey(Handle, HotkeyClicker, Native.MOD_NOREPEAT, Native.VK_F6)) failedHotkeys.Add("F6");
            if (!Native.RegisterHotKey(Handle, HotkeyCapture, Native.MOD_NOREPEAT, Native.VK_F8)) failedHotkeys.Add("F8");
            if (theme != null) Native.SetDarkTitleBar(Handle, theme.Dark);
            UpdateClickerUi();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Native.UnregisterHotKey(Handle, HotkeyClicker);
            Native.UnregisterHotKey(Handle, HotkeyCapture);
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HotkeyClicker) ToggleClicker();
                else if (id == HotkeyCapture) CaptureCursor();
                return;
            }
            uint msg = unchecked((uint)m.Msg);
            if (msg != 0 && msg == Program.ShowMessage)
            {
                ShowFromTray();
                return;
            }
            if (msg != 0 && msg == Program.ExitMessage)
            {
                skipSave = true;
                ExitApp(false);
                return;
            }
            base.WndProc(ref m);
        }

        private void ShowFromTray()
        {
            allowShow = true;
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
            Native.SetForegroundWindow(Handle);
        }

        private void ExitApp(bool ask)
        {
            if (ask && !ConfirmExit()) return;
            exiting = true;
            Close();
        }

        /// <summary>Asks before quitting while a shutdown timer or a bulk install is running.</summary>
        private bool ConfirmExit()
        {
            string key = setup.IsRunning ? "su_exit_confirm" : sdDue.HasValue ? "sd_exit_confirm" : null;
            return key == null || MessageBox.Show(this, Lang.T(key), AppInfo.Name,
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!exiting && e.CloseReason == CloseReason.UserClosing)
            {
                if (chkTray.Checked)
                {
                    e.Cancel = true;
                    Hide();
                    if (!trayHintShown)
                    {
                        trayHintShown = true;
                        tray.ShowBalloonTip(3000, AppInfo.Name, Lang.T("tr_running"), ToolTipIcon.Info);
                    }
                    return;
                }
                if (!ConfirmExit())
                {
                    e.Cancel = true;
                    return;
                }
            }
            CleanUp();
            base.OnFormClosing(e);
        }

        private void CleanUp()
        {
            if (cleanedUp) return;
            cleanedUp = true;
            Lang.Changed -= OnLanguageChanged;
            clicker.Stop();
            if (awake.Active) awake.Disable();
            secondTimer.Stop();
            fastTimer.Stop();
            pickTimer.Stop();
            CloseWarning();
            if (!skipSave) SaveSettings();
            tray.Visible = false;
            tray.Dispose();
        }

        // ------------------------------------------------------------------ settings

        private void LoadSettings()
        {
            Settings s = settings;
            cbLang.SelectedIndex = Lang.Index;
            cbTheme.SelectedIndex = s.Get("theme", "dark") == "light" ? 1 : 0;
            chkTop.Checked = s.GetBool("topmost", false);
            chkTray.Checked = s.GetBool("tray", true);
            chkAuto.Checked = Installer.IsAutostart;
            chkDesk.Checked = s.GetBool("desktop", true);

            bool fixedPos = s.GetBool("clk_fixed", false);
            bool seqPos = s.GetBool("clk_seq", false);
            rbFixed.Checked = fixedPos && !seqPos;
            rbCursor.Checked = !fixedPos && !seqPos;
            rbSequence.Checked = seqPos;
            steps.Clear();
            steps.AddRange(Sequence.FromSettings(s.Get("seq_steps", "")));
            string file = s.Get("seq_file", "");
            seqFile = file.Length > 0 ? file : null;
            sqButton.SelectedIndex = Math.Max(0, Math.Min(2, s.GetInt("seq_btn", 0)));
            sqType.SelectedIndex = s.GetInt("seq_type", 0) == 1 ? 1 : 0;
            sqDelay.Value = Clamp(sqDelay, s.GetInt("seq_delay", 500));
            RefreshSequenceList(-1);
            UpdateSequenceMode();
            nX.Value = Clamp(nX, s.GetInt("clk_x", 0));
            nY.Value = Clamp(nY, s.GetInt("clk_y", 0));
            cbButton.SelectedIndex = Math.Max(0, Math.Min(2, s.GetInt("clk_btn", 0)));
            cbClickType.SelectedIndex = s.GetInt("clk_type", 0) == 1 ? 1 : 0;
            nH.Value = Clamp(nH, s.GetInt("clk_h", 0));
            nM.Value = Clamp(nM, s.GetInt("clk_m", 0));
            nS.Value = Clamp(nS, s.GetInt("clk_s", 0));
            nMs.Value = Clamp(nMs, s.GetInt("clk_ms", 100));
            int mode = s.GetInt("clk_mode", 2);
            rbCount.Checked = mode == 0;
            rbDuration.Checked = mode == 1;
            rbInfinite.Checked = mode != 0 && mode != 1;
            nCount.Value = Clamp(nCount, s.GetInt("clk_count", 100));
            nDuration.Value = Clamp(nDuration, s.GetInt("clk_duration", 60));
            nDelay.Value = Clamp(nDelay, s.GetInt("clk_delay", 2));

            cbAction.SelectedIndex = Math.Max(0, Math.Min(Power.ActionKeys.Length - 1, s.GetInt("sd_action", 0)));
            bool at = s.GetInt("sd_mode", 0) == 1;
            rbAt.Checked = at;
            rbAfter.Checked = !at;
            sdH.Value = Clamp(sdH, s.GetInt("sd_h", 1));
            sdM.Value = Clamp(sdM, s.GetInt("sd_m", 0));
            sdS.Value = Clamp(sdS, s.GetInt("sd_s", 0));
            atH.Value = Clamp(atH, s.GetInt("sd_at_h", 23));
            atM.Value = Clamp(atM, s.GetInt("sd_at_m", 0));
            chkForce.Checked = s.GetBool("sd_force", false);
            chkWarn.Checked = s.GetBool("sd_warn", true);

            chkDisplay.Checked = s.GetBool("aw_display", true);
            chkJiggle.Checked = s.GetBool("aw_jiggle", false);
            bool awFor = s.GetInt("aw_mode", 0) == 1;
            rbAwFor.Checked = awFor;
            rbAwForever.Checked = !awFor;
            nAwMin.Value = Clamp(nAwMin, s.GetInt("aw_min", 60));

            nRmMin.Value = Clamp(nRmMin, s.GetInt("rm_min", 30));
            chkRmRepeat.Checked = s.GetBool("rm_repeat", false);
            chkRmSound.Checked = s.GetBool("rm_sound", true);

            chkUserOnly.Checked = s.GetBool("su_user", true);
            setupItems.Clear();
            setupItems.AddRange(SetupCatalog.Create());
            try { SetupList.Merge(setupItems, SetupList.FromJson(s.Get("su_list", "{\"items\":[]}"))); }
            catch (FormatException) { }
            RefreshSetupList();

            TopMost = chkTop.Checked;
        }

        private void SaveSettings()
        {
            Settings s = settings;
            s.Set("language", Lang.Code);
            s.Set("theme", cbTheme.SelectedIndex == 1 ? "light" : "dark");
            s.Set("topmost", chkTop.Checked);
            s.Set("tray", chkTray.Checked);
            s.Set("desktop", chkDesk.Checked);

            s.Set("clk_fixed", rbFixed.Checked);
            s.Set("clk_seq", rbSequence.Checked);
            s.Set("seq_steps", Sequence.ToSettings(steps));
            s.Set("seq_file", seqFile ?? "");
            s.Set("seq_btn", sqButton.SelectedIndex);
            s.Set("seq_type", sqType.SelectedIndex);
            s.Set("seq_delay", sqDelay.Value);
            s.Set("clk_x", nX.Value);
            s.Set("clk_y", nY.Value);
            s.Set("clk_btn", cbButton.SelectedIndex);
            s.Set("clk_type", cbClickType.SelectedIndex);
            s.Set("clk_h", nH.Value);
            s.Set("clk_m", nM.Value);
            s.Set("clk_s", nS.Value);
            s.Set("clk_ms", nMs.Value);
            s.Set("clk_mode", rbCount.Checked ? 0 : rbDuration.Checked ? 1 : 2);
            s.Set("clk_count", nCount.Value);
            s.Set("clk_duration", nDuration.Value);
            s.Set("clk_delay", nDelay.Value);

            s.Set("sd_action", cbAction.SelectedIndex);
            s.Set("sd_mode", rbAt.Checked ? 1 : 0);
            s.Set("sd_h", sdH.Value);
            s.Set("sd_m", sdM.Value);
            s.Set("sd_s", sdS.Value);
            s.Set("sd_at_h", atH.Value);
            s.Set("sd_at_m", atM.Value);
            s.Set("sd_force", chkForce.Checked);
            s.Set("sd_warn", chkWarn.Checked);

            s.Set("aw_display", chkDisplay.Checked);
            s.Set("aw_jiggle", chkJiggle.Checked);
            s.Set("aw_mode", rbAwFor.Checked ? 1 : 0);
            s.Set("aw_min", nAwMin.Value);

            s.Set("rm_min", nRmMin.Value);
            s.Set("rm_repeat", chkRmRepeat.Checked);
            s.Set("rm_sound", chkRmSound.Checked);

            s.Set("su_user", chkUserOnly.Checked);
            s.Set("su_list", SetupList.ToJson(setupItems, false));
            s.Save();
        }

        // ------------------------------------------------------------------ misc

        private void ShowError(Exception ex)
        {
            MessageBox.Show(this, ex.Message, Lang.T("err"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception) { }
        }
    }
}

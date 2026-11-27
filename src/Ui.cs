using System;
using System.Drawing;
using System.Windows.Forms;

namespace Companian
{
    /// <summary>Color palette and a recursive painter. Controls opt into roles through their Tag string.</summary>
    internal sealed class Theme
    {
        public bool Dark;
        public Color Back, Surface, Nav, NavHover, NavActive, Text, Muted, Accent, AccentHover, Danger, DangerHover, Input, Border, ButtonBack;

        public static Theme Get(bool dark)
        {
            Theme t = new Theme();
            t.Dark = dark;
            if (dark)
            {
                t.Back = Color.FromArgb(19, 23, 35);
                t.Surface = Color.FromArgb(29, 35, 51);
                t.Nav = Color.FromArgb(16, 21, 34);
                t.NavHover = Color.FromArgb(29, 38, 58);
                t.NavActive = Color.FromArgb(38, 46, 72);
                t.Text = Color.FromArgb(239, 241, 250);
                t.Muted = Color.FromArgb(166, 176, 198);
                t.Accent = Color.FromArgb(107, 92, 231);
                t.AccentHover = Color.FromArgb(124, 111, 239);
                t.Danger = Color.FromArgb(205, 73, 91);
                t.DangerHover = Color.FromArgb(225, 91, 108);
                t.Input = Color.FromArgb(23, 29, 44);
                t.Border = Color.FromArgb(54, 64, 88);
                t.ButtonBack = Color.FromArgb(43, 52, 73);
            }
            else
            {
                t.Back = Color.FromArgb(243, 245, 251);
                t.Surface = Color.White;
                t.Nav = Color.FromArgb(237, 240, 249);
                t.NavHover = Color.FromArgb(226, 231, 245);
                t.NavActive = Color.FromArgb(224, 226, 255);
                t.Text = Color.FromArgb(36, 41, 65);
                t.Muted = Color.FromArgb(100, 109, 134);
                t.Accent = Color.FromArgb(82, 71, 205);
                t.AccentHover = Color.FromArgb(68, 58, 184);
                t.Danger = Color.FromArgb(190, 57, 75);
                t.DangerHover = Color.FromArgb(168, 43, 61);
                t.Input = Color.FromArgb(250, 251, 255);
                t.Border = Color.FromArgb(213, 219, 235);
                t.ButtonBack = Color.FromArgb(237, 240, 248);
            }
            return t;
        }

        public void Apply(Control c)
        {
            string role = c.Tag as string;

            if (c is Form)
            {
                c.BackColor = Back;
                c.ForeColor = Text;
            }
            else if (c is NavItem)
            {
                ((NavItem)c).Theme = this;
                c.Invalidate();
            }
            else if (role == "nav")
            {
                c.BackColor = Nav;
            }
            else if (role == "card")
            {
                c.BackColor = Surface;
                c.ForeColor = Text;
            }
            else if (c is Button)
            {
                Button b = (Button)c;
                b.FlatStyle = FlatStyle.Flat;
                b.UseVisualStyleBackColor = false;
                b.Cursor = Cursors.Hand;
                if (role == "accent" || role == "danger")
                {
                    b.BackColor = role == "accent" ? Accent : Danger;
                    b.ForeColor = Color.White;
                    b.FlatAppearance.BorderSize = 0;
                    b.FlatAppearance.MouseOverBackColor = role == "accent" ? AccentHover : DangerHover;
                    b.FlatAppearance.MouseDownBackColor = role == "accent" ? ControlPaint.Dark(Accent) : ControlPaint.Dark(Danger);
                }
                else
                {
                    b.BackColor = ButtonBack;
                    b.ForeColor = Text;
                    b.FlatAppearance.BorderSize = 1;
                    b.FlatAppearance.BorderColor = Border;
                    b.FlatAppearance.MouseOverBackColor = NavHover;
                    b.FlatAppearance.MouseDownBackColor = NavActive;
                }
            }
            else if (c is ComboBox)
            {
                ComboBox cb = (ComboBox)c;
                cb.FlatStyle = FlatStyle.Flat;
                cb.BackColor = Input;
                cb.ForeColor = Text;
            }
            else if (c is TextBox || c is NumericUpDown || c is ListView)
            {
                c.BackColor = Input;
                c.ForeColor = Text;
                ListView list = c as ListView;
                if (list != null) list.GridLines = false;
            }
            else if (c is LinkLabel)
            {
                LinkLabel l = (LinkLabel)c;
                l.LinkColor = Accent;
                l.ActiveLinkColor = Accent;
                l.VisitedLinkColor = Accent;
                l.ResetBackColor();
            }
            else if (c is Label)
            {
                c.ForeColor = role == "muted" ? Muted : role == "accenttext" ? Accent : Text;
                c.ResetBackColor();
            }
            else if (c is CheckBox || c is RadioButton)
            {
                ButtonBase b = (ButtonBase)c;
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderColor = Border;
                b.FlatAppearance.CheckedBackColor = Input;
                c.ForeColor = Text;
                c.ResetBackColor();
            }
            else
            {
                c.ResetBackColor();
                c.ForeColor = Text;
            }

            foreach (Control child in c.Controls)
                Apply(child);
        }
    }

    /// <summary>Uses the same palette for the tray menu as the main window.</summary>
    internal sealed class ThemeColorTable : ProfessionalColorTable
    {
        private readonly Theme theme;

        public ThemeColorTable(Theme theme)
        {
            this.theme = theme;
            UseSystemColors = false;
        }

        public override Color ToolStripDropDownBackground { get { return theme.Surface; } }
        public override Color MenuBorder { get { return theme.Border; } }
        public override Color MenuItemBorder { get { return theme.Accent; } }
        public override Color MenuItemSelected { get { return theme.NavHover; } }
        public override Color MenuItemSelectedGradientBegin { get { return theme.NavHover; } }
        public override Color MenuItemSelectedGradientEnd { get { return theme.NavActive; } }
        public override Color MenuItemPressedGradientBegin { get { return theme.NavActive; } }
        public override Color MenuItemPressedGradientEnd { get { return theme.NavActive; } }
        public override Color ImageMarginGradientBegin { get { return theme.Surface; } }
        public override Color ImageMarginGradientMiddle { get { return theme.Surface; } }
        public override Color ImageMarginGradientEnd { get { return theme.Surface; } }
        public override Color SeparatorDark { get { return theme.Border; } }
    }

    internal static class Fonts
    {
        public static readonly Font Body = new Font("Segoe UI", 10f);
        public static readonly Font Bold = new Font("Segoe UI Semibold", 10.5f);
        public static readonly Font Header = new Font("Segoe UI Semibold", 19f);
        public static readonly Font Big = new Font("Segoe UI Light", 30f);
        public static readonly Font Brand = new Font("Segoe UI Semibold", 16f);
        public static readonly Font Small = new Font("Segoe UI", 8.75f);
        public static readonly Font Icons = PickIconFont();

        private static Font PickIconFont()
        {
            foreach (string name in new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" })
            {
                Font f = new Font(name, 13f);
                if (f.Name == name) return f;
                f.Dispose();
            }
            return null;
        }
    }

    /// <summary>A sidebar entry with an icon glyph, hover state and an accent bar when selected.</summary>
    internal sealed class NavItem : Control
    {
        private bool hover;
        private bool selected;
        private bool focused;
        public string Glyph;
        public Theme Theme;

        public NavItem()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            Height = 50;
            Margin = Padding.Empty;
        }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnTextChanged(EventArgs e)
        {
            AccessibleName = Text;
            Invalidate();
            base.OnTextChanged(e);
        }
        protected override void OnGotFocus(EventArgs e) { focused = true; Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { focused = false; Invalidate(); base.OnLostFocus(e); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Theme == null) return;
            Graphics g = e.Graphics;
            g.Clear(selected ? Theme.NavActive : hover ? Theme.NavHover : Theme.Nav);
            if (selected)
            {
                using (SolidBrush b = new SolidBrush(Theme.Accent))
                g.FillRectangle(b, 0, 10, 4, Height - 20);
            }
            Color fg = selected ? Theme.Text : Theme.Muted;
            if (Fonts.Icons != null && !string.IsNullOrEmpty(Glyph))
            {
                TextRenderer.DrawText(g, Glyph, Fonts.Icons, new Rectangle(14, 0, 30, Height),
                    selected ? Theme.Accent : fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            TextRenderer.DrawText(g, Text, selected ? Fonts.Bold : Fonts.Body, new Rectangle(52, 0, Width - 56, Height), fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (focused)
                ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(ClientRectangle, -5, -5), Theme.Text,
                    selected ? Theme.NavActive : Theme.Nav);
        }
    }

    /// <summary>Topmost window shown during the last minute of a shutdown timer.</summary>
    internal sealed class WarningForm : Form
    {
        private readonly Label message;
        private readonly Label countdown;
        private readonly Button cancel;
        private readonly Button now;

        public event EventHandler CancelClicked;
        public event EventHandler NowClicked;

        public WarningForm(Theme theme, Icon icon)
        {
            SuspendLayout();
            Text = AppInfo.Name;
            Icon = icon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            TopMost = true;
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);
            ClientSize = new Size(420, 190);
            Font = Fonts.Body;

            message = new Label { AutoSize = false, Location = new Point(20, 16), Size = new Size(380, 44), Font = Fonts.Bold };
            countdown = new Label { AutoSize = false, Location = new Point(20, 58), Size = new Size(380, 60), Font = Fonts.Big, Tag = "accenttext" };
            cancel = new Button { Location = new Point(20, 132), Size = new Size(185, 38), Tag = "accent", Font = Fonts.Bold };
            now = new Button { Location = new Point(215, 132), Size = new Size(185, 38) };
            cancel.Click += delegate { if (CancelClicked != null) CancelClicked(this, EventArgs.Empty); };
            now.Click += delegate { if (NowClicked != null) NowClicked(this, EventArgs.Empty); };
            Controls.AddRange(new Control[] { message, countdown, cancel, now });
            AcceptButton = cancel;
            theme.Apply(this);
            ResumeLayout(false);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.SetDarkTitleBar(Handle, BackColor.GetBrightness() < 0.5f);
        }

        public void ShowState(string actionName, int secondsLeft)
        {
            message.Text = Lang.F("sd_warn_msg", actionName);
            countdown.Text = TimeSpan.FromSeconds(Math.Max(0, secondsLeft)).ToString(@"m\:ss");
            cancel.Text = Lang.T("sd_cancel");
            now.Text = Lang.T("sd_now");
        }
    }

    /// <summary>Small topmost notification in the bottom-right corner, used by reminders.</summary>
    internal sealed class ToastForm : Form
    {
        private static int openCount;

        public ToastForm(Theme theme, string title, string text)
        {
            SuspendLayout();
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);
            Size = new Size(360, 130);
            Font = Fonts.Body;
            Padding = new Padding(1);

            Panel inner = new Panel { Dock = DockStyle.Fill, Tag = "card" };
            Label t = new Label { Text = title, Font = Fonts.Bold, Tag = "accenttext", AutoSize = false, Location = new Point(16, 12), Size = new Size(320, 22) };
            Label m = new Label { Text = text, AutoSize = false, Location = new Point(16, 36), Size = new Size(326, 46) };
            Button ok = new Button { Text = Lang.T("rm_close"), Tag = "accent", Size = new Size(90, 30), Location = new Point(252, 88) };
            ok.Click += delegate { Close(); };
            inner.Controls.AddRange(new Control[] { t, m, ok });
            Controls.Add(inner);
            theme.Apply(this);
            BackColor = theme.Accent; // 1 px accent border around the card
            ResumeLayout(false);

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int offset = (openCount % 4) * (Height + 10);
            Location = new Point(wa.Right - Width - 16, wa.Bottom - Height - 16 - offset);
            openCount++;
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            openCount = Math.Max(0, openCount - 1);
            base.OnFormClosed(e);
        }
    }
}

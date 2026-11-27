using System;
using System.Drawing;
using System.Windows.Forms;

namespace Companian
{
    /// <summary>One-time language choice shown before the main window on a fresh profile.</summary>
    internal sealed class LanguagePrompt : Form
    {
        private readonly ComboBox choices;

        private LanguagePrompt()
        {
            Text = "Companian — Language / Dil";
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);
            ClientSize = new Size(440, 235);
            Font = Fonts.Body;

            Theme theme = Theme.Get(false);
            BackColor = theme.Back;
            ForeColor = theme.Text;

            Label title = new Label
            {
                Text = "Choose your language / Dilinizi seçin",
                AutoSize = true,
                Font = Fonts.Header,
                Location = new Point(28, 28),
                ForeColor = theme.Text
            };
            Label description = new Label
            {
                Text = "You can change this later in Settings. / Daha sonra Ayarlar'dan değiştirebilirsiniz.",
                AutoSize = true,
                Location = new Point(30, 76),
                ForeColor = theme.Muted
            };
            choices = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(30, 122),
                Width = 220,
                Font = Fonts.Body,
                BackColor = theme.Input,
                ForeColor = theme.Text
            };
            choices.Items.AddRange(Lang.Names);
            int detected = Array.IndexOf(Lang.Codes, Lang.DetectDefault());
            choices.SelectedIndex = detected < 0 ? 1 : detected;

            Button continueButton = new Button
            {
                Text = "Continue / Devam et",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                MinimumSize = new Size(150, 40),
                Location = new Point(270, 118),
                BackColor = theme.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Font = Fonts.Bold
            };
            continueButton.FlatAppearance.BorderSize = 0;
            continueButton.FlatAppearance.MouseOverBackColor = theme.AccentHover;
            AcceptButton = continueButton;
            Controls.AddRange(new Control[] { title, description, choices, continueButton });
        }

        public static string Choose()
        {
            using (LanguagePrompt prompt = new LanguagePrompt())
                return prompt.ShowDialog() == DialogResult.OK
                    ? Lang.Codes[Math.Max(0, prompt.choices.SelectedIndex)]
                    : Lang.DetectDefault();
        }
    }
}

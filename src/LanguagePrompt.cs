using System;
using System.Drawing;
using System.Windows.Forms;

namespace Companian
{
    /// <summary>Language-neutral selector shown once on first launch or after upgrading older settings.</summary>
    internal sealed class LanguagePrompt : Form
    {
        private readonly ComboBox choices;

        private LanguagePrompt()
        {
            Text = "Companian — Language / Sprache / Idioma / Dil";
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);
            ClientSize = new Size(540, 245);
            Font = Fonts.Body;

            Theme theme = Theme.Get(false);
            BackColor = theme.Back;
            ForeColor = theme.Text;

            Label title = new Label
            {
                Text = "Choose language · Sprache · Idioma · Dil",
                AutoSize = true,
                Font = Fonts.Header,
                Location = new Point(28, 24),
                ForeColor = theme.Text
            };
            Label description = new Label
            {
                Text = "Select a language. You can change it later in Settings. / Dil seçin; daha sonra Ayarlar'dan değiştirebilirsiniz.",
                AutoSize = false,
                Size = new Size(480, 38),
                Location = new Point(30, 70),
                ForeColor = theme.Muted
            };
            choices = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(30, 133),
                Width = 245,
                Font = Fonts.Body,
                BackColor = theme.Input,
                ForeColor = theme.Text
            };
            choices.Items.AddRange(Lang.Names);
            int detected = Array.IndexOf(Lang.Codes, Lang.DetectDefault());
            choices.SelectedIndex = detected < 0 ? 1 : detected;

            Button continueButton = new Button
            {
                Text = "Start / Weiter / Iniciar / Başla",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                MinimumSize = new Size(235, 42),
                Location = new Point(285, 129),
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

        public static string Choose(string preferredLanguage)
        {
            using (LanguagePrompt prompt = new LanguagePrompt())
            {
                int preferred = Array.IndexOf(Lang.Codes, preferredLanguage);
                if (preferred < 0) preferred = Array.IndexOf(Lang.Codes, Lang.DetectDefault());
                prompt.choices.SelectedIndex = preferred < 0 ? 1 : preferred;
                return prompt.ShowDialog() == DialogResult.OK
                    ? Lang.Codes[Math.Max(0, prompt.choices.SelectedIndex)]
                    : Lang.DetectDefault();
            }
        }
    }
}

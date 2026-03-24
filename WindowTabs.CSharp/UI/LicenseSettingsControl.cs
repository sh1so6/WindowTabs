using System;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Services;

namespace WindowTabs.CSharp.UI
{
    internal sealed class LicenseSettingsControl : UserControl
    {
        private readonly SettingsSession settingsSession;
        private readonly Label statusLabel;
        private readonly TextBox licenseKeyTextBox;
        private readonly TextBox activationCodeTextBox;

        public LicenseSettingsControl(SettingsSession settingsSession)
        {
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));

            Dock = DockStyle.Fill;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(12),
                ColumnCount = 1
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            statusLabel = new Label
            {
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(3, 3, 3, 12)
            };

            var helpLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(780, 0),
                Text = "License key and offline activation code are stored in the shared WindowTabs settings file. Online activation is not migrated yet, so this screen focuses on local save/apply flows.",
                Margin = new Padding(3, 0, 3, 12)
            };

            licenseKeyTextBox = new TextBox
            {
                Dock = DockStyle.Top
            };

            activationCodeTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                Multiline = true,
                Height = 120,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 8, 0, 0)
            };

            var saveLicenseKeyButton = new Button
            {
                Text = "Save License Key",
                AutoSize = true
            };
            saveLicenseKeyButton.Click += (_, __) =>
            {
                settingsSession.Update(snapshot => snapshot.LicenseKey = licenseKeyTextBox.Text?.Trim() ?? string.Empty);
                ReloadValues();
            };

            var applyActivationCodeButton = new Button
            {
                Text = "Apply Activation Code",
                AutoSize = true
            };
            applyActivationCodeButton.Click += (_, __) =>
            {
                settingsSession.Update(snapshot =>
                {
                    var ticket = activationCodeTextBox.Text?.Trim();
                    snapshot.Ticket = string.IsNullOrWhiteSpace(ticket) ? null : ticket;
                });
                ReloadValues();
            };

            var clearActivationButton = new Button
            {
                Text = "Clear Activation",
                AutoSize = true
            };
            clearActivationButton.Click += (_, __) =>
            {
                settingsSession.Update(snapshot => snapshot.Ticket = null);
                ReloadValues();
            };

            var reloadButton = new Button
            {
                Text = "Reload",
                AutoSize = true
            };
            reloadButton.Click += (_, __) => ReloadValues();

            buttons.Controls.Add(saveLicenseKeyButton);
            buttons.Controls.Add(applyActivationCodeButton);
            buttons.Controls.Add(clearActivationButton);
            buttons.Controls.Add(reloadButton);

            panel.Controls.Add(statusLabel);
            panel.Controls.Add(helpLabel);
            panel.Controls.Add(CreateSectionLabel("License Key"));
            panel.Controls.Add(licenseKeyTextBox);
            panel.Controls.Add(CreateSectionLabel("Offline Activation Code"));
            panel.Controls.Add(activationCodeTextBox);
            panel.Controls.Add(buttons);

            Controls.Add(panel);

            ReloadValues();
        }

        public void ReloadValues()
        {
            var settings = settingsSession.Current;
            licenseKeyTextBox.Text = settings.LicenseKey ?? string.Empty;
            activationCodeTextBox.Text = settings.Ticket ?? string.Empty;

            var isActivated = !string.IsNullOrWhiteSpace(settings.Ticket);
            statusLabel.Text = isActivated
                ? "Activated locally. A ticket is stored in settings."
                : "Trial mode. No activation ticket is stored.";
            statusLabel.ForeColor = isActivated ? Color.DarkGreen : SystemColors.ControlText;
        }

        private static Control CreateSectionLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(3, 8, 3, 6)
            };
        }
    }
}

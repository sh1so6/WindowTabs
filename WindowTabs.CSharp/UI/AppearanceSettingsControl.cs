using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Models;
using WindowTabs.CSharp.Services;

namespace WindowTabs.CSharp.UI
{
    internal sealed class AppearanceSettingsControl : UserControl
    {
        private readonly SettingsSession settingsSession;
        private readonly TabAppearancePresetCatalog presetCatalog;
        private readonly DesktopMonitoringService desktopMonitoringService;
        private readonly ComboBox presetComboBox;
        private readonly Dictionary<string, NumericUpDown> numericEditors = new Dictionary<string, NumericUpDown>(StringComparer.Ordinal);
        private readonly Dictionary<string, Button> colorButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        private CheckBox pinnedIconOnlyCheckBox;
        private bool suppressEvents;

        public AppearanceSettingsControl(
            SettingsSession settingsSession,
            TabAppearancePresetCatalog presetCatalog,
            DesktopMonitoringService desktopMonitoringService)
        {
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.presetCatalog = presetCatalog ?? throw new ArgumentNullException(nameof(presetCatalog));
            this.desktopMonitoringService = desktopMonitoringService ?? throw new ArgumentNullException(nameof(desktopMonitoringService));

            Dock = DockStyle.Fill;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                ColumnCount = 1,
                Padding = new Padding(12)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var helpLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(880, 0),
                Text = "This C# editor covers the main tab appearance settings already persisted in WindowTabsSettings.txt. Presets apply a full theme; numeric and color edits are saved immediately.",
                Margin = new Padding(3, 0, 3, 12)
            };

            var presetPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            presetPanel.Controls.Add(CreateSectionLabel("Preset"));

            presetComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 220
            };
            foreach (var presetName in presetCatalog.GetPresetNames())
            {
                presetComboBox.Items.Add(presetName);
            }

            if (presetComboBox.Items.Count > 0)
            {
                presetComboBox.SelectedIndex = 0;
            }

            var applyPresetButton = new Button
            {
                Text = "Apply Preset",
                AutoSize = true
            };
            applyPresetButton.Click += (_, __) =>
            {
                var presetName = presetComboBox.SelectedItem?.ToString();
                if (presetCatalog.TryGetPreset(presetName, out var preset))
                {
                    settingsSession.Update(snapshot => snapshot.TabAppearance = preset.Clone());
                    ReloadValues();
                    desktopMonitoringService.RefreshNow("appearance-preset");
                }
            };

            var resetButton = new Button
            {
                Text = "Reset to Default",
                AutoSize = true
            };
            resetButton.Click += (_, __) =>
            {
                settingsSession.Update(snapshot => snapshot.TabAppearance = SettingsDefaults.CreateDefaultTabAppearance());
                ReloadValues();
                desktopMonitoringService.RefreshNow("appearance-reset");
            };

            presetPanel.Controls.Add(presetComboBox);
            presetPanel.Controls.Add(applyPresetButton);
            presetPanel.Controls.Add(resetButton);

            var metricsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 8, 0, 0)
            };
            metricsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            metricsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddNumeric(metricsPanel, "TabHeight", "Tab height", 0, 100);
            AddNumeric(metricsPanel, "TabMaxWidth", "Tab max width", 40, 600);
            AddNumeric(metricsPanel, "TabPinnedTabWidth", "Pinned tab width", 16, 300);
            AddCheckBox(metricsPanel, "TabPinnedTabWidthIcon", "Pinned tab uses icon width");
            AddNumeric(metricsPanel, "TabOverlap", "Tab overlap", 0, 200);
            AddNumeric(metricsPanel, "TabHeightOffset", "Tab height offset", -20, 20);
            AddNumeric(metricsPanel, "TabIndentNormal", "Indent normal", 0, 400);
            AddNumeric(metricsPanel, "TabIndentFlipped", "Indent flipped", 0, 400);

            var colorsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 12, 0, 0)
            };
            colorsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            colorsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddColor(colorsPanel, "TabInactiveTabColor", "Inactive tab color");
            AddColor(colorsPanel, "TabInactiveTextColor", "Inactive text color");
            AddColor(colorsPanel, "TabInactiveBorderColor", "Inactive border color");
            AddColor(colorsPanel, "TabMouseOverTabColor", "Mouse over tab color");
            AddColor(colorsPanel, "TabMouseOverTextColor", "Mouse over text color");
            AddColor(colorsPanel, "TabMouseOverBorderColor", "Mouse over border color");
            AddColor(colorsPanel, "TabActiveTabColor", "Active tab color");
            AddColor(colorsPanel, "TabActiveTextColor", "Active text color");
            AddColor(colorsPanel, "TabActiveBorderColor", "Active border color");
            AddColor(colorsPanel, "TabFlashTabColor", "Flash tab color");
            AddColor(colorsPanel, "TabFlashTextColor", "Flash text color");
            AddColor(colorsPanel, "TabFlashBorderColor", "Flash border color");

            root.Controls.Add(helpLabel);
            root.Controls.Add(presetPanel);
            root.Controls.Add(CreateSectionLabel("Metrics"));
            root.Controls.Add(metricsPanel);
            root.Controls.Add(CreateSectionLabel("Colors"));
            root.Controls.Add(colorsPanel);

            Controls.Add(root);

            ReloadValues();
        }

        public void ReloadValues()
        {
            suppressEvents = true;
            try
            {
                var appearance = GetCurrentAppearance();
                SetNumeric("TabHeight", appearance.TabHeight);
                SetNumeric("TabMaxWidth", appearance.TabMaxWidth);
                SetNumeric("TabPinnedTabWidth", appearance.TabPinnedTabWidth);
                pinnedIconOnlyCheckBox.Checked = appearance.TabPinnedTabWidthIcon;
                SetNumeric("TabOverlap", appearance.TabOverlap);
                SetNumeric("TabHeightOffset", appearance.TabHeightOffset);
                SetNumeric("TabIndentNormal", appearance.TabIndentNormal);
                SetNumeric("TabIndentFlipped", appearance.TabIndentFlipped);

                SetColorButton("TabInactiveTabColor", appearance.TabInactiveTabColor);
                SetColorButton("TabInactiveTextColor", appearance.TabInactiveTextColor);
                SetColorButton("TabInactiveBorderColor", appearance.TabInactiveBorderColor);
                SetColorButton("TabMouseOverTabColor", appearance.TabMouseOverTabColor);
                SetColorButton("TabMouseOverTextColor", appearance.TabMouseOverTextColor);
                SetColorButton("TabMouseOverBorderColor", appearance.TabMouseOverBorderColor);
                SetColorButton("TabActiveTabColor", appearance.TabActiveTabColor);
                SetColorButton("TabActiveTextColor", appearance.TabActiveTextColor);
                SetColorButton("TabActiveBorderColor", appearance.TabActiveBorderColor);
                SetColorButton("TabFlashTabColor", appearance.TabFlashTabColor);
                SetColorButton("TabFlashTextColor", appearance.TabFlashTextColor);
                SetColorButton("TabFlashBorderColor", appearance.TabFlashBorderColor);
            }
            finally
            {
                suppressEvents = false;
            }
        }

        private void AddNumeric(TableLayoutPanel panel, string propertyName, string labelText, int minimum, int maximum)
        {
            var row = panel.RowCount++;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(CreateLabel(labelText), 0, row);

            var numeric = new NumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum,
                Width = 140,
                Anchor = AnchorStyles.Left
            };
            numeric.ValueChanged += (_, __) =>
            {
                if (suppressEvents)
                {
                    return;
                }

                UpdateAppearance(appearance => SetNumericProperty(appearance, propertyName, (int)numeric.Value), "appearance-metric");
            };

            numericEditors[propertyName] = numeric;
            panel.Controls.Add(numeric, 1, row);
        }

        private void AddCheckBox(TableLayoutPanel panel, string propertyName, string labelText)
        {
            var row = panel.RowCount++;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(CreateLabel(labelText), 0, row);

            pinnedIconOnlyCheckBox = new CheckBox
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            pinnedIconOnlyCheckBox.CheckedChanged += (_, __) =>
            {
                if (suppressEvents)
                {
                    return;
                }

                UpdateAppearance(appearance => appearance.TabPinnedTabWidthIcon = pinnedIconOnlyCheckBox.Checked, "appearance-pin-width");
            };

            panel.Controls.Add(pinnedIconOnlyCheckBox, 1, row);
        }

        private void AddColor(TableLayoutPanel panel, string propertyName, string labelText)
        {
            var row = panel.RowCount++;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(CreateLabel(labelText), 0, row);

            var colorButton = new Button
            {
                AutoSize = true,
                Width = 180,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left
            };
            colorButton.Click += (_, __) =>
            {
                using (var colorDialog = new ColorDialog())
                {
                    colorDialog.FullOpen = true;
                    colorDialog.Color = GetColorProperty(GetCurrentAppearance(), propertyName);
                    if (colorDialog.ShowDialog(FindForm()) != DialogResult.OK)
                    {
                        return;
                    }

                    UpdateAppearance(appearance => SetColorProperty(appearance, propertyName, colorDialog.Color), "appearance-color");
                    ReloadValues();
                }
            };

            colorButtons[propertyName] = colorButton;
            panel.Controls.Add(colorButton, 1, row);
        }

        private void UpdateAppearance(Action<TabAppearanceInfo> update, string reason)
        {
            settingsSession.Update(snapshot =>
            {
                snapshot.TabAppearance ??= SettingsDefaults.CreateDefaultTabAppearance();
                update(snapshot.TabAppearance);
            });
            desktopMonitoringService.RefreshNow(reason);
        }

        private TabAppearanceInfo GetCurrentAppearance()
        {
            return settingsSession.Current.TabAppearance ?? SettingsDefaults.CreateDefaultTabAppearance();
        }

        private void SetNumeric(string propertyName, int value)
        {
            if (numericEditors.TryGetValue(propertyName, out var numeric))
            {
                var clamped = Math.Max((int)numeric.Minimum, Math.Min((int)numeric.Maximum, value));
                numeric.Value = clamped;
            }
        }

        private void SetColorButton(string propertyName, Color color)
        {
            if (!colorButtons.TryGetValue(propertyName, out var button))
            {
                return;
            }

            button.BackColor = color;
            button.ForeColor = GetContrastColor(color);
            button.Text = "#" + ColorSerialization.ToHexString(color).PadLeft(6, '0');
        }

        private static void SetNumericProperty(TabAppearanceInfo appearance, string propertyName, int value)
        {
            switch (propertyName)
            {
                case "TabHeight":
                    appearance.TabHeight = value;
                    break;
                case "TabMaxWidth":
                    appearance.TabMaxWidth = value;
                    break;
                case "TabPinnedTabWidth":
                    appearance.TabPinnedTabWidth = value;
                    break;
                case "TabOverlap":
                    appearance.TabOverlap = value;
                    break;
                case "TabHeightOffset":
                    appearance.TabHeightOffset = value;
                    break;
                case "TabIndentNormal":
                    appearance.TabIndentNormal = value;
                    break;
                case "TabIndentFlipped":
                    appearance.TabIndentFlipped = value;
                    break;
            }
        }

        private static void SetColorProperty(TabAppearanceInfo appearance, string propertyName, Color color)
        {
            switch (propertyName)
            {
                case "TabInactiveTabColor":
                    appearance.TabInactiveTabColor = color;
                    break;
                case "TabInactiveTextColor":
                    appearance.TabInactiveTextColor = color;
                    break;
                case "TabInactiveBorderColor":
                    appearance.TabInactiveBorderColor = color;
                    break;
                case "TabMouseOverTabColor":
                    appearance.TabMouseOverTabColor = color;
                    break;
                case "TabMouseOverTextColor":
                    appearance.TabMouseOverTextColor = color;
                    break;
                case "TabMouseOverBorderColor":
                    appearance.TabMouseOverBorderColor = color;
                    break;
                case "TabActiveTabColor":
                    appearance.TabActiveTabColor = color;
                    break;
                case "TabActiveTextColor":
                    appearance.TabActiveTextColor = color;
                    break;
                case "TabActiveBorderColor":
                    appearance.TabActiveBorderColor = color;
                    break;
                case "TabFlashTabColor":
                    appearance.TabFlashTabColor = color;
                    break;
                case "TabFlashTextColor":
                    appearance.TabFlashTextColor = color;
                    break;
                case "TabFlashBorderColor":
                    appearance.TabFlashBorderColor = color;
                    break;
            }
        }

        private static Color GetColorProperty(TabAppearanceInfo appearance, string propertyName)
        {
            switch (propertyName)
            {
                case "TabInactiveTabColor":
                    return appearance.TabInactiveTabColor;
                case "TabInactiveTextColor":
                    return appearance.TabInactiveTextColor;
                case "TabInactiveBorderColor":
                    return appearance.TabInactiveBorderColor;
                case "TabMouseOverTabColor":
                    return appearance.TabMouseOverTabColor;
                case "TabMouseOverTextColor":
                    return appearance.TabMouseOverTextColor;
                case "TabMouseOverBorderColor":
                    return appearance.TabMouseOverBorderColor;
                case "TabActiveTabColor":
                    return appearance.TabActiveTabColor;
                case "TabActiveTextColor":
                    return appearance.TabActiveTextColor;
                case "TabActiveBorderColor":
                    return appearance.TabActiveBorderColor;
                case "TabFlashTabColor":
                    return appearance.TabFlashTabColor;
                case "TabFlashTextColor":
                    return appearance.TabFlashTextColor;
                case "TabFlashBorderColor":
                    return appearance.TabFlashBorderColor;
                default:
                    return Color.Empty;
            }
        }

        private static Color GetContrastColor(Color background)
        {
            var brightness = (background.R * 299) + (background.G * 587) + (background.B * 114);
            return brightness >= 140000 ? Color.Black : Color.White;
        }

        private static Control CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 8, 3, 8)
            };
        }

        private static Control CreateSectionLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
                Margin = new Padding(3, 12, 3, 8)
            };
        }
    }
}

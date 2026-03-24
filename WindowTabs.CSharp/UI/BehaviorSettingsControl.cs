using System;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Services;

namespace WindowTabs.CSharp.UI
{
    internal sealed class BehaviorSettingsControl : UserControl
    {
        private readonly SettingsSession settingsSession;
        private readonly FilterService filterService;
        private readonly HotKeySettingsStore hotKeySettingsStore;

        public BehaviorSettingsControl(
            SettingsSession settingsSession,
            FilterService filterService,
            HotKeySettingsStore hotKeySettingsStore)
        {
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            this.hotKeySettingsStore = hotKeySettingsStore ?? throw new ArgumentNullException(nameof(hotKeySettingsStore));

            Dock = DockStyle.Fill;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                ColumnCount = 2,
                Padding = new Padding(12)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddCheckBox(panel, "Run at startup", settingsSession.Current.RunAtStartup, value =>
                settingsSession.Update(snapshot => snapshot.RunAtStartup = value));
            AddCheckBox(panel, "Hide inactive tabs", settingsSession.Current.HideInactiveTabs, value =>
                settingsSession.Update(snapshot => snapshot.HideInactiveTabs = value));
            AddCheckBox(panel, "Enable tabs by default", filterService.IsTabbingEnabledForAllProcessesByDefault, value =>
                filterService.IsTabbingEnabledForAllProcessesByDefault = value);
            AddCheckBox(panel, "Enable Ctrl+Number hotkeys", settingsSession.Current.EnableCtrlNumberHotKey, value =>
                settingsSession.Update(snapshot => snapshot.EnableCtrlNumberHotKey = value));
            AddCheckBox(panel, "Enable hover activate", settingsSession.Current.EnableHoverActivate, value =>
                settingsSession.Update(snapshot => snapshot.EnableHoverActivate = value));
            AddComboBox(panel, "Default tab position", new[] { "TopLeft", "TopRight" }, settingsSession.Current.TabPositionByDefault, value =>
                settingsSession.Update(snapshot => snapshot.TabPositionByDefault = value));
            AddComboBox(panel, "Hide tabs mode", new[] { "never", "down", "doubleclick" }, settingsSession.Current.HideTabsWhenDownByDefault, value =>
                settingsSession.Update(snapshot => snapshot.HideTabsWhenDownByDefault = value));
            AddNumeric(panel, "Hide tabs delay (ms)", settingsSession.Current.HideTabsDelayMilliseconds, 0, 10000, value =>
                settingsSession.Update(snapshot => snapshot.HideTabsDelayMilliseconds = value));
            AddCheckBox(panel, "Hide tabs on fullscreen", settingsSession.Current.HideTabsOnFullscreen, value =>
                settingsSession.Update(snapshot => snapshot.HideTabsOnFullscreen = value));
            AddCheckBox(panel, "Snap tab height margin", settingsSession.Current.SnapTabHeightMargin, value =>
                settingsSession.Update(snapshot => snapshot.SnapTabHeightMargin = value));
            AddNumeric(panel, "HotKey prevTab", hotKeySettingsStore.Get("prevTab"), 0, short.MaxValue, value =>
                hotKeySettingsStore.Set("prevTab", value));
            AddNumeric(panel, "HotKey nextTab", hotKeySettingsStore.Get("nextTab"), 0, short.MaxValue, value =>
                hotKeySettingsStore.Set("nextTab", value));

            Controls.Add(panel);
        }

        private static void AddCheckBox(TableLayoutPanel panel, string labelText, bool initialValue, Action<bool> onChanged)
        {
            var row = panel.RowCount++;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(CreateLabel(labelText), 0, row);
            var checkBox = new CheckBox
            {
                Checked = initialValue,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            checkBox.CheckedChanged += (_, __) => onChanged(checkBox.Checked);
            panel.Controls.Add(checkBox, 1, row);
        }

        private static void AddComboBox(TableLayoutPanel panel, string labelText, string[] values, string initialValue, Action<string> onChanged)
        {
            var row = panel.RowCount++;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(CreateLabel(labelText), 0, row);
            var comboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Anchor = AnchorStyles.Left
            };
            comboBox.Items.AddRange(values);
            comboBox.SelectedItem = string.IsNullOrWhiteSpace(initialValue) ? values[0] : initialValue;
            comboBox.SelectedIndexChanged += (_, __) => onChanged(comboBox.SelectedItem?.ToString() ?? values[0]);
            panel.Controls.Add(comboBox, 1, row);
        }

        private static void AddNumeric(TableLayoutPanel panel, string labelText, int initialValue, int minimum, int maximum, Action<int> onChanged)
        {
            var row = panel.RowCount++;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(CreateLabel(labelText), 0, row);
            var numeric = new NumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = Math.Min(maximum, Math.Max(minimum, initialValue)),
                Width = 120,
                Anchor = AnchorStyles.Left
            };
            numeric.ValueChanged += (_, __) => onChanged((int)numeric.Value);
            panel.Controls.Add(numeric, 1, row);
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
    }
}

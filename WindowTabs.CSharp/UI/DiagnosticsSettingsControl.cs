using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using WindowTabs.CSharp.Models;
using WindowTabs.CSharp.Services;

namespace WindowTabs.CSharp.UI
{
    internal sealed class DiagnosticsSettingsControl : UserControl
    {
        private readonly DesktopSnapshotService desktopSnapshotService;
        private readonly TextBox diagnosticsTextBox;

        public DiagnosticsSettingsControl(DesktopSnapshotService desktopSnapshotService)
        {
            this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));

            Dock = DockStyle.Fill;

            var toolbar = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                Dock = DockStyle.Top
            };

            var scanButton = new ToolStripButton("Scan");
            scanButton.Click += (_, __) => RefreshDiagnostics();
            var copyButton = new ToolStripButton("Copy to clipboard");
            copyButton.Click += (_, __) =>
            {
                diagnosticsTextBox.SelectAll();
                diagnosticsTextBox.Copy();
            };
            var copySettingsButton = new ToolStripButton("Copy settings file");
            copySettingsButton.Click += (_, __) => CopySettingsFile();

            toolbar.Items.Add(scanButton);
            toolbar.Items.Add(copyButton);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(copySettingsButton);

            diagnosticsTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Font = new System.Drawing.Font("Consolas", 9)
            };

            Controls.Add(diagnosticsTextBox);
            Controls.Add(toolbar);
        }

        public void RefreshDiagnostics()
        {
            var windows = desktopSnapshotService.EnumerateWindowsInZOrder();
            var processes = windows
                .Where(window => window.Process != null)
                .GroupBy(window => window.Process.ProcessId)
                .Select(group => group.First().Process)
                .OrderBy(process => process.ProcessId)
                .ToList();

            var root = new JObject
            {
                ["processes"] = new JArray(processes.Select(SerializeProcess)),
                ["windows"] = new JArray(windows.Select(SerializeWindow))
            };

            diagnosticsTextBox.Text = root.ToString();
        }

        private static JObject SerializeProcess(ProcessSnapshot process)
        {
            return new JObject
            {
                ["pid"] = process.ProcessId,
                ["canQueryProcess"] = process.CanQueryProcess,
                ["path"] = process.ProcessPath
            };
        }

        private static JObject SerializeWindow(WindowSnapshot window)
        {
            return new JObject
            {
                ["hwnd"] = window.Handle.ToInt64(),
                ["style"] = window.Style,
                ["styleEx"] = window.ExtendedStyle,
                ["isVisible"] = window.IsVisibleOnScreen,
                ["isTopMost"] = window.IsTopMost,
                ["title"] = window.Text,
                ["pid"] = window.Process?.ProcessId ?? 0
            };
        }

        private static void CopySettingsFile()
        {
            var settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WindowTabs");
            var settingsFile = Path.Combine(settingsFolder, "WindowTabsSettings.txt");
            var targetFile = Path.Combine(".", "WindowTabsSettings.txt");
            File.Copy(settingsFile, targetFile, false);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WindowTabs.CSharp.Models;
using WindowTabs.CSharp.Services;

namespace WindowTabs.CSharp.UI
{
    internal sealed class ProgramsSettingsControl : UserControl
    {
        private readonly DesktopMonitoringService desktopMonitoringService;
        private readonly FilterService filterService;
        private readonly ProcessSettingsService processSettingsService;
        private readonly DataGridView grid;
        private readonly CheckBox showConfiguredOnlyCheckBox;
        private readonly ToolStripButton refreshButton;
        private bool suppressEvents;

        public ProgramsSettingsControl(
            DesktopMonitoringService desktopMonitoringService,
            FilterService filterService,
            ProcessSettingsService processSettingsService)
        {
            this.desktopMonitoringService = desktopMonitoringService ?? throw new ArgumentNullException(nameof(desktopMonitoringService));
            this.filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            this.processSettingsService = processSettingsService ?? throw new ArgumentNullException(nameof(processSettingsService));

            Dock = DockStyle.Fill;

            var toolbar = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                Dock = DockStyle.Top
            };
            refreshButton = new ToolStripButton("Refresh");
            refreshButton.Click += (_, __) => ReloadRows();
            toolbar.Items.Add(refreshButton);
            toolbar.Items.Add(new ToolStripSeparator());

            showConfiguredOnlyCheckBox = new CheckBox
            {
                Text = "Show configured only",
                AutoSize = true
            };
            showConfiguredOnlyCheckBox.CheckedChanged += (_, __) => ReloadRows();
            toolbar.Items.Add(new ToolStripControlHost(showConfiguredOnlyCheckBox));

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EditMode = DataGridViewEditMode.EditOnEnter
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProcessName",
                HeaderText = "Process",
                DataPropertyName = "ProcessName",
                ReadOnly = true,
                Width = 180
            });
            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "IsRunning",
                HeaderText = "Running",
                DataPropertyName = "IsRunning",
                ReadOnly = true,
                Width = 60
            });
            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "EnableTabs",
                HeaderText = "Enable Tabs",
                DataPropertyName = "EnableTabs",
                Width = 80
            });
            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "AutoGrouping",
                HeaderText = "Auto Group",
                DataPropertyName = "EnableAutoGrouping",
                Width = 80
            });

            var categoryColumn = new DataGridViewComboBoxColumn
            {
                Name = "Category",
                HeaderText = "Category",
                DataPropertyName = "CategoryNumber",
                Width = 75,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                ValueType = typeof(int)
            };
            categoryColumn.Items.AddRange(Enumerable.Range(0, 11).Cast<object>().ToArray());
            grid.Columns.Add(categoryColumn);

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ProcessPath",
                HeaderText = "Path",
                DataPropertyName = "ProcessPath",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            grid.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "Remove",
                HeaderText = string.Empty,
                Text = "Remove",
                UseColumnTextForButtonValue = true,
                Width = 70
            });

            grid.CurrentCellDirtyStateChanged += OnGridCurrentCellDirtyStateChanged;
            grid.CellValueChanged += OnGridCellValueChanged;
            grid.CellContentClick += OnGridCellContentClick;

            Controls.Add(grid);
            Controls.Add(toolbar);
        }

        public void ReloadRows()
        {
            suppressEvents = true;
            try
            {
                var rows = BuildRows(
                    desktopMonitoringService.CurrentState?.RefreshResult ?? new DesktopRefreshResult(),
                    showConfiguredOnlyCheckBox.Checked);

                grid.Rows.Clear();
                foreach (var row in rows)
                {
                    var rowIndex = grid.Rows.Add(
                        row.ProcessName,
                        row.IsRunning,
                        row.EnableTabs,
                        row.EnableAutoGrouping,
                        row.CategoryNumber,
                        row.ProcessPath,
                        "Remove");

                    var gridRow = grid.Rows[rowIndex];
                    gridRow.Tag = row;
                    gridRow.Cells["AutoGrouping"].ReadOnly = !row.EnableTabs;
                    gridRow.Cells["Category"].ReadOnly = !row.EnableTabs;
                    gridRow.Cells["Remove"].ReadOnly = !row.HasSettings;
                    if (!row.HasSettings)
                    {
                        gridRow.Cells["Remove"].Style.ForeColor = SystemColors.GrayText;
                    }
                }
            }
            finally
            {
                suppressEvents = false;
            }
        }

        private void OnGridCurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grid.IsCurrentCellDirty)
            {
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void OnGridCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (suppressEvents || e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            var row = grid.Rows[e.RowIndex].Tag as ProgramSettingsRow;
            if (row == null)
            {
                return;
            }

            var columnName = grid.Columns[e.ColumnIndex].Name;
            switch (columnName)
            {
                case "EnableTabs":
                    var enableTabs = Convert.ToBoolean(grid.Rows[e.RowIndex].Cells["EnableTabs"].Value);
                    filterService.SetIsTabbingEnabledForProcess(row.ProcessPath, enableTabs);
                    if (!enableTabs)
                    {
                        processSettingsService.SetAutoGroupingEnabled(row.ProcessPath, false);
                        processSettingsService.SetCategoryForProcess(row.ProcessPath, 0);
                    }

                    ReloadRows();
                    break;
                case "AutoGrouping":
                    var autoGrouping = Convert.ToBoolean(grid.Rows[e.RowIndex].Cells["AutoGrouping"].Value);
                    processSettingsService.SetAutoGroupingEnabled(row.ProcessPath, autoGrouping);
                    ReloadRows();
                    break;
                case "Category":
                    var categoryNumber = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["Category"].Value ?? 0);
                    processSettingsService.SetCategoryForProcess(row.ProcessPath, categoryNumber);
                    if (categoryNumber > 0)
                    {
                        processSettingsService.SetAutoGroupingEnabled(row.ProcessPath, true);
                    }

                    ReloadRows();
                    break;
            }
        }

        private void OnGridCellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (suppressEvents || e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (grid.Columns[e.ColumnIndex].Name != "Remove")
            {
                return;
            }

            var row = grid.Rows[e.RowIndex].Tag as ProgramSettingsRow;
            if (row == null || !row.HasSettings)
            {
                return;
            }

            processSettingsService.RemoveProcessSettings(row.ProcessPath);
            ReloadRows();
        }

        private IReadOnlyList<ProgramSettingsRow> BuildRows(DesktopRefreshResult refreshResult, bool configuredOnly)
        {
            var rows = new List<ProgramSettingsRow>();
            var screenRegion = refreshResult.ScreenRegion ?? new RectValue();
            var runningPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var window in refreshResult.Windows)
            {
                if (window?.Process == null || string.IsNullOrWhiteSpace(window.Process.ProcessPath))
                {
                    continue;
                }

                if (!filterService.IsAppWindow(window, screenRegion))
                {
                    continue;
                }

                runningPaths.Add(window.Process.ProcessPath);
            }

            var paths = new HashSet<string>(runningPaths, StringComparer.OrdinalIgnoreCase);
            foreach (var configuredPath in processSettingsService.GetAllConfiguredProcessPaths())
            {
                paths.Add(configuredPath);
            }

            foreach (var processPath in paths)
            {
                var hasSettings = processSettingsService.HasProcessSettings(processPath);
                var isRunning = runningPaths.Contains(processPath);
                if (configuredOnly && !hasSettings)
                {
                    continue;
                }

                rows.Add(new ProgramSettingsRow
                {
                    ProcessPath = processPath,
                    ProcessName = Path.GetFileName(processPath),
                    IsRunning = isRunning,
                    HasSettings = hasSettings,
                    EnableTabs = filterService.GetIsTabbingEnabledForProcess(processPath),
                    EnableAutoGrouping = processSettingsService.GetAutoGroupingEnabled(processPath),
                    CategoryNumber = processSettingsService.GetCategoryForProcess(processPath)
                });
            }

            return rows
                .OrderBy(row => row.CategoryNumber)
                .ThenBy(row => row.ProcessName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private sealed class ProgramSettingsRow
        {
            public string ProcessName { get; set; } = string.Empty;

            public string ProcessPath { get; set; } = string.Empty;

            public bool IsRunning { get; set; }

            public bool HasSettings { get; set; }

            public bool EnableTabs { get; set; }

            public bool EnableAutoGrouping { get; set; }

            public int CategoryNumber { get; set; }
        }
    }
}

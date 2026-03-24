using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowTabs.CSharp.Models;
using WindowTabs.CSharp.Services;

namespace WindowTabs.CSharp.UI
{
    internal sealed class WorkspaceSettingsControl : UserControl
    {
        private readonly WorkspaceLayoutsService workspaceLayoutsService;
        private readonly TreeView workspaceTree;
        private readonly ToolStripButton newButton;
        private readonly ToolStripButton restoreButton;
        private readonly ToolStripButton removeButton;

        public WorkspaceSettingsControl(WorkspaceLayoutsService workspaceLayoutsService)
        {
            this.workspaceLayoutsService = workspaceLayoutsService ?? throw new ArgumentNullException(nameof(workspaceLayoutsService));

            Dock = DockStyle.Fill;

            var toolbar = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                Dock = DockStyle.Top
            };
            newButton = new ToolStripButton("New");
            newButton.Click += (_, __) =>
            {
                workspaceLayoutsService.CreateFromCurrentDesktop();
                ReloadTree();
            };
            restoreButton = new ToolStripButton("Restore")
            {
                Enabled = false
            };
            restoreButton.Click += (_, __) =>
            {
                if (workspaceTree.SelectedNode?.Tag is WorkspaceLayout layout)
                {
                    workspaceLayoutsService.RestoreWorkspace(layout);
                }
            };
            removeButton = new ToolStripButton("Remove")
            {
                Enabled = false
            };
            removeButton.Click += (_, __) =>
            {
                var workspaceName = FindWorkspaceName(workspaceTree.SelectedNode);
                if (!string.IsNullOrWhiteSpace(workspaceName))
                {
                    workspaceLayoutsService.RemoveWorkspace(workspaceName);
                    ReloadTree();
                }
            };

            toolbar.Items.Add(newButton);
            toolbar.Items.Add(restoreButton);
            toolbar.Items.Add(removeButton);

            workspaceTree = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false,
                FullRowSelect = true
            };
            workspaceTree.AfterSelect += OnAfterSelect;

            Controls.Add(workspaceTree);
            Controls.Add(toolbar);
        }

        public void ReloadTree()
        {
            workspaceTree.BeginUpdate();
            try
            {
                workspaceTree.Nodes.Clear();
                foreach (var workspace in workspaceLayoutsService.LoadLayouts())
                {
                    var workspaceNode = new TreeNode(workspace.Name)
                    {
                        Tag = workspace
                    };

                    foreach (var group in workspace.Groups)
                    {
                        var groupNode = new TreeNode(group.Name)
                        {
                            Tag = group
                        };

                        foreach (var window in group.Windows.OrderBy(window => window.ZOrder))
                        {
                            groupNode.Nodes.Add(new TreeNode(window.Title)
                            {
                                Tag = window
                            });
                        }

                        workspaceNode.Nodes.Add(groupNode);
                    }

                    workspaceTree.Nodes.Add(workspaceNode);
                }

                workspaceTree.ExpandAll();
                UpdateButtons();
            }
            finally
            {
                workspaceTree.EndUpdate();
            }
        }

        private void OnAfterSelect(object sender, TreeViewEventArgs e)
        {
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            restoreButton.Enabled = workspaceTree.SelectedNode?.Tag is WorkspaceLayout;
            removeButton.Enabled = workspaceTree.SelectedNode != null;
        }

        private static string FindWorkspaceName(TreeNode node)
        {
            while (node != null)
            {
                if (node.Tag is WorkspaceLayout workspace)
                {
                    return workspace.Name;
                }

                node = node.Parent;
            }

            return string.Empty;
        }
    }
}

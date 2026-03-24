using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowTabs.CSharp.Services
{
    internal sealed class PresentationDialogService
    {
        public string ShowRenamePrompt(IWin32Window owner, string title, string initialValue)
        {
            using (var dialog = new Form())
            using (var textBox = new TextBox())
            using (var okButton = new Button())
            using (var cancelButton = new Button())
            using (var buttons = new FlowLayoutPanel())
            using (var layout = new TableLayoutPanel())
            {
                dialog.Text = title;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.ClientSize = new Size(360, 110);

                textBox.Dock = DockStyle.Top;
                textBox.Text = initialValue ?? string.Empty;

                okButton.Text = "OK";
                okButton.DialogResult = DialogResult.OK;
                cancelButton.Text = "Cancel";
                cancelButton.DialogResult = DialogResult.Cancel;

                buttons.Dock = DockStyle.Fill;
                buttons.FlowDirection = FlowDirection.RightToLeft;
                buttons.Controls.Add(cancelButton);
                buttons.Controls.Add(okButton);

                layout.Dock = DockStyle.Fill;
                layout.Padding = new Padding(12);
                layout.RowCount = 3;
                layout.ColumnCount = 1;
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.Controls.Add(new Label
                {
                    Text = "Tab name",
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, 6)
                }, 0, 0);
                layout.Controls.Add(textBox, 0, 1);
                layout.Controls.Add(buttons, 0, 2);

                dialog.Controls.Add(layout);
                dialog.AcceptButton = okButton;
                dialog.CancelButton = cancelButton;

                return dialog.ShowDialog(owner) == DialogResult.OK
                    ? textBox.Text?.Trim()
                    : null;
            }
        }

        public Color? ShowColorPicker(IWin32Window owner, Color? initialColor)
        {
            using (var dialog = new ColorDialog())
            {
                dialog.FullOpen = true;
                if (initialColor.HasValue)
                {
                    dialog.Color = initialColor.Value;
                }

                return dialog.ShowDialog(owner) == DialogResult.OK
                    ? dialog.Color
                    : (Color?)null;
            }
        }
    }
}

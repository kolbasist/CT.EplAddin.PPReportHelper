using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class UsabilityListSettingsDialog : Form
    {
        private readonly PropertyGrid _propertyGrid;
        private readonly Button _applyButton;
        private readonly Button _closeButton;

        internal UsabilityListSettingsDialog()
        {
            Text = "Настройки таблицы применимости";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;

            Width = 480;
            Height = 470;

            _propertyGrid = new PropertyGrid();
            _propertyGrid.SetBounds(8, 8, 444, 360);
            _propertyGrid.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Bottom |
                AnchorStyles.Left |
                AnchorStyles.Right;
            _propertyGrid.SelectedObject = UsabilityListSettings.Instance;

            _applyButton = new Button();
            _applyButton.Text = "Apply";
            _applyButton.SetBounds(288, 382, 75, 24);

            _closeButton = new Button();
            _closeButton.Text = "Close";
            _closeButton.DialogResult = DialogResult.Cancel;
            _closeButton.SetBounds(374, 382, 75, 24);

            _applyButton.Click += ApplyButton_Click;

            Controls.Add(_propertyGrid);
            Controls.Add(_applyButton);
            Controls.Add(_closeButton);

            AcceptButton = _applyButton;
            CancelButton = _closeButton;

            Load += UsabilityListSettingsDialog_Load;
        }

        private void UsabilityListSettingsDialog_Load(
            object sender,
            EventArgs e)
        {
            UsabilityListSettings.Instance.LoadSettings();
            _propertyGrid.Refresh();
        }

        private void ApplyButton_Click(
            object sender,
            EventArgs e)
        {
            UsabilityListSettings.Instance.SaveSettings();
        }

        internal static DialogResult ShowModal()
        {
            using (UsabilityListSettingsDialog dialog =
                   new UsabilityListSettingsDialog())
            {
                return dialog.ShowDialog(
                    new WindowWrapper(
                        Process.GetCurrentProcess().MainWindowHandle));
            }
        }
    }
}
using System;
using System.Diagnostics;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using EplAction = Eplan.EplApi.ApplicationFramework.Action;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class UsabilityListPropertiesDialog : Form
    {
        private readonly PropertyGrid _propertyGrid;
        private readonly Button _applyButton;
        private readonly Button _closeButton;

        internal UsabilityListPropertiesDialog()
        {
            Text = "Свойства таблицы применимости";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;

            Width = 540;
            Height = 480;

            _propertyGrid = new PropertyGrid();
            _propertyGrid.SetBounds(8, 8, 504, 360);
            _propertyGrid.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Bottom |
                AnchorStyles.Left |
                AnchorStyles.Right;

            _applyButton = new Button();
            _applyButton.Text = "Apply";
            _applyButton.SetBounds(346, 386, 75, 24);

            _closeButton = new Button();
            _closeButton.Text = "Close";
            _closeButton.DialogResult = DialogResult.Cancel;
            _closeButton.SetBounds(432, 386, 75, 24);

            _applyButton.Click += ApplyButton_Click;

            Controls.Add(_propertyGrid);
            Controls.Add(_applyButton);
            Controls.Add(_closeButton);

            AcceptButton = _applyButton;
            CancelButton = _closeButton;
        }

        internal void SetItem(
            UsabilityList list)
        {
            _propertyGrid.SelectedObject = list;
        }

        private void ApplyButton_Click(
            object sender,
            EventArgs e)
        {
            UsabilityList list =
                _propertyGrid.SelectedObject as UsabilityList;

            if (list == null)
            {
                return;
            }

            if (!list.HasData &&
                !list.TryReloadDataFromActivePage())
            {
                MessageBox.Show(
                    "Не удалось получить данные для таблицы применимости. Таблица не будет перерисована.",
                    "Таблица применимости",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            /*
             * Не вызываем RefreshCurrentPropertiesFromBlockGraphics() перед Render().
             * Иначе только что изменённые значения из PropertyGrid перезаписываются
             * старыми значениями, прочитанными с размещённой таблицы.
             * Обновляем только позицию блока.
             */
            list.RefreshOriginFromBlockLocation();
            list.Render();

            EplAction redrawAction =
                new ActionManager().FindAction("gedRedraw");

            if (redrawAction != null)
            {
                redrawAction.Execute(new ActionCallingContext());
            }
        }

        internal static DialogResult ShowModal(
            UsabilityList list)
        {
            using (UsabilityListPropertiesDialog dialog =
                   new UsabilityListPropertiesDialog())
            {
                dialog.SetItem(list);

                return dialog.ShowDialog(
                    new WindowWrapper(
                        Process.GetCurrentProcess().MainWindowHandle));
            }
        }
    }
}
using ChartEditLibrary.Interfaces;
using ChartEditLibrary.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChartEditWinform.Controls
{
    public partial class ShowControl : UserControl
    {
        readonly DraggableChartVM vm = null!;
        public ShowControl()
        {
            InitializeComponent();
        }

        public ShowControl(IChartControl chartControl) : this()
        {
            this.vm = chartControl.ChartData;
            draggableChartControl1.ChartControl = chartControl;
            chartEditControl1.BindData(chartControl.ChartData);
        }

        public void ChangeEditView(bool hide)
        {
            if (hide)
                tableLayoutPanel1.ColumnStyles[1].Width = 0;
            else
                tableLayoutPanel1.ColumnStyles[1].Width = 617;
        }

        internal void AutoFit()
        {
            draggableChartControl1.AutoFit();
        }

        internal async Task Export(string selectedPath)
        {
            string content = vm.GetSaveContent();
            await File.WriteAllTextAsync($"{selectedPath}\\{vm.FileName}.csv", content).ConfigureAwait(false);
            await File.WriteAllBytesAsync($"{selectedPath}\\{vm.FileName}.png", draggableChartControl1.GetImage()).ConfigureAwait(false);
        }
    }
}

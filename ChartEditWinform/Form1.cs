using ChartEditWinform.ChartCore;
using ScottPlot;
using System.Diagnostics;

namespace ChartEditWinform
{
    public partial class Form1 : Form
    {
        string? fileName;
        readonly OpenFileDialog dialog;
        public Form1()
        {
            InitializeComponent();
            dialog = new OpenFileDialog()
            {
                Filter = "csv�ļ�(*.txt)|*.csv",
            };
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Debugger.IsAttached)
            {
                string fileName = @"D:\d1\20231031�������������ͼ\5356B-1.csv";
                var vm = new ChartCore.Entity.DraggableChartVM(fileName);
                draggableChart1.ChartData = vm;
                chartEditControl1.DragData = vm;
            }
        }

        private void ����ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(fileName))
                dialog.InitialDirectory = Path.GetDirectoryName(fileName);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                fileName = dialog.FileName;
                var vm = new ChartCore.Entity.DraggableChartVM(fileName);
                draggableChart1.ChartData = vm;
                chartEditControl1.DragData = vm;
            }
        }

        private void chartEditControl1_Load(object sender, EventArgs e)
        {

        }
    }
}

using ChartEditWPF.ViewModels;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChartEditWPF.Windows
{
    /// <summary>
    /// QualityRangeChartWindow.xaml 的交互逻辑
    /// </summary>
    public partial class QualityRangeChartWindow : Window
    {

        public QualityRangeChartWindow()
        {
            InitializeComponent();
        }

        public void Show(IEnumerable<QualityRangeControlViewModel> qualityRangeViewModel)
        {
            GenerteChart(qualityRangeViewModel.ToArray());
            base.ShowDialog();
        }

        public byte[] GetImage(IEnumerable<QualityRangeControlViewModel> qualityRangeViewModel)
        {
            GenerteChart(qualityRangeViewModel.ToArray());
            PixelSize size = new(1920, 1080);
            return chart.Plot.GetImageBytes((int)size.Width, (int)size.Height, ImageFormat.Png);
        }

        private void GenerteChart(QualityRangeControlViewModel[] samples)
        {
            var myPlot = chart.Plot;
            myPlot.Title("寡糖分析");
            myPlot.Font.Automatic();
            myPlot.Axes.Top.IsVisible = false;
            myPlot.Axes.Right.IsVisible = false;
            Legend legend = myPlot.ShowLegend(Edge.Bottom).Legend;
            ScottPlot.Palettes.Category10 palette = new();
            List<Bar> bars = new List<Bar>();
            List<LegendItem> legendItems = legend.ManualItems;
            int rowCount = samples[0].DP.Length;
            int colCount = samples.Length;

            for (int col = 0; col < colCount; ++col)
            {
                var sample = samples[col];
                legendItems.Add(new LegendItem() { LabelText = sample.SampleName, FillColor = palette.GetColor(col) });
                for (int row = 0; row < rowCount; ++row)
                {
                    Bar bar = new()
                    {
                        Position = row * (colCount + 1) + col,
                        FillColor = palette.GetColor(col),
                        Value = sample.Rows[row].Average.GetValueOrDefault(),
                        Error = sample.Rows[row].StdDev,
                        BorderLineWidth = 1f,
                    };
                    bars.Add(bar);
                }
            }
            Tick[] ticks = new Tick[rowCount];
            for (int i = 0; i < rowCount; ++i)
            {
                ticks[i] = new Tick(i * (colCount + 1) + colCount / 2 - 1, "dp" + samples[0].DP[i]);
            }
            myPlot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);

            myPlot.Add.Bars(bars);

            myPlot.Axes.Bottom.MajorTickStyle.Length = 0;
            myPlot.HideGrid();

            // tell the plot to autoscale with no padding beneath the bars
            myPlot.Axes.Margins(bottom: 0);
            chart.Refresh();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}

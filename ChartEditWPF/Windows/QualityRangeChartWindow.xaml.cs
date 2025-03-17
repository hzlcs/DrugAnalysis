using ChartEditLibrary.Model;
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
            
            myPlot.Axes.Top.IsVisible = false;
            myPlot.Axes.Right.IsVisible = false;
            var legend = myPlot.ShowLegend(Edge.Bottom).Legend;
            ScottPlot.Palettes.Category10 palette = new();
            List<Bar> bars = new List<Bar>();
            var legendItems = legend.ManualItems;
            var rowCount = samples[0].Descriptions.Length;
            var colCount = samples.Length;

            for (var col = 0; col < colCount; ++col)
            {
                var sample = samples[col];
                bool database = sample.database is not null;
                legendItems.Add(new LegendItem() { LabelText = sample.SampleName + (database ? "均值" : ""), FillColor = palette.GetColor(col) });
                for (var row = 0; row < rowCount; ++row)
                {
                    double value;
                    double error;
                    if (!database)
                    {
                        value = sample.DataRows[row].Data[0].Average.GetValueOrDefault();
                        error = sample.DataRows[row].Data[0].StdDev;
                    }
                    else
                    {
                        var values = sample.DataRows[row].Data.Select(v => v.Average.GetValueOrDefault()).ToArray();
                        value = values.Average();
                        error = AreaDatabase.CalculateStdDev(values);
                    }
                    Bar bar = new()
                    {
                        Position = row * (colCount + 1) + col,
                        FillColor = palette.GetColor(col),
                        Value = value,
                        Error = error,
                        BorderLineWidth = 1f,
                    };
                    bars.Add(bar);
                }
            }
            var ticks = new Tick[rowCount];
            string description = samples[0].Description;
            string[] desc = samples[0].Descriptions;
            if (description == DescriptionManager.Glu)
            {
                description = "";
                desc = DescriptionManager.GetShortGluDescription(desc);
            }
            for (var i = 0; i < rowCount; ++i)
            {
                ticks[i] = new Tick(i * (colCount + 1) + colCount / 2, description + desc[i]);
            }
            myPlot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);

            myPlot.Add.Bars(bars);
            myPlot.Axes.Bottom.MajorTickStyle.Length = 0;
            myPlot.HideGrid();

            // tell the plot to autoscale with no padding beneath the bars
            myPlot.Axes.Margins(bottom: 0);
            myPlot.Font.Set("微软雅黑");
            chart.Refresh();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

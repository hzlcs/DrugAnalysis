using ChartEditLibrary;
using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditWPF.Models;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static ChartEditLibrary.Model.HotChartManager;
using static ChartEditLibrary.Model.PCAManager;
using Color = System.Drawing.Color;
using LinearGradientBrush = System.Drawing.Drawing2D.LinearGradientBrush;

namespace ChartEditWPF.Behaviors
{
    internal class ChartPlot : ScottPlot.WPF.WpfPlot
    {

        private IChartControl chartControl = null!;
        public IChartControl ChartControl
        {
            get
            {
                return chartControl;
            }
            set
            {
                SetValue(ChartControlProperty, value);
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            ChartControl.OnFocusChanged(true);
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            ChartControl.OnFocusChanged(false);
            base.OnLostFocus(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            //ChartControl.AfterMouseWheel();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var point = e.GetPosition(this);
            var mousePoint = Plot.GetCoordinates(new ScottPlot.Pixel((float)point.X * DisplayScale, (float)point.Y * DisplayScale));
            ChartControl.MouseDown(mousePoint, e.LeftButton == MouseButtonState.Pressed);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            ChartControl.MouseUp();
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var point = e.GetPosition(this);
            var mousePoint = Plot.GetCoordinates(new ScottPlot.Pixel((float)point.X * DisplayScale, (float)point.Y * DisplayScale));
            ChartControl.MouseMove(mousePoint);
            base.OnMouseMove(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
                return;
            ChartControl.KeyDown(e.Key.ToString());
            base.OnKeyDown(e);
        }


        // Using a DependencyProperty as the backing store for ChartControl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChartControlProperty =
            DependencyProperty.Register("ChartControl", typeof(IChartControl), typeof(ChartPlot), new PropertyMetadata(null, OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chartPlot = (ChartPlot)d;
            if (e.NewValue is IChartControl chartControl)
            {
                chartPlot.chartControl = chartControl;
                chartControl.BindControl(chartPlot);
            }
        }
    }

    internal class PCAChartPlot : ScottPlot.WPF.WpfPlot
    {

        public double[] SingularValues { get; set; } = null!;

        public SamplePCA[] Samples
        {
            get { return (SamplePCA[])GetValue(SamplesProperty); }
            set { SetValue(SamplesProperty, value); }
        }

        public PCAChartPlot()
        {
            Plot.HideGrid();
            Plot.Legend.IsVisible = true;
            Plot.Font.Automatic();
        }

        public static readonly DependencyProperty SamplesProperty =
            DependencyProperty.Register("Samples", typeof(SamplePCA[]), typeof(PCAChartPlot), new PropertyMetadata(null, SampleChanged));

        private static void SampleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not SamplePCA[] samples)
                return;
            if (d is not PCAChartPlot chart)
                return;
            chart.Plot.Clear();
            var plot = chart.Plot;
            var xMax = (int)Math.Ceiling(chart.SingularValues[0]) + 1;
            var yMax = (int)Math.Ceiling(chart.SingularValues[1]) + 1;
            System.Drawing.Color lineColor = System.Drawing.Color.DarkGray;

            //x
            var xLine = plot.Add.Line(-xMax, 0, xMax, 0);
            xLine.Color = ScottPlot.Color.FromColor(lineColor);
            xLine.LineWidth = 2;
            //y
            var yLine = plot.Add.Line(0, -yMax, 0, yMax);
            yLine.Color = ScottPlot.Color.FromColor(lineColor);
            yLine.LineWidth = 2;
            ScottPlot.Palettes.Category10 palette = new();

            var index = 0;
            foreach (var sample in samples)
            {
                plot.Legend.ManualItems.Add(new ScottPlot.LegendItem() { LabelText = sample.ClassName, FillColor = palette.GetColor(index) });
                for (var i = 0; i < sample.Points.Length; ++i)
                {
                    plot.Add.Marker(sample.Points[i].X, sample.Points[i].Y, color: palette.GetColor(index));
                    var txt = plot.Add.Text(sample.SampleNames[i], sample.Points[i].X + 0.2 * plot.PlotControl!.DisplayScale, sample.Points[i].Y);
                    txt.Alignment = ScottPlot.Alignment.MiddleCenter;
                    txt.LabelAlignment = ScottPlot.Alignment.MiddleCenter;
                    txt.LabelFontSize = 16;
                }
                ++index;
            }
            var ellips = plot.Add.Ellipse(0, 0, chart.SingularValues[0], chart.SingularValues[1]);
            ellips.LineColor = ScottPlot.Color.FromColor(lineColor);
            ellips.LineWidth = 2;
            plot.Legend.FontSize = 24;
            plot.Axes.SetLimits(-xMax, xMax, -yMax, yMax);
            plot.Axes.Bottom.TickLabelStyle.FontSize = 24;
            plot.Axes.Left.TickLabelStyle.FontSize = 24;
            plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(1);
            plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(1);
        }
    }

    internal class HotChartPlot : ScottPlot.WPF.WpfPlot
    {
        static readonly ScottPlot.Color[][] colors;
        static HotChartPlot()
        {
            Color[] targetColors = [Color.Green, Color.Blue, Color.Red, Color.Purple, Color.Gold];
            var temp = targetColors.Select(v => GetSingleColorList(Color.White, v, ColorCount + 1)).ToArray();
            colors = temp.Select(v => v.Select(x => Extension.ToScottColor(x)).ToArray()).ToArray();
        }

        private HotChartDataDetail detail = null!;
        public HotChartDataDetail Detail
        {
            get => detail;
            set
            {
                detail = value;
                DataChanged(value);
            }
        }

        public (DescData[], Dictionary<string, Dictionary<int, DescData[]>>) Detail2
        {
            set
            {
                DrawCenter(value.Item1);
                DrawDetail(value.Item2);
            }
        }

        public HotChartPlot()
        {
            Plot.Axes.SquareUnits();
            Plot.Axes.Frameless();
            //Plot.HideGrid();
        }


        private void DataChanged(HotChartDataDetail data)
        {
            
            DrawData(data);
            Refresh();
            //this.Plot.Axes.AutoScale();
        }

        private static readonly double DetailStart = 2;
        private static readonly double DetailInterval = 0.5;
        private static readonly int ColorCount = 20;
        private static readonly float lineWidth = 1.1f;
        private static readonly ScottPlot.Color lineColor = Extension.ToScottColor(Color.Black);

        private void DrawData(HotChartDataDetail detail)
        {

            DrawMain(detail);

        }

        private void DrawMain(HotChartDataDetail detail)
        {
            double totalRadian = detail.Scale;


            //画线
            var datas = detail.Datas.SelectMany(v => v.Value).ToArray();
            var sum = datas.Sum(v => v.Value.GetValueOrDefault());
            var dict = datas.GroupBy(v => v.Description).ToDictionary(v => v.Key, v => v.Sum(x => x.Value.GetValueOrDefault()));
            double currentRadin = 0;
            double innerR = DetailStart;
            double outerR = DetailStart + DetailInterval * detail.Datas.Count;

            List<SampleData> sampleDatas = [];
            foreach (var kv in detail.Datas)
                foreach (var i in kv.Value)
                    sampleDatas.Add(new SampleData(kv.Key, i.Description, i.Value));
            var sampleGroup = sampleDatas.GroupBy(v => v.Description).ToDictionary(v => v.Key, v => v.ToArray());
            int colorIndex = DescriptionManager.ComDescription.GetDescriptionStart(int.Parse(detail.Description))[0] - 'a';
            var lineColors = colors[colorIndex];
            double baseValue = sampleDatas.Select(v => v.value.GetValueOrDefault()).Max();

            foreach (var kv in dict)
            {
                double radin = kv.Value / sum * Math.PI * 2 * totalRadian;
                radin = 1.0 / dict.Count * Math.PI * 2 * totalRadian;
                var x = Math.Cos(currentRadin + radin);
                var y = Math.Sin(currentRadin + radin);
                Angle start = Angle.FromRadians(currentRadin);
                Angle end = Angle.FromRadians(radin);
                var sampleInnerR = DetailStart;
                foreach (var sample in sampleGroup[kv.Key])
                {
                    var rate = sample.value.GetValueOrDefault() / baseValue;
                    int index = (int)Math.Ceiling(rate * ColorCount);
                    var color = lineColors[index];
                    var fill = Plot.Add.AnnularSector(default, sampleInnerR + DetailInterval, sampleInnerR, default, end, start);
                    fill.FillColor = color;
                    fill.LineWidth = 0;
                    sampleInnerR += DetailInterval;
                }
                SetLineStyle(Plot.Add.Line(innerR * x, innerR * y, outerR * x, outerR * y));
                currentRadin += radin;
            }
            SetLineStyle(Plot.Add.Line(innerR, 0, outerR, 0));

            double r = DetailStart - DetailInterval;
            //画圆
            if (Utility.ToleranceEqual(totalRadian, 1))
            {
                var txt = Plot.Add.Text($"dp{detail.Description}\n{detail.area:P2}", 0, 0);
                txt.Alignment = Alignment.MiddleCenter;
                txt.LabelFontSize = 18;
                SetLineStyle(Plot.Add.Circle(default, 1));
                for (int i = 0; i <= detail.Datas.Count; ++i)
                {
                    r += DetailInterval;
                    SetLineStyle(Plot.Add.Circle(default, r));
                }
            }
            else
            {
                Angle start = Angle.FromRadians(0);
                Angle end = Angle.FromRadians(totalRadian * Math.PI * 2);
                SetLineStyle(Plot.Add.AnnularSector(default, 1, 0, start, end));
                var txt = Plot.Add.Text($"dp{detail.Description}\n{detail.area:P2}", 0.5 * Math.Cos(totalRadian * Math.PI), 0.5 * Math.Sin(totalRadian * Math.PI));
                txt.Alignment = Alignment.MiddleCenter;
                txt.LabelFontSize = 18;
                for (int i = 0; i <= detail.Datas.Count; ++i)
                {
                    r += DetailInterval;
                    SetLineStyle(Plot.Add.Arc(default, r, start, end));
                }
            }
            r += 0.5;
            //Plot.Axes.Left.Max = r;
            //Plot.Axes.Left.Min = -r;
            Plot.Axes.SetLimits(-r, r, -r, r);
        }

        private static void SetLineStyle(LinePlot line)
        {
            line.Color = lineColor;
            line.LineWidth = lineWidth;
        }

        private static void SetLineStyle(Ellipse ellipse)
        {
            ellipse.LineColor = lineColor;
            ellipse.LineWidth = lineWidth;
        }

        record SampleData(string SampleName, string Description, double? value);

        private static Color[] GetSingleColorList(Color srcColor, Color desColor, int count)
        {
            List<Color> colorFactorList = new List<Color>();
            int redSpan = desColor.R - srcColor.R;
            int greenSpan = desColor.G - srcColor.G;
            int blueSpan = desColor.B - srcColor.B;
            for (int i = 0; i < count + 1; i++)
            {
                Color color = Color.FromArgb(255,
                    srcColor.R + (int)((double)i / count * redSpan),
                    srcColor.G + (int)((double)i / count * greenSpan),
                    srcColor.B + (int)((double)i / count * blueSpan)
                );
                colorFactorList.Add(color);
            }
            return colorFactorList.ToArray();
        }

        readonly Dictionary<char, double> centerRadin = [];
        private void DrawCenter(DescData[] datas)
        {
            Array.Sort(datas, MainComparison);
            double sum = datas.Select(v => v.Value.GetValueOrDefault()).Sum();
            Plot.Add.Circle(0, 0, 1);
            Coordinates start = new Coordinates(0, 0);
            double currentRadin = 0;
            foreach (var degree in datas)
            {
                var radin = degree.Value.GetValueOrDefault() / sum * Math.PI * 2;
                var d = DescriptionManager.ComDescription.GetDescriptionStart(int.Parse(degree.Description))[0];
                centerRadin[d] = radin;
                Coordinates end = new Coordinates(Math.Cos(currentRadin + radin), Math.Sin(currentRadin + radin));
                Plot.Add.Line(start.X, start.Y, end.X, end.Y);
                Coordinates labelEnd = new Coordinates(Math.Cos(currentRadin + radin / 2) * 0.5, Math.Sin(currentRadin + radin / 2) * 0.5);
                Plot.Add.Text("dp" + degree.Description, labelEnd.X, labelEnd.Y);
                currentRadin += radin;
            }
        }



        private void DrawDetail(Dictionary<string, Dictionary<int, DescData[]>> datas)
        {

            var allDatas = datas.SelectMany(v => v.Value).SelectMany(v => v.Value).GroupBy(v => v.Description[0])
                .ToDictionary(v => v.Key, v => v.ToArray());
            double curRadin = 0;
            double innerRadiu = DetailStart;
            double outerRadiu = DetailStart + DetailInterval * datas.Count;
            foreach (var kv in allDatas)
            {
                char d = kv.Key;
                var values = kv.Value;
                Array.Sort(values, DescriptionComparer);
                var sum = values.Sum(v => v.Value.GetValueOrDefault());
                var color = colors[d - 'a'][5];
                foreach (var data in values)
                {
                    double radin = data.Value.GetValueOrDefault() / sum * centerRadin[d];
                    radin = 1.0 / values.Length * centerRadin[d]; 
                    curRadin += radin;
                    double x = Math.Cos(curRadin);
                    double y = Math.Sin(curRadin);
                    Coordinates start = new Coordinates(x * innerRadiu, y * innerRadiu);
                    Coordinates end = new Coordinates(x * outerRadiu, y * outerRadiu);
                    Plot.Add.Line(start, end).Color = color;
                }
            }


            Plot.Add.Circle(0, 0, innerRadiu);
            double radiu = innerRadiu;
            foreach (var kv in datas)
            {
                radiu += DetailInterval;
                Plot.Add.Circle(0, 0, radiu);
            }


        }

        static IComparer<DescData> DescriptionComparer = Comparer<DescData>.Create(DescriptionComparison);

        private static int DescriptionComparison(DescData x, DescData y)
        {
            if (x.Description[0] != y.Description[0])
                return x.Description[0] - y.Description[0];
            return int.Parse(x.Description[1..]) - int.Parse(y.Description[1..]);
        }

        private static int MainComparison(DescData x, DescData y)
        {
            return int.Parse(x.Description) - int.Parse(y.Description);
        }
    }

}

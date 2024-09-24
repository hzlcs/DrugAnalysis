using ChartEditLibrary.Interfaces;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static ChartEditLibrary.Model.PCAManager;

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

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var point = e.GetPosition((IInputElement)e.Source);
            ChartControl.MouseDown(this, new System.Drawing.PointF((float)point.X * DisplayScale, (float)point.Y * DisplayScale), e.LeftButton == MouseButtonState.Pressed);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            ChartControl.MouseUp(this);
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var point = e.GetPosition(this);
            ChartControl.MouseMove(this, new System.Drawing.PointF((float)point.X * DisplayScale, (float)point.Y * DisplayScale));
            base.OnMouseMove(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            ChartControl.KeyDown(this, e.Key.ToString());
            base.OnKeyDown(e);
        }

        // Using a DependencyProperty as the backing store for ChartControl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChartControlProperty =
            DependencyProperty.Register("ChartControl", typeof(IChartControl), typeof(ChartPlot), new PropertyMetadata(null, OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChartPlot chartPlot = (ChartPlot)d;
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

        // Using a DependencyProperty as the backing store for SamplesProperty.  This enables animation, styling, binding, etc...
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
            int xMax = (int)Math.Ceiling(chart.SingularValues[0]) + 1;
            int yMax = (int)Math.Ceiling(chart.SingularValues[1]) + 1;
            plot.Add.Line(-xMax, 0, xMax, 0);
            plot.Add.Line(0, -yMax, 0, yMax);
            ScottPlot.Palettes.Category10 palette = new();
            int index = 0;
            foreach (var sample in samples)
            {
                plot.Legend.ManualItems.Add(new ScottPlot.LegendItem() { LabelText = sample.ClassName, FillColor = palette.GetColor(index) });
                for (int i = 0; i < sample.Points.Length; ++i)
                {
                    plot.Add.Marker(sample.Points[i].X, sample.Points[i].Y, color: palette.GetColor(index));
                    plot.Add.Text(sample.SampleNames[i], sample.Points[i].X, sample.Points[i].Y);

                }
                ++index;
            }
            plot.Add.Ellipse(0, 0, chart.SingularValues[0], chart.SingularValues[1]);

        }
    }

}

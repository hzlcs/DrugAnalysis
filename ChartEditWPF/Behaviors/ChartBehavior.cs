using ChartEditLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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
            ChartControl.MouseMove(this, new System.Drawing.PointF((float)point.X * DisplayScale, (float)point.Y *DisplayScale));
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
            if(e.NewValue is IChartControl chartControl)
            {
                chartPlot.chartControl = chartControl;
                chartControl.BindControl(chartPlot);
            }
        }

        
    }
}

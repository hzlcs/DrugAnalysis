using ChartEditLibrary.Interfaces;
using ChartEditWPF.Models;
using LanguageExt.ClassInstances.Pred;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LinearGradientBrush = System.Drawing.Drawing2D.LinearGradientBrush;

namespace ChartEditWPF.Behaviors
{
    internal sealed class ChartPlot : ScottPlot.WPF.WpfPlot
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

}

using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ScottPlot;
using ScottPlot.Palettes;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Interfaces
{
    public interface IChartControl
    {
        IPlotControl PlotControl { get; }

        DraggableChartVm ChartData { get; set; }

        void MouseDown(object? sender, PointF mousePoint, bool left);

        void MouseMove(object? sender, PointF mousePoint);

        void MouseUp(object? sender);

        void KeyDown(object? sender, string key);

        void AutoFit();

        byte[] GetImage();

        void BindControl(IPlotControl chartPlot);

        void ChangeActived(bool actived);

    }

    public abstract class ChartControl(IMessageBox messageBox, IFileDialog dialog, IInputForm inputForm) : IChartControl
    {
        public IPlotControl PlotControl { get; set; } = null!;
        public DraggableChartVm ChartData { get; set; } = null!;
        protected Text? MyHighlightText;
        protected Coordinates mouseCoordinates;
        protected DraggedLineInfo? draggedLine;
        protected CoordinateLine oldBaseLine;
        protected readonly IMessageBox _messageBox = messageBox;
        protected readonly IFileDialog _dialog = dialog;
        protected readonly IInputForm _inputForm = inputForm;

        protected Vector2d GetSensitivity()
        {
            return new Vector2d(PlotControl.Plot.Axes.Bottom.Width / 50, PlotControl.Plot.Axes.Left.Height / 50);
        }

        public void AutoFit()
        {
            PlotControl.Plot.Axes.AutoScale();
            PlotControl.Refresh();
        }

        public virtual void BindControl(IPlotControl chartPlot)
        {
            PlotControl = chartPlot;
            chartPlot.Plot.Clear();
            chartPlot.Plot.XLabel(ChartData.FileName);
            MyHighlightText = chartPlot.Plot.Add.Text("", 0, 0);
            MyHighlightText.IsVisible = false;

            var source = chartPlot.Plot.Add.ScatterPoints(ChartData.DataSource);
            source.Color = ScottPlot.Color.FromARGB((uint)System.Drawing.Color.SkyBlue.ToArgb());
            source.MarkerSize = 3;
            foreach (var baseLine in ChartData.BaseLines)
            {
                chartPlot.AddBaseLine(baseLine, ChartData);
            }
            ChartData.BaseLines.CollectionChanged += BaseLines_CollectionChanged;
            foreach (var i in ChartData.SplitLines)
            {
                chartPlot.AddSplitLine(i);
            }
            ChartData.SplitLines.CollectionChanged += VerticalLines_CollectionChanged;

            var plot = chartPlot.Plot;
            plot.FigureBackground.Color = System.Drawing.Color.White.ToScottColor();
            plot.Grid.IsVisible = false;
            chartPlot.Plot.Axes.AutoScale();
            chartPlot.Refresh();
            chartPlot.Menu.Clear();
        }

        private void BaseLines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (BaseLine item in e.NewItems!)
                {
                    PlotControl.AddBaseLine(item, ChartData);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (BaseLine item in e.OldItems!)
                {
                    PlotControl.RemoveEditLine(item);
                }
            }
        }

        private void VerticalLines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (SplitLine item in e.NewItems!)
                {
                    PlotControl.AddSplitLine(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (SplitLine item in e.OldItems!)
                {
                    PlotControl.RemoveEditLine(item);
                }
            }
        }

        public byte[] GetImage()
        {
            PixelSize size = new(1920, 1080);
            return PlotControl.Plot.GetImageBytes((int)size.Width, (int)size.Height, ImageFormat.Png);
        }

        public void KeyDown(object? sender, string key)
        {
            Coordinates? markPoint = null;
            if (key == "A")
            {
                if (!ChartData.DraggedLine.HasValue || ChartData.DraggedLine.Value.DraggedLine is not SplitLine sl)
                    return;
                var point = ChartData.GetChartPoint(sl.Start.X - ChartData.Unit);
                if (point.HasValue && point.Value.X >= sl.BaseLine.Start.X)
                {
                    ChartData.DraggedLine.Value.DraggedLine.Line = sl.BaseLine.CreateSplitLine(point.Value);
                    markPoint = point;
                }
            }
            else if (key == "D")
            {
                if (!ChartData.DraggedLine.HasValue || ChartData.DraggedLine.Value.DraggedLine is not SplitLine sl)
                    return;
                var point = ChartData.GetChartPoint(sl.Start.X + ChartData.Unit);
                if (point.HasValue && point.Value.X < sl.BaseLine.End.X)
                {
                    ChartData.DraggedLine.Value.DraggedLine.Line = sl.BaseLine.CreateSplitLine(point.Value);
                    markPoint = point;
                }
            }
            else
            {
                return;
            }
            if (markPoint.HasValue && MyHighlightText is not null)
            {
                MyHighlightText.IsVisible = true;
                var value = markPoint.Value;
                MyHighlightText.Location = value;
                MyHighlightText.LabelText = $"({value.X: 0.000}, {value.Y: 0.000})";
            }
            PlotControl.Refresh();
        }

        public abstract void MouseDown(object? sender, PointF mousePoint, bool left);

        public abstract void MouseMove(object? sender, PointF mousePoint);

        public abstract void MouseUp(object? sender);
        public virtual void ChangeActived(bool actived) { }
    }


}

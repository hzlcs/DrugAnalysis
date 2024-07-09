using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Interfaces
{
    public interface IChartControl
    {
        IPlotControl PlotControl { get; }

        DraggableChartVM ChartData { get; set; }

        void MouseDown(object? sender, PointF mousePoint, bool left);

        void MouseMove(object? sender, PointF mousePoint);

        void MouseUp(object? sender);

        void KeyDown(object? sender, string key);

        void ClearLineMenu(IPlotControl control);

        void SaveDataMenu(IPlotControl control);

        void RemoveLineMenu(IPlotControl control);

        void AddLineMenu(IPlotControl control);

        void SetDPMenu(IPlotControl control);

        void AutoFit();

        byte[] GetImage();

        void BindControl(IPlotControl chartPlot);
    }

    public class ChartControl(IMessageBox messageBox, IFileDialog dialog, IInputForm inputForm) : IChartControl
    {
        public IPlotControl PlotControl { get; set; } = null!;
        public DraggableChartVM ChartData { get; set; } = null!;
        private Text? MyHighlightText;
        private Coordinates mouseCoordinates;
        private DraggedLineInfo? draggedLine;
        private CoordinateLine oldBaseLine;
        private readonly IMessageBox _messageBox = messageBox;
        private readonly IFileDialog _dialog = dialog;
        private readonly IInputForm _inputForm = inputForm;

        public void BindControl(IPlotControl chartPlot)
        {
            PlotControl = chartPlot;
            chartPlot.Plot.Clear();
            chartPlot.Plot.XLabel(ChartData.FileName);
            MyHighlightText = chartPlot.Plot.Add.Text("", 0, 0);
            MyHighlightText.IsVisible = false;

            chartPlot.Menu.Add("Add Line", AddLineMenu);
            chartPlot.Menu.Add("Remove Line", RemoveLineMenu);
            chartPlot.Menu.Add("Clear These Line", ClearLineMenu);
            chartPlot.Menu.Add("Save Data", SaveDataMenu);
            chartPlot.Menu.Add("Set DP", SetDPMenu);

            var source = chartPlot.Plot.Add.ScatterPoints(ChartData.DataSource);
            source.Color = ScottPlot.Color.FromARGB((uint)System.Drawing.Color.SkyBlue.ToArgb());
            source.MarkerSize = 3;

            chartPlot.AddEditLine(ChartData.BaseLine);
            foreach (var i in ChartData.SplitLines)
            {
                chartPlot.AddEditLine(i);
            }
            ChartData.SplitLines.CollectionChanged += VerticalLines_CollectionChanged;
            //chartData.SplitLines[chartData.SplitLines.Count - 1].TrySetDPIndex(2);
            //chartPlot.PerformAutoScale();
            var plot = chartPlot.Plot;
            plot.FigureBackground.Color = System.Drawing.Color.White.ToScottColor();
            plot.Grid.IsVisible = false;
            chartPlot.Plot.Axes.AutoScale();
            chartPlot.Refresh();

                
            
        }

        private void VerticalLines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (SplitLine item in e.NewItems!)
                {
                    PlotControl.AddEditLine(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (SplitLine item in e.OldItems!)
                {
                    PlotControl.RemoveSplitLine(item);
                }
            }
        }

        private Vector2d GetSensitivity()
        {
            return new Vector2d(PlotControl.Plot.Axes.Bottom.Width / 50, PlotControl.Plot.Axes.Left.Height / 50);
        }
        public void MouseDown(object? sender, PointF mousePoint, bool left)
        {
            if (ChartData is null)
                return;

            Pixel mousePixel = new(mousePoint.X, mousePoint.Y);
            mouseCoordinates = PlotControl.Plot.GetCoordinates(mousePixel);
            Debug.WriteLine(mouseCoordinates);
            ChartData.Sensitivity = GetSensitivity();



            if (left)
            {
                draggedLine = ChartData.GetDraggedLine(mouseCoordinates);
                if (draggedLine is not null)
                {
                    if (draggedLine.Value.IsBaseLine)
                        oldBaseLine = draggedLine.Value.DraggedLine.Line;
                    var value = draggedLine.Value.GetMarkPoint();
                    if (MyHighlightText is not null)
                    {
                        MyHighlightText.IsVisible = true;
                        MyHighlightText.Location = value;
                        MyHighlightText.LabelText = $"({value.X: 0.000}, {value.Y: 0.000})";
                    }
                    PlotControl.Refresh();
                    PlotControl.Interaction.Disable();
                }
            }
        }

        private void MoveLine(Coordinates? chartPoint, Coordinates mouseLocation)
        {
            if (!draggedLine.HasValue)
                return;
            var line = draggedLine.Value;
            var editLine = line.DraggedLine;
            if (line.IsBaseLine)
            {
                chartPoint ??= mouseLocation;
                if (line.IsStart)
                {
                    if (editLine.Start.Equals(chartPoint.Value))
                        return;
                    editLine.Start = chartPoint.Value;
                }
                else
                {
                    if (editLine.End.Equals(chartPoint.Value))
                        return;
                    editLine.End = chartPoint.Value;
                }
            }
            else
            {
                chartPoint ??= editLine.End;
                if (chartPoint.Value.X == editLine.Start.X)
                    return;
                editLine.Line = ChartData!.CreateSplitLine(chartPoint.Value);
            }
        }

        public void MouseMove(object? sender, PointF mousePoint)
        {
            if (ChartData is null)
                return;
            if (draggedLine is null)
                return;
            Pixel mousePixel = new(mousePoint.X, mousePoint.Y);
            Coordinates mouseLocation = PlotControl.Plot.GetCoordinates(mousePixel);
            var line = draggedLine.Value;
            var chartPoint = ChartData.GetChartPoint(mouseLocation.X, line.IsBaseLine ? mouseLocation.Y : null);
            MoveLine(chartPoint, mouseLocation);
            var markPoint = draggedLine.Value.GetMarkPoint();
            if (MyHighlightText is not null)
            {
                MyHighlightText.Location = markPoint;
                MyHighlightText.LabelText = $"({markPoint.X: 0.000}, {markPoint.Y: 0.000})";
            }

            PlotControl.Refresh();
        }

        public void MouseUp(object? sender)
        {
            if (ChartData is null)
                return;
            if (draggedLine is null)
                return;
            var line = draggedLine.Value;
            if (line.IsBaseLine)
            {
                ChartData.UpdateBaseLine(oldBaseLine, line.DraggedLine.Line);
            }
            draggedLine = null;
            //ChartData.DraggedLine = null;
            if (MyHighlightText is not null)
                MyHighlightText.IsVisible = false;
            PlotControl.Interaction.Enable();
            PlotControl.Refresh();
        }

        public void ClearLineMenu(IPlotControl control)
        {
            var range = control.Plot.Axes.Bottom.Range;
            var lines = ChartData.SplitLines.Where(v => v.Start.X > range.Min && v.Start.X < range.Max).ToArray();
            if (!_messageBox.ConfirmOperation($"是否删除这{lines.Length}条分割线?"))
                return;
            foreach (var line in lines)
                ChartData.RemoveSplitLine(line);
            control.Refresh();
        }

        public void SaveDataMenu(IPlotControl control)
        {
            _dialog.FileName = ChartData.FileName + ".csv";
            if (_dialog.ShowDialog())
            {
                string fileName = _dialog.FileName;
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                string dir = Path.GetDirectoryName(fileName)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(fileName, ChartData.GetSaveContent(), Encoding.UTF8);
            }
        }

        public void RemoveLineMenu(IPlotControl control)
        {
            var lineInfo = ChartData.GetDraggedLine(mouseCoordinates);
            if (!lineInfo.HasValue)
            {
                _messageBox.Show("Can't remove line here");
            }
            else if (lineInfo.Value.IsBaseLine)
            {
                _messageBox.Show("Can't remove baseLine");
            }
            else
            {
                SplitLine line = (SplitLine)lineInfo.Value.DraggedLine;
                if (!_messageBox.ConfirmOperation($"确认移除分割线：({line.End.X}, {line.End.Y})?"))
                    return;
                ChartData.RemoveSplitLine(line);
                control.Refresh();
            }
        }

        public void AddLineMenu(IPlotControl control)
        {
            var chartPoint = ChartData.GetChartPoint(mouseCoordinates.X);
            if (chartPoint is null)
            {
                _messageBox.Show("Can't add line here");
            }
            else
            {
                var line = ChartData.AddSplitLine(chartPoint.Value);
                ChartData.DraggedLine = DraggableChartVM.GetFocusLineInfo(line);
                control.Refresh();
            }
        }

        public void SetDPMenu(IPlotControl control)
        {
            var lineInfo = ChartData.GetDraggedLine(mouseCoordinates, true);
            if (!lineInfo.HasValue)
            {
                _messageBox.Show("Can't set dp here");
            }
            else if (lineInfo.Value.IsBaseLine)
            {
                _messageBox.Show("Can't set baseLine");
            }
            else
            {
                SplitLine line = (SplitLine)lineInfo.Value.DraggedLine;
                if (_inputForm.TryGetInput(line.DP, out string value))
                {
                    if (!line.TrySetDPIndex(value, ChartData.SplitLines))
                    {
                        _messageBox.Show("无效的DP值");
                    }
                    else
                    {
                        ChartData.DraggedLine = null;
                        ChartData.DraggedLine = DraggableChartVM.GetFocusLineInfo(line);
                    }
                }

            }
        }

        public void AutoFit()
        {
            PlotControl.Plot.Axes.AutoScale();
            PlotControl.Refresh();
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
                if (!ChartData.DraggedLine.HasValue || ChartData.DraggedLine.Value.IsBaseLine)
                    return;
                var x = ChartData.DraggedLine.Value.DraggedLine.Start.X;
                var point = ChartData.GetChartPoint(x - ChartData.Unit);
                if (point.HasValue)
                {
                    ChartData.DraggedLine.Value.DraggedLine.Line = ChartData.CreateSplitLine(point.Value);
                    markPoint = point;
                }
            }
            else if (key == "D")
            {
                if (!ChartData.DraggedLine.HasValue || ChartData.DraggedLine.Value.IsBaseLine)
                    return;
                var x = ChartData.DraggedLine.Value.DraggedLine.Start.X;
                var point = ChartData.GetChartPoint(x + ChartData.Unit);
                if (point.HasValue)
                {
                    ChartData.DraggedLine.Value.DraggedLine.Line = ChartData.CreateSplitLine(point.Value);
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
    }

}

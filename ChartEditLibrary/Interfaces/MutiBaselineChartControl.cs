using ChartEditLibrary.Model;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Interfaces
{
    public class MutiBaselineChartControl(IMessageBox messageBox, IFileDialog dialog, IInputForm inputForm) : ChartControl(messageBox, dialog, inputForm)
    {
        public override void BindControl(IPlotControl chartPlot)
        {
            base.BindControl(chartPlot);
        }

        private SplitLine? baseLine_endLine;

        public override void MouseDown(object? sender, PointF mousePoint, bool left)
        {
            if (ChartData is null)
                return;

            Pixel mousePixel = new(mousePoint.X, mousePoint.Y);
            mouseCoordinates = PlotControl.Plot.GetCoordinates(mousePixel);
            Debug.WriteLine(mouseCoordinates);
            ChartData.Sensitivity = GetSensitivity();


            if (!left)
                return;

            draggedLine = ChartData.GetDraggedLine(mouseCoordinates);
            if (draggedLine is null)
            {
                var baseLine = ChartData.GetBaseLine(mouseCoordinates);
                if (baseLine is not null)
                    return;
                int index = ChartData.GetDateSourceIndex(mouseCoordinates.X);
                baseLine = new Model.BaseLine(mouseCoordinates, new Coordinates(ChartData.DataSource[index + 1].X, mouseCoordinates.Y));
                draggedLine = new DraggedLineInfo(baseLine, false);
                ChartData.AddBaseLine(baseLine);
                baseLine_endLine = ChartData.AddSplitLine(baseLine, baseLine.End);
            }

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

        public override void MouseMove(object? sender, PointF mousePoint)
        {
            if (ChartData is null)
                return;
            if (draggedLine is null)
                return;
            Pixel mousePixel = new(mousePoint.X, mousePoint.Y);
            var mouseLocation = PlotControl.Plot.GetCoordinates(mousePixel);
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
                    if (baseLine_endLine is not null)
                    {
                        var linePoint = ChartData.GetChartPoint(editLine.End.X);
                        if (linePoint.HasValue)
                        {
                            if(baseLine_endLine.Line.Start.X != linePoint.Value.X)
                            {
                                baseLine_endLine.Line = baseLine_endLine.BaseLine.CreateSplitLine(linePoint.Value);
                            }
                        }
                        
                    }
                }
            }
            else
            {
                chartPoint ??= editLine.End;
                var point = chartPoint.Value;
                if (point.X == editLine.Start.X)
                    return;
                SplitLine sl = (SplitLine)editLine;
                if (point.X >= sl.BaseLine.Start.X && point.X <= sl.BaseLine.End.X)
                    editLine.Line = sl.BaseLine.CreateSplitLine(point);
            }
        }

        public override void MouseUp(object? sender)
        {
            if (ChartData is null)
                return;
            if (draggedLine is null)
                return;
            var line = draggedLine.Value;
            if (line.DraggedLine is BaseLine baseLine)
            {
                ChartData.UpdateBaseLine(baseLine);
                baseLine_endLine = null;
            }
            draggedLine = null;
            //ChartData.DraggedLine = null;
            if (MyHighlightText is not null)
                MyHighlightText.IsVisible = false;
            PlotControl.Interaction.Enable();
            PlotControl.Refresh();

        }
    }

}

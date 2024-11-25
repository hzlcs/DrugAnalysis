using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using MathNet.Numerics.Distributions;
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

            
            chartPlot.Menu.Add("Add Line", AddLineMenu);
            chartPlot.Menu.Add("Remove Line", RemoveLineMenu);
            chartPlot.Menu.Add("Delete Peak", DeletePeakMenu);
            chartPlot.Menu.Add("Clear these lines", ClearTheseLineMenu);
            chartPlot.Menu.Add("Set Assignment", SetAssignmentMenu);
        }

        private void ClearTheseLineMenu(IPlotControl control)
        {
            var range = control.Plot.Axes.Bottom.Range;
            var lines = ChartData.SplitLines.Where(v => v.Start.X > range.Min && v.Start.X < range.Max).ToArray();
            if (!_messageBox.ConfirmOperation($"是否删除这{lines.Length}条分割线?"))
                return;
            foreach (var line in lines)
                ChartData.RemoveSplitLine(line);
            control.Refresh();
        }

        private void SetAssignmentMenu(IPlotControl control)
        {
            var lineInfo = ChartData.GetDraggedLine(mouseCoordinates, true);
            if (!lineInfo.HasValue)
            {
                _messageBox.Show("Can't set assignment here");
            }
            else if (lineInfo.Value.IsBaseLine)
            {
                _messageBox.Show("Can't set baseLine");
            }
            else
            {
                var line = (SplitLine)lineInfo.Value.DraggedLine;
                if (_inputForm.TryGetInput(line.Description, out var value))
                {
                    if (!TrySetDPIndex(line, value, ChartData.SplitLines))
                    {
                        _messageBox.Show("无效的DP值");
                    }
                    else
                    {
                        ChartData.DraggedLine = null;
                        ChartData.DraggedLine = DraggableChartVm.GetFocusLineInfo(line);
                    }
                }

            }
        }

        private static bool TrySetDPIndex(SplitLine line, string? value, IList<SplitLine> lines)
        {
            if (value is null)
            {
                line.Description = null;
                return true;
            }
            line.Description = value;
            line.UpdateUI();
            return true;
        }

        private void DeletePeakMenu(IPlotControl control)
        {
            var lineInfo = ChartData.GetDraggedLine(mouseCoordinates, true);
            if (!lineInfo.HasValue)
            {
                _messageBox.Show("No line here");
                return;
            }
            if (lineInfo.Value.IsBaseLine)
            {
                _messageBox.Show("Can't delete baseLine");
                return;
            }
            var line = (SplitLine)lineInfo.Value.DraggedLine;
            var baseLine = line.BaseLine;
            var lines = baseLine.SplitLines;
            if(lines.Count == 1)
            {
                ChartData.RemoveBaseLine(baseLine);
            }
            else
            {
                int index = lines.IndexOf(line);
                ChartData.RemoveSplitLine(line);
                if (index == 0)
                {
                    baseLine.Start = line.Start;
                }
                else if (index == lines.Count)
                {
                    baseLine.End = lines[^1].Start;
                }
                else
                {
                    var left = lines[..index];
                    var right = lines[index..];
                    ChartData.RemoveBaseLine(baseLine);
                    var lbaseLine = new BaseLine(baseLine.Start, left[^1].Start);
                    ChartData.AddBaseLine(lbaseLine);
                    foreach (var l in left)
                    {
                        ChartData.AddSplitLine(lbaseLine, l.End).Description = l.Description;
                    }
                    var rbaseLine = new BaseLine(line.Start, baseLine.End);
                    ChartData.AddBaseLine(rbaseLine);
                    foreach (var r in right)
                    {
                        ChartData.AddSplitLine(rbaseLine, r.End).Description = r.Description;
                    }
                }
            }

            control.Refresh();
        }

        private void RemoveLineMenu(IPlotControl control)
        {
            var lineInfo = ChartData.GetDraggedLine(mouseCoordinates);
            if (!lineInfo.HasValue)
            {
                _messageBox.Show("No line here");
                return;
            }
            ChartData.RemoveLine(lineInfo.Value.DraggedLine);
            control.Refresh();
        }

        private void AddLineMenu(IPlotControl control)
        {
            var chartPoint = ChartData.GetChartPoint(mouseCoordinates.X);
            if (chartPoint is null)
            {
                _messageBox.Show("Can't add line here");
                return;
            }
            var point = chartPoint.Value;
            var baseLine = ChartData.BaseLines.FirstOrDefault(v => v.Include(point.X));
            if (baseLine is null)
            {
                _messageBox.Show("Can't add line without baseLine");
                return;
            }
            var line = ChartData.AddSplitLine(chartPoint.Value);
            ChartData.DraggedLine = DraggableChartVm.GetFocusLineInfo(line);
            control.Refresh();
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
            if (draggedLine is null && actived)
            {
                var baseLine = ChartData.GetBaseLine(mouseCoordinates);
                if (baseLine is not null)
                    return;
                int index = ChartData.GetDateSourceIndex(mouseCoordinates.X);
                if (ChartData.DataSource[index].Y < mouseCoordinates.Y || mouseCoordinates.Y <= PlotControl.Plot.Axes.Left.Min)
                    return;
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

                    if (baseLine_endLine is not null)
                    {
                        if (chartPoint.Value.X < editLine.Line.Start.X)
                        {
                            return;
                        }

                        var linePoint = ChartData.GetChartPoint(editLine.End.X);
                        if (linePoint.HasValue)
                        {
                            int index = ChartData.GetDateSourceIndex(editLine.End.X);
                            var point = linePoint.Value;
                            while (point.X > editLine.End.X)
                                point = ChartData.DataSource[--index];
                            baseLine_endLine.Line = baseLine_endLine.BaseLine.CreateSplitLine(point);
                            Debug.Assert(editLine.End.X >= baseLine_endLine.Start.X);

                        }

                    }
                    editLine.End = chartPoint.Value;
                }
            }
            else
            {
                chartPoint ??= editLine.End;
                var point = chartPoint.Value;
                if (point.X == editLine.Start.X)
                    return;
                SplitLine sl = (SplitLine)editLine;
                if ((point.X >= sl.BaseLine.Start.X || point.X > sl.Start.X) && (point.X <= sl.BaseLine.End.X || point.X < sl.End.X))
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
                if(baseLine_endLine is not null)
                {
                    if(Math.Abs(baseLine.Start.X - baseLine.End.X) < ChartData.Unit * 2)
                    {
                        ChartData.RemoveBaseLine(baseLine);
                    }
                    else
                    {
                        ChartData.GenerateSplitLine(baseLine);
                    }
                }
                else
                {
                    ChartData.UpdateBaseLine(baseLine);
                }
                
                baseLine_endLine = null;
            }
            draggedLine = null;
            //ChartData.DraggedLine = null;
            if (MyHighlightText is not null)
                MyHighlightText.IsVisible = false;
            PlotControl.Interaction.Enable();
            PlotControl.Refresh();

        }

        private bool actived;
        public override void ChangeActived(bool actived)
        {
            base.ChangeActived(actived);
            this.actived = actived;
        }
    }

}

using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using MathNet.Numerics.Distributions;
using ScottPlot;
using ScottPlot.Plottables;
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
            vstreetMarker = chartPlot.Plot.Add.Marker(0, 0, size: 2, color: System.Drawing.Color.Red.ToScottColor());
            vstreetMarker.IsVisible = false;

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
                string dp = _inputForm.ShowCombboxDialog("Set Assignment", SampleDescription.GluDescriptions);
                if (!TrySetDPIndex(line, dp))
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

        private static bool TrySetDPIndex(SplitLine line, string? value)
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
            if (lines.Count == 1)
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

        public override void MouseDown(Coordinates mousePoint, bool left)
        {
            base.MouseDown(mousePoint, left);
            
            if (left && draggedLine is null && actived)
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
                PlotControl.Interaction.Disable();
            }

        }

        public override void MouseMove(Coordinates mousePoint)
        {
            base.MouseMove(mousePoint);
            if (draggedLine is null)
                return;
            var line = draggedLine.Value;
            var chartPoint = ChartData.GetChartPoint(mousePoint.X, line.IsBaseLine ? mousePoint.Y : null);
            MoveLine(chartPoint, mousePoint);
            
            

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

        public override void MouseUp()
        {
            base.MouseUp();
            
            if (draggedLine is null)
                return;
            var line = draggedLine.Value;
            if (line.DraggedLine is BaseLine baseLine)
            {
                if (baseLine_endLine is not null)
                {
                    baseLine_endLine.Line = baseLine.CreateSplitLine(baseLine_endLine.End);
                    if (Math.Abs(baseLine.Start.X - baseLine.End.X) < ChartData.Unit * 2)
                    {
                        ChartData.RemoveBaseLine(baseLine);
                    }
                    else
                    {
                        ChartData.GenerateSplitLine(baseLine);
                    }
                    baseLine_endLine = null;
                }
                else
                {
                    ChartData.UpdateBaseLine(baseLine);
                }
                if (ChartData.CheckCrossBaseLine(baseLine, out var point))
                {
                    if (_messageBox.ConfirmOperation("基线存在交叉，是否校正为切点？"))
                    {
                        baseLine.Start = point;
                        ChartData.UpdateBaseLine(baseLine);
                    };
                }
            }
            draggedLine = null;

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
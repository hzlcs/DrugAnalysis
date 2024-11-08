using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using OpenTK.Mathematics;
using ScottPlot;
using ScottPlot.Plottables;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace ChartEditLibrary.Interfaces
{
    public class SingleBaselineChartControl(IMessageBox messageBox, IFileDialog dialog, IInputForm inputForm) : ChartControl(messageBox, dialog, inputForm)
    {

        public override void BindControl(IPlotControl chartPlot)
        {
            base.BindControl(chartPlot);
            chartPlot.Menu.Add("Add Line", AddLineMenu);
            chartPlot.Menu.Add("Remove Line", RemoveLineMenu);
            chartPlot.Menu.Add("Clear These Line", ClearLineMenu);
            chartPlot.Menu.Add("Save Data", SaveDataMenu);
            chartPlot.Menu.Add("Set DP", SetDPMenu);
        }

        
        public override void MouseDown(object? sender, PointF mousePoint, bool left)
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
                var point = chartPoint.Value;
                if (point.X == editLine.Start.X)
                    return;
                SplitLine sl = (SplitLine)editLine;
                if(point.X >= sl.BaseLine.Start.X && point.X <= sl.BaseLine.End.X)
                    editLine.Line = sl.BaseLine.CreateSplitLine(point);
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
            if (_dialog.ShowDialog(ChartData.FileName + ".csv", out var fileNames))
            {
                var fileName = fileNames[0];
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                var dir = Path.GetDirectoryName(fileName)!;
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
                var line = (SplitLine)lineInfo.Value.DraggedLine;
                if (!_messageBox.ConfirmOperation($"确认移除分割线：({line.End.X}, {line.End.Y})?"))
                    return;
                ChartData.RemoveSplitLine(line);
                if (!string.IsNullOrWhiteSpace(line.DP))
                {
                    string[] dps = line.DP.Split('-');
                    if (dps.Length > 1 && int.TryParse(dps[0], out var dp) && int.TryParse(dps[1], out var index))
                    {
                        var lines = ChartData.SplitLines.Where(v => (v.DP?.StartsWith(dps[0])).GetValueOrDefault()).Reverse().ToArray();
                        if (lines.Length == 1)
                        {
                            lines[0].DP = dps[0];
                            lines[0].UpdateUI();
                        }
                        else
                        {
                            index = 0;
                            foreach (var l in lines)
                            {
                                l.DP = $"{dp}-{++index}";
                                l.UpdateUI();
                            }
                        }
                    }
                }
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
                ChartData.DraggedLine = DraggableChartVm.GetFocusLineInfo(line);
                var first = ChartData.SplitLines.FirstOrDefault(v => v.RTIndex > line.RTIndex);
                if (first is not null && !string.IsNullOrWhiteSpace(first.DP))
                {
                    string[] dps = first.DP.Split('-');
                    if (int.TryParse(dps[0], out var dp))
                    {
                        if (dps.Length == 1)
                        {
                            first.DP = dp + "-1";
                            first.UpdateUI();
                            line.DP = dp + "-2";
                            line.UpdateUI();
                        }
                        else
                        {
                            var index = int.Parse(dps[1]);
                            line.DP = $"{dp}-{++index}";
                            line.UpdateUI();
                            foreach (var l in ChartData.SplitLines.Reverse().Where(v => v.RTIndex < line.RTIndex &&
                            (v.DP?.StartsWith(dp.ToString())).GetValueOrDefault()))
                            {
                                l.DP = $"{dp}-{++index}";
                                l.UpdateUI();
                            }
                        }
                    }
                }
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
                var line = (SplitLine)lineInfo.Value.DraggedLine;
                if (_inputForm.TryGetInput(line.DP, out var value))
                {
                    if (!line.TrySetDPIndex(value, ChartData.SplitLines))
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

    }
}

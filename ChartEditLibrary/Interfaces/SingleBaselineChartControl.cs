using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using MathNet.Numerics.Distributions;
using OpenTK.Mathematics;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.Statistics;
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
                if (point.X >= sl.BaseLine.Start.X && point.X <= sl.BaseLine.End.X)
                    editLine.Line = sl.BaseLine.CreateSplitLine(point);
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

        public override void MouseUp()
        {
            base.MouseUp();
            if (draggedLine is null)
                return;
            var line = draggedLine.Value;
            if (line.DraggedLine is BaseLine baseLine)
            {
                ChartData.UpdateBaseLine(baseLine);
            }
            draggedLine = null;
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
                if (!string.IsNullOrWhiteSpace(line.Description))
                {
                    string[] dps = line.Description.Split('-');
                    if (dps.Length > 1 && int.TryParse(dps[0], out var dp) && int.TryParse(dps[1], out var index))
                    {
                        var lines = ChartData.SplitLines.Where(v =>
                        {
                            if (string.IsNullOrWhiteSpace(v.Description))
                                return false;
                            var _dps = v.Description.Split('-');
                            if (_dps[0] == dps[0])
                                return true;
                            return false;
                        }).Reverse().ToArray();
                        if (lines.Length == 1)
                        {
                            lines[0].Description = dps[0];
                            lines[0].UpdateUI();
                        }
                        else
                        {
                            index = 0;
                            foreach (var l in lines)
                            {
                                l.Description = $"{dp}-{++index}";
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
                if (first is not null && !string.IsNullOrWhiteSpace(first.Description))
                {
                    string[] dps = first.Description.Split('-');
                    if (int.TryParse(dps[0], out var dp))
                    {
                        if (dps.Length == 1)
                        {
                            first.Description = dp + "-1";
                            first.UpdateUI();
                            line.Description = dp + "-2";
                            line.UpdateUI();
                        }
                        else
                        {
                            var index = int.Parse(dps[1]);
                            line.Description = $"{dp}-{++index}";
                            line.UpdateUI();
                            foreach (var l in ChartData.SplitLines.Reverse().Where(v => v.RTIndex < line.RTIndex &&
                            (v.Description?.StartsWith(dp.ToString())).GetValueOrDefault()))
                            {
                                l.Description = $"{dp}-{++index}";
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

        /// <summary>
        /// 聚合度表
        /// </summary>
        private static readonly int[] DPTable = [2, 3, .. Enumerable.Range(2, 18).Select(v => v * 2)];

        /// <summary>
        /// 设置本线的DP值
        /// <para>第一次设置时，若<paramref name="value"/>为整数且所有左边的线都没有设置DP值，则依次设置所有左边的线的DP值</para>
        /// <para>Todo: 修改DP时智能改动其他峰的DP</para>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static bool TrySetDPIndex(SplitLine line, string? value, IList<SplitLine> lines)
        {
            if (value is null)
            {
                line.Description = null;
                return true;
            }
            if (line.Description is null && int.TryParse(value, out var dp))
            {
                if (lines.Take(line.Index).All(v => v.Description is null))
                {
                    //获取该DP在聚合度表中的索引
                    var index = Array.IndexOf(DPTable, dp);
                    if (index >= 0)
                    {
                        for (var i = line.Index - 1; i >= 0; --i)
                        {
                            lines[i].Description = DPTable.ElementAtOrDefault(index).ToString();
                            ++index;
                        }
                        return true;
                    }
                }

            }
            line.Description = value;
            line.UpdateUI();
            return true;
        }

        [Obsolete("智能设置DP值代开发")]
        private static void SetDPIndex(IList<SplitLine> sameLines, int index)
        {
            var dpValue = DPTable[index];
            if (sameLines.Count > 1)
            {
                for (var i = 0; i < sameLines.Count; i++)
                {
                    //sameLines[i].dpIndex = index;
                    sameLines[i].Description = $"{dpValue}-{i + 1}";
                }
            }
            else
            {
                //sameLines[0].dpIndex = index;
                sameLines[0].Description = dpValue.ToString();
            }
        }
    }
}

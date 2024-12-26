using ChartEditLibrary.Model;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.ViewModel
{
    public partial class DraggableChartVm
    {
        private bool IsEndPoint(Coordinates point)
        {
            int index = GetDateSourceIndex(point.X);
            if (index == -1)
                return false;
            return new CoordinateLine(point, DataSource[index]).Length < 1E-6;
        }


        private CoordinateLine GetEndPointLine(BaseLine baseLine)
        {
            Debug.Assert(BaseLines.Contains(baseLine));
            var line = baseLine.Line;
            Coordinates start = line.Start;
            if (!IsEndPoint(line.Start))
            {
                int index = GetDateSourceIndex(line.Start.X);
                int lineIndex = BaseLines.IndexOf(baseLine);
                int endIndex = lineIndex == 0 ? 0 : GetDateSourceIndex(BaseLines[lineIndex - 1].End.X);
                for (int i = index - 1; i > endIndex; --i)
                {
                    double y = line.Y(DataSource[i].X);
                    if (Math.Abs(y - DataSource[i].Y) < 1E-6)
                    {
                        start = DataSource[i];
                        break;
                    }
                }
            }
            Coordinates end = line.End;
            if (!IsEndPoint(line.End))
            {
                int index = GetDateSourceIndex(line.End.X);
                int lineIndex = BaseLines.IndexOf(baseLine);
                int endIndex = lineIndex == BaseLines.Count - 1 ? DataSource.Length - 1 : GetDateSourceIndex(BaseLines[lineIndex + 1].Start.X);
                for (int i = index + 1; i < endIndex; ++i)
                {
                    double y = line.Y(DataSource[i].X);
                    if (Math.Abs(y - DataSource[i].Y) < 1E-6)
                    {
                        end = DataSource[i];
                        break;
                    }
                }
            }
            return new CoordinateLine(start, end);
        }

        private bool IsVstreetPoint(Coordinates point)
        {
            var index = GetDateSourceIndex(point.X);
            if (index == -1)
                return false;
            return IsVstreetPoint(index);
        }

        private bool IsVstreetPoint(int index)
        {
            return DataSource[index].Y < DataSource[index - 1].Y && DataSource[index].Y < DataSource[index + 1].Y;
        }

        private Coordinates GetVstreetPoint(Coordinates point)
        {
            return GetVstreetPoint(GetDateSourceIndex(point.X));
        }

        private Coordinates GetVstreetPoint(int index)
        {
            int interval = 10;
            Coordinates point = DataSource[index];
            int end = index + interval;
            for (int i = index - interval; i < end; ++i)
            {
                if (DataSource[i].Y < point.Y)
                    point = DataSource[i];
            }
            if (!IsVstreetPoint(point))
                return DataSource[index];
            return point;
        }

        private double GetYOffset(Coordinates point)
        {
            int index = GetDateSourceIndex(point.X);
            Coordinates p1 = DataSource[index];
            if (p1.X == point.X)
                return p1.Y - point.Y;
            Coordinates p2;
            if (p1.X < point.X)
                p2 = DataSource[index + 1];
            else
                p2 = DataSource[index - 1];
            return p1.Y + (point.X - p1.X) * (p2.Y - p1.Y) / (p2.X - p1.X) - point.Y;
        }
        /// <summary>
        /// 获取分割线<paramref name="line"/>与线<paramref name="nextLine"/>之间的面积
        /// </summary>
        /// <param name="nextLine">默认自身的<see cref="SplitLine.NextLine"/></param>
        public double GetArea(SplitLine line, EditLineBase? nextLine = null)
        {
            nextLine ??= line.NextLine;
            var dataEnd = GetDateSourceIndex(line.Start.X);
            var dataStart = GetDateSourceIndex(nextLine.Start.X);

            return GetArea(line.BaseLine, dataStart, dataEnd);
        }

        /// <summary>
        /// 获取指定范围内的面积
        /// </summary>
        public double GetArea(BaseLine baseLine, int dataStart, int dataEnd)
        {
            double area = 0;
            for (var i = dataStart; i < dataEnd; ++i)
            {
                area += DataSource[i].Y + DataSource[i + 1].Y;
            }

            var res = area * Unit / 2;
            var y1 = 0 - baseLine.GetY(DataSource[dataStart].X);
            var y2 = 0 - baseLine.GetY(DataSource[dataEnd].X);
            res += (y1 + y2) * (DataSource[dataEnd].X - DataSource[dataStart].X) / 2;
            return res;
        }

        private Coordinates[] GetT(Coordinates[] data, int start, int end)
        {
            var t = new Coordinates[end - start + 1];
            for (var i = 0; i < t.Length; ++i)
            {
                t[i] = new Coordinates(data[start + i].X, (data[start + i + 1].Y - data[start + i].Y) / Unit);
            }

            return t;
        }

        private static void GetPoints(Coordinates[] coordinates, int start, int end, out int[] minDots,
            out int[] maxDots)
        {
            var inter = 5;
            var maxDotList = new List<int>();
            var minDotList = new List<int>();
            for (var i = start; i < end; i++)
            {
                var max = i;
                for (var j = i; j < end; j++)
                {
                    if (coordinates[j].Y > coordinates[max].Y)
                        max = j;
                    else if (j - max > inter)
                        break;
                }

                maxDotList.Add(max);
                var min = max;
                for (var j = max + 1; j < end; j++)
                {
                    if (coordinates[j].Y < coordinates[min].Y)
                        min = j;
                    else if (j - min > inter)
                        break;
                }

                minDotList.Add(min);
                i = min;
            }

            Debug.Assert(minDotList.Count == maxDotList.Count);
            minDots = [.. minDotList];
            maxDots = [.. maxDotList];
        }
        /// <summary>
        /// 获取坐标点一定范围内的分割线或基线
        /// </summary>
        /// <param name="dataPoint">鼠标坐标点</param>
        /// <param name="region">是否按区域查询（"设置DP"时为True）</param>
        /// <returns></returns>
        public DraggedLineInfo? GetDraggedLine(Coordinates dataPoint, bool region = false)
        {
            if (CurrentBaseLine != null)
            {
                CurrentBaseLine.IsSelected = false;
                CurrentBaseLine = null;
            }
            foreach (var l in SplitLines)
                l.IsSelected = false;
            foreach (var baseLine in BaseLines)
            {
                if (baseLine.Start.Distance(dataPoint) < Sensitivity.Length)
                {
                    DraggedLine = GetFocusLineInfo(baseLine, true);
                    CurrentBaseLine = baseLine;
                    baseLine.IsSelected = true;
                    return DraggedLine;
                }
                else if (baseLine.End.Distance(dataPoint) < Sensitivity.Length)
                {
                    DraggedLine = GetFocusLineInfo(baseLine, false);
                    CurrentBaseLine = baseLine;
                    baseLine.IsSelected = true;
                    return DraggedLine;
                }
            }

            SplitLine? line = null;
            if (!region)
            {
                var nearest = SplitLines.MinBy(x => Math.Abs(x.Start.X - dataPoint.X));
                if (nearest is not null)
                {
                    if (Math.Abs(nearest.Start.X - dataPoint.X) < Sensitivity.X
                        && Math.Min(nearest.Start.Y, nearest.End.Y) < dataPoint.Y
                        && Math.Max(nearest.Start.Y, nearest.End.Y) > dataPoint.Y)
                    {
                        line = nearest;
                    }
                }
            }
            else
            {
                line = SplitLines.FirstOrDefault(v => v.Start.X > dataPoint.X);
            }
            if (line is null)
            {
                DraggedLine = null;
            }
            else
            {
                DraggedLine = GetFocusLineInfo(line, false);
                CurrentBaseLine = line.BaseLine;
                line.IsSelected = true;
            }

            return DraggedLine;
        }


        /// <summary>
        /// 获取对应x附近的坐标点，若y有值，则也要求坐标点在y附近
        /// </summary>
        /// <returns>对应的坐标点</returns>
        public Coordinates? GetChartPoint(double x, double? y = null)
        {
            if (DataSource.Length == 0)
                return null;
            if (x < DataSource[0].X || x > DataSource[^1].X)
                return null;

            var index = (int)((x - DataSource[0].X) / Unit);
            if (Math.Abs(DataSource[index].X - x) > Math.Abs(DataSource[index + 1].X - x))
                index += 1;
            if (y.HasValue)
            {
                if (Math.Abs(y.Value - DataSource[index].Y) > Sensitivity.Y)
                {
                    return new Coordinates(x, y.Value);
                }
            }

            return DataSource[index];
        }

        /// <summary>
        /// 拖动的线改动且是分割线时，监听其移动事件
        /// </summary>
        partial void OnDraggedLineChanged(DraggedLineInfo? oldValue, DraggedLineInfo? newValue)
        {
            Debug.WriteLine("draggedline changed");
            if (oldValue is { DraggedLine: SplitLine oldLine })
                oldLine.SplitLineMoving -= OnLineMoving;

            //移除对上条线的移动监测
            if (oldValue is { IsBaseLine: false })
                oldValue.Value.DraggedLine.SplitLineMoving -= OnLineMoving;

            if (!newValue.HasValue || newValue.Value.IsBaseLine)
                return;
            //添加对当前线的移动监测
            newValue.Value.DraggedLine.SplitLineMoving += OnLineMoving;

            draggedSplitLineIndex = SplitLines.IndexOf((SplitLine)newValue.Value.DraggedLine);
            DraggedLineChanged?.Invoke((SplitLine)newValue.Value.DraggedLine);
        }


        public static DraggedLineInfo GetFocusLineInfo(EditLineBase line, bool? start = null)
        {
            return new DraggedLineInfo(line, start.GetValueOrDefault());
        }

        /// <summary>
        /// 分割线移动时，检测是否跨过相邻分割线
        /// </summary>
        private void OnLineMoving(SplitLine splitLine, CoordinateLine oldValue, CoordinateLine newValue)
        {
            if (oldValue.Start.X < newValue.Start.X)
            {
                var after = SplitLines.ElementAtOrDefault(draggedSplitLineIndex + 1);
                if (after == null || after.BaseLine != splitLine.BaseLine || !(oldValue.Start.X >= after.Start.X)) return;
                SplitLines[draggedSplitLineIndex] = after;
                SplitLines[draggedSplitLineIndex + 1] = splitLine;
                after.NextLine = splitLine.NextLine;
                splitLine.NextLine = after;
                if (draggedSplitLineIndex + 2 < SplitLines.Count)
                    SplitLines[draggedSplitLineIndex + 2].NextLine = splitLine;
                splitLine.Index = ++draggedSplitLineIndex + 1;
                after.Index = splitLine.Index - 1;
                UpdateLineArea(draggedSplitLineIndex);
            }
            else
            {
                var before = SplitLines.ElementAtOrDefault(draggedSplitLineIndex - 1);
                //跨过分割线后，交换其与跨过的线在集合中的位置
                if (before == null || before.BaseLine != splitLine.BaseLine || !(oldValue.Start.X <= before.Start.X)) return;
                SplitLines[draggedSplitLineIndex] = before;
                SplitLines[draggedSplitLineIndex - 1] = splitLine;
                splitLine.NextLine = before.NextLine;
                before.NextLine = splitLine;
                if (draggedSplitLineIndex + 1 < SplitLines.Count)
                    SplitLines[draggedSplitLineIndex + 1].NextLine = before;
                splitLine.Index = --draggedSplitLineIndex + 1;
                before.Index = splitLine.Index + 1;
                UpdateLineArea(draggedSplitLineIndex + 1);
            }
        }

        /// <summary>
        /// 发生跨线移动时，更新受影响峰的面积
        /// </summary>
        /// <param name="index">中间线的索引</param>
        private void UpdateLineArea(int index)
        {
            var area1 = SplitLines[index - 1].Area;
            var area2 = SplitLines[index].Area;
            var area3 = SplitLines[index + 1].Area;
            var end = SplitLines.Count == index + 1;


            SplitLines[index - 1].Area = area2 + area1;
            SplitLines[index - 1].AreaRatio = SplitLines[index - 1].Area / SumArea;

            SplitLines[index].Area = -area1;
            SplitLines[index].AreaRatio = SplitLines[index].Area / SumArea;
            if (end)
                return;
            SplitLines[index + 1].Area = area3 + area1;
            SplitLines[index + 1].AreaRatio = SplitLines[index + 1].Area / SumArea;
        }

        private void OnLineMoved(SplitLine line, CoordinateLine oldValue, CoordinateLine newValue)
        {
            //本次移动的范围
            var startIndex = GetDateSourceIndex(oldValue.Start.X);
            var endIndex = GetDateSourceIndex(newValue.Start.X);
            //向右移动
            var right = true;
            //向左移动
            if (startIndex > endIndex)
            {
                (endIndex, startIndex) = (startIndex, endIndex);
                right = false;
            }

            //面积变化量
            var change = GetArea(line.BaseLine, startIndex, endIndex) * (right ? 1 : -1);

            //更新面积
            line.Area += change;
            line.AreaRatio = line.Area / SumArea;



            //更新RT值
            if (right)
            {
                //向右移动时，需先获取当前移动范围内的最大值，若大于RT值则更新RT值
                var max = DataSource[startIndex..endIndex].MaxBy(v => v.Y);
                if (max.Y > DataSource[line.RTIndex].Y)
                {
                    line.RTIndex = GetDateSourceIndex(max.X);
                    line.RT = DataSource[line.RTIndex].X;
                }
            }
            else
            {
                if (line.RTIndex > startIndex)
                {
                    line.RTIndex = startIndex;
                    line.RT = DataSource[line.RTIndex].X;
                }
            }

            //当前是最后一个分割线时更新总面积及分割线的面积比例
            if (line == line.BaseLine.SplitLines.Last())
            {
                UpdateArea();
                return;
            }


            //处理移动对右边一条线的影响
            var rightLine = SplitLines.ElementAtOrDefault(line.Index);
            if (rightLine is null || rightLine.BaseLine != line.BaseLine)
                return;

            //此时面积为减去变化量
            rightLine.Area -= change;
            rightLine.AreaRatio = rightLine.Area / SumArea;

            //RT值处理与本身相反
            if (!right)
            {
                var max = DataSource[startIndex..endIndex].MaxBy(v => v.Y);
                if (max.Y > DataSource[rightLine.RTIndex].Y)
                {
                    rightLine.RTIndex = GetDateSourceIndex(max.X);
                    rightLine.RT = DataSource[rightLine.RTIndex].X;
                }
            }
            else
            {
                if (rightLine.RTIndex < endIndex)
                {
                    rightLine.RTIndex = endIndex;
                    rightLine.RT = DataSource[rightLine.RTIndex].X;
                }
            }

        }


        private void OnNextLineChanged(SplitLine sender, EditLineBase? oldValue, EditLineBase newValue)
        {
            if (oldValue is not null)
                return; //第一次设置时
            //初始化面积和RT
            sender.Area = GetArea(sender, newValue);
            var startIndex = GetDateSourceIndex(newValue.Start.X);
            var endIndex = GetDateSourceIndex(sender.Start.X);
            Debug.Assert(startIndex < endIndex && startIndex > 0 && endIndex < DataSource.Length);
            sender.RTIndex = GetDateSourceIndex(DataSource[startIndex..endIndex].MaxBy(v => v.Y).X);
            sender.RT = DataSource[sender.RTIndex].X;
        }
    }
}

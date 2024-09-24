using ChartEditLibrary.Model;
using ChartEditLibrary.Entitys;
using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using ScottPlot.Colormaps;
using LanguageExt.Common;
using LanguageExt;

namespace ChartEditLibrary.ViewModel
{
    public partial class DraggableChartVM : ObservableObject
    {
        public event Action<SplitLine>? DraggedLineChanged;

        public double Unit { get; }

        /// <summary>
        /// 通知前端数据发生变化
        /// </summary>
        public event Action? OnBaseLineChanged;

        public string FileName { get; init; }

        public string FilePath { get; init; }

        [ObservableProperty]
        private DraggedLineInfo? draggedLine;

        private int draggedSplitLineIndex;

        public Coordinates YMax { get; init; }

        public Coordinates YMinHalf { get; init; }

        public BaseLine BaseLine { get; init; }

        public ObservableCollection<SplitLine> SplitLines { get; init; } = [];

        public Coordinates[] DataSource { get; init; }

        private ExportType exportType;

        /// <summary>
        /// 鼠标操作的敏感度
        /// </summary>
        public Vector2d Sensitivity { get; set; }

        public double SumArea { get; set; }

        private readonly int heighestIndex;

        private bool inited = false;

        private DraggableChartVM(string filePath, Coordinates[] dataSource, ExportType exportType)
        {
            this.FilePath = filePath;
            this.FileName = Path.GetFileNameWithoutExtension(filePath);
            this.exportType = exportType;
            this.DataSource = dataSource;
            Unit = dataSource[1].X - dataSource[0].X;
            YMax = dataSource[0];
            YMinHalf = dataSource[0];
            int half = dataSource.Length / 2;
            for (int i = 0; i < dataSource.Length; ++i)
            {
                var data = dataSource[i];
                if (data.Y > YMax.Y && data.X < 42.5)
                {
                    YMax = data;
                    heighestIndex = i;
                }
                if (i < half && YMinHalf.Y > data.Y)
                    YMinHalf = data;
            }
            BaseLine = new BaseLine(new Coordinates(YMinHalf.X, YMinHalf.Y), new Coordinates(dataSource[^1].X, YMinHalf.Y));
        }

        /// <summary>
        /// 拖动的线改动且是分割线时，监听其移动事件
        /// </summary>
        partial void OnDraggedLineChanged(DraggedLineInfo? oldValue, DraggedLineInfo? newValue)
        {
            //移除对上条线的移动监测
            if (oldValue.HasValue)
            {
                if (!oldValue.Value.IsBaseLine)
                    oldValue.Value.DraggedLine.SplitLineMoving -= OnLineMoving;
            }
            //添加对当前线的移动监测
            if (newValue.HasValue && !newValue.Value.IsBaseLine)
            {
                if (!newValue.Value.IsBaseLine)
                {
                    draggedSplitLineIndex = SplitLines.IndexOf((SplitLine)newValue.Value.DraggedLine);
                    newValue.Value.DraggedLine.SplitLineMoving += OnLineMoving;
                    DraggedLineChanged?.Invoke((SplitLine)newValue.Value.DraggedLine);
                }
            }
        }

        /// <summary>
        /// 基线变动时，更新所有分割线的面积
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public void UpdateBaseLine(CoordinateLine oldValue, CoordinateLine newValue)
        {
            foreach (var i in SplitLines)
            {
                double newY1 = newValue.Y(i.Start.X);
                double newY2 = newValue.Y(i.NextLine.Start.X);
                double width = i.Start.X - i.NextLine.Start.X;
                double y1 = i.Start.Y - newY1;
                double y2 = i.NextLine.Start.Y - newY2;
                i.Area += (y1 + y2) * width / 2;
                i.Start = new Coordinates(i.Start.X, newY1);
            }
            SumArea = SplitLines.Sum(x => x.Area);
            foreach (var i in SplitLines)
            {
                i.AreaRatio = i.Area / SumArea;
            }
            OnBaseLineChanged?.Invoke();
        }

        /// <summary>
        /// 分割线移动时，检测是否跨过相邻分割线
        /// </summary>
        private void OnLineMoving(SplitLine line, CoordinateLine oldValue, CoordinateLine newValue)
        {
            if (oldValue.Start.X < newValue.Start.X)
            {
                var after = SplitLines.ElementAtOrDefault(draggedSplitLineIndex + 1);
                if (after != null && line.Start.X >= after.Start.X)
                {
                    SplitLines[draggedSplitLineIndex] = after;
                    SplitLines[draggedSplitLineIndex + 1] = line;
                    after.NextLine = line.NextLine;
                    line.NextLine = after;
                    if (draggedSplitLineIndex + 2 < SplitLines.Count)
                        SplitLines[draggedSplitLineIndex + 2].NextLine = line;
                    line.Index = ++draggedSplitLineIndex + 1;
                    after.Index = line.Index - 1;
                    UpdateLineArea(draggedSplitLineIndex, true);
                }
            }
            else
            {
                var before = SplitLines.ElementAtOrDefault(draggedSplitLineIndex - 1);
                //跨过分割线后，交换其与跨过的线在集合中的位置
                if (before != null && line.Start.X <= before.Start.X)
                {
                    SplitLines[draggedSplitLineIndex] = before;
                    SplitLines[draggedSplitLineIndex - 1] = line;
                    line.NextLine = before.NextLine;
                    before.NextLine = line;
                    if (draggedSplitLineIndex + 1 < SplitLines.Count)
                        SplitLines[draggedSplitLineIndex + 1].NextLine = before;
                    line.Index = --draggedSplitLineIndex + 1;
                    before.Index = line.Index + 1;
                    UpdateLineArea(draggedSplitLineIndex + 1, false);
                }
            }

            //OnDataChanged?.Invoke();


        }

        /// <summary>
        /// 发生跨线移动时，更新受影响峰的面积
        /// </summary>
        /// <param name="index"></param>
        /// <param name="after">是否是向右移动时发生</param>
        void UpdateLineArea(int index, bool after)
        {
            //SplitLines[index].Area = GetArea(SplitLines[index]);
            //SplitLines[index].AreaRatio = SplitLines[index].Area / SumArea;
            //if(index > 0)
            //{
            //    SplitLines[index - 1].Area = GetArea(SplitLines[index - 1]);
            //    SplitLines[index - 1].AreaRatio = SplitLines[index - 1].Area / SumArea;
            //}
            //if (index + 1 < SplitLines.Count)
            //{
            //    SplitLines[index + 1].Area = GetArea(SplitLines[index + 1]);
            //    SplitLines[index + 1].AreaRatio = SplitLines[index + 1].Area / SumArea;
            //}
            //return;
            double area1 = SplitLines[index - 1].Area;
            double area2 = SplitLines[index].Area;
            bool end = SplitLines.Count == index + 1;
            double area3 = end ? 0 : SplitLines[index + 1].Area;
            if (after)
            {
                SplitLines[index - 1].Area = area2 + area1;
                SplitLines[index - 1].AreaRatio = SplitLines[index - 1].Area / SumArea;
                SplitLines[index].Area = -area1;
                SplitLines[index].AreaRatio = SplitLines[index].Area / SumArea;
                if (!end)
                {
                    SplitLines[index + 1].Area = area3 + area1;
                    SplitLines[index + 1].AreaRatio = SplitLines[index + 1].Area / SumArea;
                }
            }
            else
            {
                SplitLines[index - 1].Area = area2 + area1;
                SplitLines[index - 1].AreaRatio = SplitLines[index - 1].Area / SumArea;
                SplitLines[index].Area = -area1;
                SplitLines[index].AreaRatio = SplitLines[index].Area / SumArea;
                if (!end)
                {
                    SplitLines[index + 1].Area = area3 + area1;
                    SplitLines[index + 1].AreaRatio = SplitLines[index + 1].Area / SumArea;
                }
            }

        }

        /// <summary>
        /// 获取坐标点一定范围内的分割线或基线
        /// </summary>
        /// <param name="dataPoint"></param>
        /// <returns></returns>
        public DraggedLineInfo? GetDraggedLine(Coordinates dataPoint, bool region = false)
        {
            BaseLine.IsSelected = false;
            foreach (var line in SplitLines)
            {
                line.IsSelected = false;
            }
            if (BaseLine.Start.Distance(dataPoint) < Sensitivity.Y)
            {
                DraggedLine = GetFocusLineInfo(BaseLine, true);
            }
            else if (BaseLine.End.Distance(dataPoint) < Sensitivity.Y)
            {
                DraggedLine = GetFocusLineInfo(BaseLine, false);
            }
            else
            {
                SplitLine? line = null;
                if (!region)
                {
                    SplitLine nearest = SplitLines.MinBy(x => Math.Abs(x.Start.X - dataPoint.X))!;
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
                }
            }
            if (DraggedLine != null)
            {
                DraggedLine.Value.DraggedLine.IsSelected = true;
            }
            return DraggedLine;
        }

        public static DraggedLineInfo GetFocusLineInfo(EditLineBase line, bool? start = null)
        {
            return new DraggedLineInfo(line, start.GetValueOrDefault());
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

            int index = (int)((x - DataSource[0].X) / Unit);
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
        /// 获取X在数据源中的索引
        /// </summary>
        public int GetDateSourceIndex(double x)
        {
            int index = (int)((x - DataSource[0].X) / Unit);
            if (Math.Abs(DataSource[index].X - x) > Math.Abs(DataSource[index + 1].X - x))
                index += 1;
            return index;
        }

        /// <summary>
        /// 根据当前的基线创建该数据点的分割线
        /// </summary>
        public CoordinateLine CreateSplitLine(Coordinates point)
        {
            return BaseLine.CreateSplitLine(point);
        }

        /// <summary>
        /// 根据当前的基线创建该X在基线上的点
        /// </summary>
        public Coordinates CreateSplitLinePoint(double x)
        {
            return BaseLine.CreateSplitLineStartPoint(x);
        }

        /// <summary>
        /// 添加分割线
        /// </summary>
        /// <param name="line"></param>
        public void AddSplitLine(SplitLine line)
        {
            line.SplitLineMoved += OnLineMoved;
            line.NextLineChanged += OnNextLineChanged;
            int index = SplitLines.BinaryInsert(line);
            line.Index = index + 1;
            for (int i = index + 1; i < SplitLines.Count; ++i)
            {
                ++SplitLines[i].Index;
            }
            if (index == 0)
            {
                line.NextLine = BaseLine;
                if (SumArea == 0)
                    SumArea = line.Area;
                line.AreaRatio = line.Area / SumArea;
                if (SplitLines.Count > 1)
                {
                    var parentLine = SplitLines[1];
                    parentLine.NextLine = line;
                    parentLine.Area = GetArea(SplitLines[1]);
                    parentLine.AreaRatio = SplitLines[1].Area / SumArea;
                    if (DataSource[parentLine.RTIndex].X < line.Start.X)
                    {
                        parentLine.RTIndex = GetDateSourceIndex(line.Start.X);
                        parentLine.RT = DataSource[parentLine.RTIndex].X;
                    }
                }
            }
            else if (index == SplitLines.Count - 1)
            {
                line.NextLine = SplitLines[index - 1];
                SumArea += line.Area;
                foreach (var i in SplitLines)
                {
                    i.AreaRatio = i.Area / SumArea;
                }
            }
            else
            {
                line.NextLine = SplitLines[index - 1];
                line.AreaRatio = line.Area / SumArea;
                var parentLine = SplitLines[index + 1];
                parentLine.NextLine = line;
                parentLine.Area = GetArea(SplitLines[index + 1]);
                parentLine.AreaRatio = SplitLines[index + 1].Area / SumArea;
                if (DataSource[parentLine.RTIndex].X < line.Start.X)
                {
                    parentLine.RTIndex = GetDateSourceIndex(line.Start.X);
                    parentLine.RT = DataSource[parentLine.RTIndex].X;
                }
            }

        }

        /// <summary>
        /// 移除分割线
        /// </summary>
        /// <param name="line"></param>
        public void RemoveSplitLine(SplitLine line)
        {
            line.SplitLineMoved -= OnLineMoved;
            line.NextLineChanged -= OnNextLineChanged;
            int index = SplitLines.IndexOf(line);
            for (int i = index + 1; i < SplitLines.Count; ++i)
            {
                --SplitLines[i].Index;
            }
            if (index == 0)
            {
                if (SplitLines.Count > 1)
                {
                    var changeLine = SplitLines[1];
                    changeLine.NextLine = BaseLine;
                    changeLine.Area = GetArea(SplitLines[1]);
                    changeLine.AreaRatio = changeLine.Area / SumArea;
                    if (DataSource[changeLine.RTIndex].Y < DataSource[line.RTIndex].Y)
                    {
                        changeLine.RTIndex = line.RTIndex;
                        changeLine.RT = DataSource[line.RTIndex].X;
                    }
                }
                else
                {
                    SumArea = 0;
                }
            }
            else if (index == SplitLines.Count - 1)
            {
                SumArea -= line.Area;
                foreach (var i in SplitLines)
                {
                    i.AreaRatio = i.Area / SumArea;
                }
            }
            else
            {
                var changeLine = SplitLines[index + 1];
                changeLine.NextLine = line.NextLine;
                changeLine.Area += line.Area;
                changeLine.AreaRatio = changeLine.Area / SumArea;
                if (DataSource[changeLine.RTIndex].Y < DataSource[line.RTIndex].Y)
                {
                    changeLine.RTIndex = line.RTIndex;
                    changeLine.RT = DataSource[changeLine.RTIndex].X;
                }
            }
            SplitLines.Remove(line);
            DraggedLine = null;
        }

        /// <summary>
        /// 获取分割线<paramref name="line"/>与线<paramref name="nextLine"/>之间的面积
        /// </summary>
        /// <param name="nextLine">默认自身的<see cref="SplitLine.NextLine"/></param>
        public double GetArea(SplitLine line, EditLineBase? nextLine = null)
        {
            nextLine ??= line.NextLine;
            int dataEnd = GetDateSourceIndex(line.Start.X);
            int dataStart = GetDateSourceIndex(nextLine.Start.X);

            return GetArea(dataStart, dataEnd);
        }

        /// <summary>
        /// 获取指定范围内的面积
        /// </summary>
        public double GetArea(int dataStart, int dataEnd)
        {
            double area = 0;
            for (int i = dataStart; i < dataEnd; ++i)
            {
                area += DataSource[i].Y + DataSource[i + 1].Y;
            }
            var res = area * Unit / 2;
            double y1 = 0 - BaseLine.GetY(DataSource[dataStart].X);
            double y2 = 0 - BaseLine.GetY(DataSource[dataEnd].X);
            res += (y1 + y2) * (DataSource[dataEnd].X - DataSource[dataStart].X) / 2;
            return res;
        }

        public void InitSplitLine(object? tag)
        {
            if (inited)
                return;
            inited = true;
            switch (exportType)
            {
                case ExportType.Enoxaparin:
                    InitSplitLine_YN(tag);
                    break;
                case ExportType.Other:
                    InitSplitLine_Other();
                    break;
                default:
                    break;
            }
        }

        private void InitSplitLine_Other()
        {
            double perVaule = YMax.Y / 100;
            //double m = dataSource.Take(dataSource.Length / 2).Min(v => v.Y);
            //for (int i = 0; i < dataSource.Length; i++)
            //    dataSource[i].Y -= m;
            int end = DataSource.Length / 3 * 2;
            while (end < DataSource.Length - 1 && DataSource[end].Y > 0)
                end++;
            Config config = Config.GetConfig(exportType);
            List<int> maxDots = [];
            List<int> minDots = [];
            GetPoints(DataSource, 0, end, out int[] _minDots, out int[] _maxDots);
            for (int i = 0; i < _minDots.Length; i++)
            {
                int max = _maxDots[i];
                int min = _minDots[i];
                if (DataSource[max].X <= config.XMin)
                    continue;
                if (DataSource[max].X >= config.XMax)
                    break;
                if (minDots.Count == 0 && DataSource[max].Y < config.FirstYMin * perVaule)
                    continue;
                if (DataSource[max].Y < config.YMin * perVaule)
                    continue;
                if (DataSource[max].Y - DataSource[min].Y < config.MinHeight * perVaule)
                    continue;
                if (minDots.Count > 0 && DataSource[max].Y - DataSource[minDots[^1]].Y < config.MinHeight * perVaule)
                    continue;

                maxDots.Add(max);
                minDots.Add(min);

                //if (maxDots.Count > 1 && DataSource[max].X - DataSource[maxDots[^2]].X < 0.3) //若两个顶点峰太近,移除后一个
                //    maxDots.Remove(max);

            }
            foreach (var i in minDots)
            {
                AddSplitLine(DataSource[i]);
            }
        }

        private void InitSplitLine_YN(object? tag)
        {
            double perVaule = YMax.Y / 100;
            //double m = dataSource.Take(dataSource.Length / 2).Min(v => v.Y);
            //for (int i = 0; i < dataSource.Length; i++)
            //    dataSource[i].Y -= m;
            int end = DataSource.Length / 3 * 2;
            while (end < DataSource.Length - 1 && DataSource[end].Y > 0)
                end++;
            Config config = Config.GetConfig(exportType);
            List<int> maxDots = new List<int>();
            List<int> minDots = new List<int>();
            GetPoints(DataSource, 0, end, out int[] _minDots, out int[] _maxDots);

            for (int i = 0; i < _minDots.Length; i++)
            {
                int max = _maxDots[i];
                int min = _minDots[i];

                if (DataSource[max].X <= config.XMin)
                    continue;
                if (DataSource[max].X >= config.XMax)
                    break;
                if (minDots.Count == 0 && DataSource[max].Y < config.FirstYMin * perVaule)
                    continue;
                if (DataSource[max].Y < config.YMin * perVaule)
                    continue;
                if (DataSource[max].Y - DataSource[min].Y < config.MinHeight * perVaule)
                    continue;
                if (minDots.Count > 0 && DataSource[max].Y - DataSource[minDots[^1]].Y < config.MinHeight * perVaule)
                    continue;

                maxDots.Add(max);
                minDots.Add(min);

                //if (maxDots.Count > 1 && DataSource[max].X - DataSource[maxDots[^2]].X < 0.3) //若两个顶点峰太近,移除后一个
                //    maxDots.Remove(max);
            }
            int dp6Index = maxDots.IndexOf(heighestIndex);
            Debug.Assert(dp6Index != -1);
            if (4.Equals(tag))
            {
                if (DataSource[maxDots[dp6Index]].Y - DataSource[minDots[dp6Index - 1]].Y > 10)
                {
                    dp6Index -= 2;
                }
                else
                {
                    dp6Index -= 1;
                }
            }
            //dp6
            int startMin = dp6Index - 1;
            int endMin = dp6Index;
            if (DataSource[maxDots[dp6Index]].Y - DataSource[minDots[dp6Index - 1]].Y < 10) //若左边还有一个顶点峰
            {
                --startMin;
            }
            else if (DataSource[maxDots[dp6Index]].Y - DataSource[minDots[dp6Index]].Y < 10) //若右边还有一个顶点峰
            {
                ++endMin;
            }
            if (DataSource[maxDots[endMin]].X - DataSource[minDots[endMin]].X > -0.7)
            {
                ++endMin;
            }
            int dp6Start = minDots[endMin];
            int dp6End = minDots[startMin];
            int peakCount = endMin - startMin;
            if (peakCount != 3)
            {
                int startT = minDots[startMin];
                int endT = minDots[endMin];
                var t = GetT(DataSource, startT, endT);
                GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                for (int i = 0; i < _minDots.Length; ++i)
                {
                    int? add = null;
                    if (t[_maxDots[i]].Y < 2)
                    {
                        if ((endT - startT - _maxDots[i]) * Unit > 0.3)
                        {
                            add = startT + _maxDots[i];
                        }
                    }
                    else if (t[_minDots[i]].Y > -2.5)
                    {
                        add = startT + _minDots[i];
                    }

                    if (add.HasValue)
                    {
                        if (minDots.Contains(add.Value))
                            continue;
                        minDots.Add(add.Value);
                        ++endMin;
                        if (add.Value > dp6Start)
                        {
                            dp6Start = add.Value;
                        }
                    }
                }
            }
            minDots.Sort();

            //dp4
            startMin = endMin;
            endMin = startMin + 1;
            if (DataSource[maxDots[endMin]].Y < DataSource[minDots[endMin]].Y * 3.5)
            {
                ++endMin;
            }
            else if (DataSource[maxDots[endMin]].X - DataSource[maxDots[endMin + 1]].X > -0.7)
                ++endMin;
            int dp4Start = minDots[endMin];
            peakCount = endMin - startMin;
            if (peakCount != 2)
            {
                int startT = minDots[startMin];
                int endT = minDots[endMin];
                var t = GetT(DataSource, startT, endT);
                GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                for (int i = 0; i < _minDots.Length; ++i)
                {
                    if (t[_maxDots[i]].Y < 0 || t[_minDots[i]].Y > 0)
                    {
                        int add = startT + _maxDots[i];
                        if (minDots.Contains(add))
                            continue;
                        minDots.Add(add);
                        ++endMin;
                        if (add > dp4Start)
                        {
                            dp4Start = add;
                        }
                    }
                }
            }
            minDots.Sort();

            //dp3
            startMin = endMin;
            endMin = startMin + 1;
            if (DataSource[minDots[endMin]].X - DataSource[minDots[startMin]].X < 1)
            {
                ++endMin;
            }
            int dp3Start = minDots[endMin];
            peakCount = endMin - startMin;
            if (peakCount != 2)
            {
                int startT = minDots[startMin];
                int endT = minDots[endMin];
                var t = GetT(DataSource, startT, endT);
                GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                for (int i = 0; i < _minDots.Length; ++i)
                {
                    if (t[_maxDots[i]].Y < 0 || t[_minDots[i]].Y > 0)
                    {
                        int add = startT + _maxDots[i];
                        if (minDots.Contains(add))
                            continue;
                        minDots.Add(add);
                        ++endMin;
                        if (add > dp3Start)
                        {
                            dp3Start = add;
                        }
                        break;
                    }
                }
            }
            minDots.Sort();

            //dp2
            startMin = endMin;
            endMin = minDots.Count - 1;
            int dp2Start = minDots[endMin];
            peakCount = endMin - startMin;
            if (peakCount > 2)
            {
                minDots.RemoveAt(minDots.Count - 1);
                maxDots.RemoveAt(maxDots.Count - 1);
                --endMin;
                dp2Start = minDots[endMin];
            }

            minDots.Sort();
            int dp = 2;
            int sort = 1;
            foreach (var i in minDots.Distinct().Reverse())
            {
                if (i <= dp6End)
                {
                    dp += 2;
                    sort = 1;
                }
                else if (i <= dp6Start)
                {
                    if (dp != 6)
                    {
                        dp = 6;
                        sort = 1;
                    }
                }
                else if (i <= dp4Start)
                {
                    if (dp != 4)
                    {
                        dp = 4;
                        sort = 1;
                    }
                }
                else if (i <= dp3Start)
                {
                    if (dp != 3)
                    {
                        dp = 3;
                        sort = 1;
                    }
                }
                AddSplitLine(DataSource[i]).DP = dp + "-" + sort;
                ++sort;
            }

            foreach (var i in SplitLines.GroupBy(v => v.DP![..^2]).ToArray())
            {
                if (i.Count() == 1)
                {
                    i.First().DP = i.Key;
                }
            }
        }

        private Coordinates[] GetT(Coordinates[] data, int start, int end)
        {
            Coordinates[] t = new Coordinates[end - start + 1];
            for (int i = 0; i < t.Length; ++i)
            {
                t[i] = new Coordinates(data[start + i].X, (data[start + i + 1].Y - data[start + i].Y) / Unit);
            }
            return t;
        }

        private static void GetPoints(Coordinates[] coordinates, int start, int end, out int[] minDots, out int[] maxDots)
        {
            int inter = 5;
            List<int> maxDotList = new List<int>();
            List<int> minDotList = new List<int>();
            for (int i = start; i < end; i++)
            {

                int max = i;
                for (int j = i; j < end; j++)
                {
                    if (coordinates[j].Y > coordinates[max].Y)
                        max = j;
                    else if (j - max > inter)
                        break;
                }
                maxDotList.Add(max);
                int min = max;
                for (int j = max + 1; j < end; j++)
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

        public SplitLine AddSplitLine(Coordinates point)
        {
            CoordinateLine chartLine = CreateSplitLine(point);
            var line = new SplitLine(chartLine);
            AddSplitLine(line);
            return line;
        }

        public static async Task<DraggableChartVM> CreateAsync(string filePath, ExportType exportType)
        {
            var (dataSource, saveLine) = await ReadCsv(filePath).ConfigureAwait(false);

            var res = new DraggableChartVM(filePath, dataSource, exportType);
            if (saveLine != null)
                res.ApplyResult(saveLine);
            return res;
        }

        private void ApplyResult(string[] lines)
        {
            string[] baseInfo = lines[0].Split(',');
            exportType = Enum.Parse<ExportType>(baseInfo[1]);
            BaseLine.Start = new Coordinates(double.Parse(baseInfo[2]), double.Parse(baseInfo[3]));
            BaseLine.End = new Coordinates(double.Parse(baseInfo[4]), double.Parse(baseInfo[5]));
            for (int i = 2; i < lines.Length; i++)
            {
                string[] data = lines[i].Split(',');
                double x = double.Parse(data[1]);
                Coordinates? point = GetChartPoint(x);
                if (point.HasValue)
                    AddSplitLine(point.Value).DP = data[6][2..].TrimEnd('\r');
            }
            inited = true;
        }

        public static DraggableChartVM Create(CacheContent cache)
        {
            Coordinates[] dataSource = Enumerable.Range(0, cache.X.Length).Select(i => new Coordinates(cache.X[i], cache.Y[i])).ToArray();
            var vm = new DraggableChartVM(cache.FilePath, dataSource, ExportType.None);
            vm.ApplyResult(cache.SaveContent);
            return vm;
        }

        /// <summary>
        /// 获取保存于文件的内容
        /// </summary>
        /// <returns></returns>
        public string GetSaveContent()
        {
            return string.Join(Environment.NewLine, GetSaveRowContent());
        }

        private string[] GetSaveRowContent()
        {
            string baseInfo = $"{FileName},{exportType},{BaseLine.Start.X},{BaseLine.Start.Y},{BaseLine.End.X},{BaseLine.End.Y},";
            string title = "Peak,Start X,End X,Center X,Area,Area Sum %,DP";
            IEnumerable<string> lines = SplitLines.Select(x =>
            $"{x.Index},{x.Start.X:f3},{x.NextLine.Start.X:f3},{x.RT:f3},{x.Area:f2},{x.AreaRatio * 100:f2},DP{x.DP}");
            return lines.Prepend(title).Prepend(baseInfo).ToArray();
        }

        public SaveRow[] GetSaveRow()
        {
            SaveRow baseInfo = new("", $"{FileName},{exportType},{BaseLine.Start.X},{BaseLine.Start.Y},{BaseLine.End.X},{BaseLine.End.Y}");
            SaveRow title = new("", "Peak,Start X,End X,Center X,Area,Area Sum %");
            IEnumerable<SaveRow> lines = SplitLines.Select(x =>
            new SaveRow(x.DP!, $"{x.Index},{x.Start.X:f3},{x.NextLine.Start.X:f3},{x.RT:f3},{x.Area:f2},{x.AreaRatio * 100:f2}"));
            return lines.Prepend(title).Prepend(baseInfo).ToArray();
        }

        public async Task<Result<bool>> SaveToFile()
        {
            try
            {
                static string GetDataLine(string line)
                {
                    int index = 0;
                    for (int i = 0; i < 3; ++i)
                    {
                        index = line.IndexOf(',', index) + 1;
                        if (index == 0)
                            return line;
                    }
                    return line[0..(index - 1)];
                }
                string[] save = GetSaveRowContent();
                string[] datas = File.ReadAllLines(FilePath);
                File.Delete(FilePath);
                using StreamWriter writer = new(File.Create(FilePath));
                for (int i = 0; i < datas.Length; ++i)
                {
                    string line = datas[i];
                    if (i < save.Length)
                    {
                        line = GetDataLine(line) + ",," + save[i];
                    }
                    await writer.WriteLineAsync(line);
                }
                return new Result<bool>(true);
            }
            catch (UnauthorizedAccessException ue)
            {
                return new Result<bool>(new UnauthorizedAccessException($"无法修改只读的源文件:'{FilePath}'", ue));
            }
            catch (IOException e)
            {
                return new Result<bool>(new IOException($"文件'{FilePath}'已被占用，请先关闭文件", e));
            }
        }

        public readonly struct SaveRow(string dp, string line)
        {
            public readonly string dp = dp;
            public readonly string line = line;
        }

        internal void ApplyResult(string saveContent)
        {
            string[] lines = saveContent.Split(Environment.NewLine);
            ApplyResult(lines);
        }

        private void OnLineMoved(EditLineBase mover, CoordinateLine oldValue, CoordinateLine newValue)
        {
            if (mover is not SplitLine line)
                return;
            //本次移动的范围
            int startIndex = GetDateSourceIndex(oldValue.Start.X);
            int endIndex = GetDateSourceIndex(newValue.Start.X);

            //向右移动
            int sign = 1;
            //向左移动
            if (startIndex > endIndex)
            {
                (endIndex, startIndex) = (startIndex, endIndex);
                sign = -1;
            }
            //面积变化量
            double change = GetArea(startIndex, endIndex) * sign;

            //更新面积
            line.Area += change;
            line.AreaRatio = line.Area / SumArea;
            //当前是最后一个分割线时更新总面积及分割线的面积比例
            if (this.Equals(SplitLines[^1]))
            {
                SumArea += change;
                foreach (var temp in SplitLines)
                {
                    temp.AreaRatio = temp.Area / SumArea;
                }
            }

            if (sign == -1)
            {
                //向左移动时，若跨过RT值，则默认RT值为当前移动值，即默认递减
                if (line.RTIndex > endIndex)
                {
                    line.RTIndex = endIndex;
                    line.RT = DataSource[line.RTIndex].X;
                }
            }
            else
            {
                //向右移动时，需先获取当前移动范围内的最大值，若大于RT值则更新RT值
                var max = DataSource[startIndex..endIndex].MaxBy(v => v.Y);
                if (max.Y > DataSource[line.RTIndex].Y)
                {
                    line.RTIndex = GetDateSourceIndex(max.X);
                    line.RT = DataSource[line.RTIndex].X;
                }
            }

            //处理移动对右边一条线的影响
            var rightLine = SplitLines.ElementAtOrDefault(line.Index);
            if (rightLine is not null)
            {
                //此时面积为减去变化量
                rightLine.Area -= change;
                rightLine.AreaRatio = rightLine.Area / SumArea;

                //RT值处理与本身相反
                if (sign == -1)
                {
                    var max = DataSource[startIndex..endIndex].MaxBy(v => v.Y);
                    if (max.Y > DataSource[rightLine.RTIndex].Y)
                    {
                        rightLine.RTIndex = GetDateSourceIndex(max.X);
                        rightLine.RT = DataSource[line.RTIndex].X;
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
        }

        private void OnNextLineChanged(SplitLine sender, EditLineBase? oldValue, EditLineBase newValue)
        {
            if (oldValue is null)//第一次设置时
            {
                //初始化面积和RT
                sender.Area = GetArea(sender, newValue);
                int startIndex = GetDateSourceIndex(newValue.Start.X);
                int endIndex = GetDateSourceIndex(sender.Start.X);
                sender.RTIndex = GetDateSourceIndex(DataSource[startIndex..endIndex].MaxBy(v => v.Y).X);
                sender.RT = DataSource[sender.RTIndex].X;
            }
            //添加对下一条线移动的监听，以处理移动对本线的影响
            //if (oldValue is SplitLine old)
            //    old.SplitLineMoved -= OnLineMoved;
            //if (newValue is SplitLine newLine)
            //    newLine.SplitLineMoved += OnLineMoved;
        }

        private static async Task<(Coordinates[], string[]?)> ReadCsv(string path)
        {
            char[] spe = [',', '\t'];
            string[] data;
            bool hasResult = false;
            string fileName = Path.GetFileNameWithoutExtension(path);
            List<string> saveContent = new List<string>();
            static string? GetSaveLine(string line)
            {
                int index = 0;
                for (int i = 0; i < 4; ++i)
                {
                    index = line.IndexOf(',', index) + 1;
                    if (index == 0)
                        return null;
                }
                return line[index..];
            }
            using (StreamReader sr = new(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                string? first = sr.ReadLine();
                if (first is not null)
                {
                    string? temp = GetSaveLine(first);
                    if (temp is not null)
                    {
                        saveContent.Add(temp);
                        hasResult = true;
                    }
                }
                string? second = sr.ReadLine();
                if (hasResult && second != null)
                    saveContent.Add(GetSaveLine(second)!);
                data = (await sr.ReadToEndAsync().ConfigureAwait(false)).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                if (hasResult)
                {
                    for (int i = 0; i < data.Length; ++i)
                    {
                        string? temp = GetSaveLine(data[i]);
                        if (temp is null)
                            break;
                        saveContent.Add(temp);
                    }
                }
            }
            try
            {
                double[][] temp = data.Select(v => v.Split(spe).Skip(1).Take(2).Select(v1 => double.Parse(v1)).ToArray())
                .Where(v => v[0] >= 20 && v[0] <= 60).ToArray();
                return (temp.Select(v => new Coordinates(v[0], v[1])).ToArray(), hasResult ? saveContent.ToArray() : null);
            }
            catch
            {
                throw new Exception(Path.GetFileNameWithoutExtension(path) + "数据格式错误");
            }

        }
    }

}

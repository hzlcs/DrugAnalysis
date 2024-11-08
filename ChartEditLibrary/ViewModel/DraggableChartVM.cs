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
    public partial class DraggableChartVm : ObservableObject
    {
        public event Action<SplitLine>? DraggedLineChanged;

        /// <summary>
        /// x轴单位间距
        /// </summary>
        public double Unit { get; }

        public string FileName { get; }

        public string FilePath { get; }

        [ObservableProperty]
        private DraggedLineInfo? draggedLine;

        private int draggedSplitLineIndex;

        private readonly Coordinates yMax;

        public ObservableCollection<BaseLine> BaseLines { get; } = [];

        public bool SingleBaseLine { get; }

        public BaseLine? CurrentBaseLine { get; private set; }

        public ObservableCollection<SplitLine> SplitLines { get; } = [];

        public Coordinates[] DataSource { get; }

        private ExportType exportType;

        /// <summary>
        /// 鼠标操作的敏感度
        /// </summary>
        public Vector2d Sensitivity { get; set; }

        private double SumArea { get; set; }

        private readonly int highestIndex;

        /// <summary>
        /// 是否初始化
        /// </summary>
        private bool initialized = false;

        private DraggableChartVm(string filePath, Coordinates[] dataSource, ExportType exportType)
        {
            this.FilePath = filePath;
            this.FileName = Path.GetFileNameWithoutExtension(filePath);
            this.exportType = exportType;
            this.DataSource = dataSource;
            Unit = dataSource[1].X - dataSource[0].X;
            yMax = dataSource[0];

            for (var i = 0; i < dataSource.Length; ++i)
            {
                var data = dataSource[i];
                if (data.Y > yMax.Y && data.X < 42.5)
                {
                    yMax = data;
                    highestIndex = i;
                }
            }
            if (exportType != ExportType.None)
                SingleBaseLine = true;
        }

        private BaseLine GetDefaultBaseLine()
        {
            var yMinHalf = DataSource[0];
            var minRange = DataSource.Length / 3;
            for (var i = 0; i < DataSource.Length; ++i)
            {
                var data = DataSource[i];

                if (i < minRange && yMinHalf.Y > data.Y)
                    yMinHalf = data;
            }

            return new BaseLine(new Coordinates(yMinHalf.X, yMinHalf.Y),
                new Coordinates(DataSource[^1].X, yMinHalf.Y));
        }

        /// <summary>
        /// 拖动的线改动且是分割线时，监听其移动事件
        /// </summary>
        partial void OnDraggedLineChanged(DraggedLineInfo? oldValue, DraggedLineInfo? newValue)
        {
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

        /// <summary>
        /// 基线变动时，更新所有分割线的面积
        /// </summary>
        /// <param name="_"></param>
        /// <param name="newValue"></param>
        public void UpdateBaseLine(BaseLine baseLine)
        {
            var newValue = baseLine.Line;
            IEnumerable<SplitLine> splitLines = SingleBaseLine ? SplitLines : SplitLines.Where(v => v.BaseLine == baseLine).ToArray();
            double sumArea = SumArea - splitLines.Sum(v => v.Area);
            foreach (SplitLine i in splitLines)
            {
                var newY1 = newValue.Y(i.Start.X);
                var newY2 = newValue.Y(i.NextLine.Start.X);
                var width = i.Start.X - i.NextLine.Start.X;
                var y1 = i.Start.Y - newY1;
                var y2 = i.NextLine.Start.Y - newY2;
                i.Area += (y1 + y2) * width / 2;
                i.Start = new Coordinates(i.Start.X, newY1);
            }

            SumArea = sumArea + splitLines.Sum(x => x.Area);
            foreach (var i in SplitLines)
            {
                i.AreaRatio = i.Area / SumArea;
            }
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
                if (baseLine.Start.Distance(dataPoint) < Sensitivity.Y)
                {
                    DraggedLine = GetFocusLineInfo(baseLine, true);
                    CurrentBaseLine = baseLine;
                    baseLine.IsSelected = true;
                    return DraggedLine;
                }
                else if (baseLine.End.Distance(dataPoint) < Sensitivity.Y)
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
        /// 获取X在数据源中的索引
        /// </summary>
        public int GetDateSourceIndex(double x)
        {
            var index = (int)((x - DataSource[0].X) / Unit);
            if (Math.Abs(DataSource[index].X - x) > Math.Abs(DataSource[index + 1].X - x))
                index += 1;
            return index;
        }

        /// <summary>
        /// 移除分割线
        /// </summary>
        /// <param name="line"></param>
        public void RemoveSplitLine(SplitLine line)
        {
            line.SplitLineMoved -= OnLineMoved;
            line.NextLineChanged -= OnNextLineChanged;
            var index = SplitLines.IndexOf(line);
            for (var i = index + 1; i < SplitLines.Count; ++i)
            {
                --SplitLines[i].Index;
            }

            if (index == 0)
            {
                if (SplitLines.Count > 1)
                {
                    var changeLine = SplitLines[1];
                    changeLine.NextLine = line.BaseLine;
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

        public void InitSplitLine(object? tag)
        {
            if (initialized)
                return;
            initialized = true;
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

            if (SplitLines.Count > 0)
            {
                var lastLine = SplitLines.Last();
                var y = lastLine.BaseLine.End.Y;
                var last = GetDateSourceIndex(lastLine.End.X);
                for (var i = last; i < DataSource.Length; ++i)
                {
                    if (DataSource[i].Y < y)
                    {
                        lastLine.BaseLine.End = new Coordinates(DataSource[i].X, DataSource[i].Y);
                        UpdateBaseLine(lastLine.BaseLine);
                        break;
                    }
                }
            }
        }

        private void InitSplitLine_Other()
        {
            CurrentBaseLine = GetDefaultBaseLine();
            BaseLines.Add(CurrentBaseLine);
            var perVaule = yMax.Y / 100;
            //double m = dataSource.Take(dataSource.Length / 2).Min(v => v.Y);
            //for (int i = 0; i < dataSource.Length; i++)
            //    dataSource[i].Y -= m;
            var end = DataSource.Length / 3 * 2;
            while (end < DataSource.Length - 1 && DataSource[end].Y > 0)
                end++;
            var config = Config.GetConfig(exportType);
            List<int> maxDots = [];
            List<int> minDots = [];
            GetPoints(DataSource, 0, end, out var _minDots, out var _maxDots);
            for (var i = 0; i < _minDots.Length; i++)
            {
                var max = _maxDots[i];
                var min = _minDots[i];
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
                if (DataSource[min].X <= CurrentBaseLine.Start.X)
                    continue;

                maxDots.Add(max);
                minDots.Add(min);

                //if (maxDots.Count > 1 && DataSource[max].X - DataSource[maxDots[^2]].X < 0.3) //若两个顶点峰太近,移除后一个
                //    maxDots.Remove(max);
            }

            foreach (var i in minDots)
            {
                AddSplitLine(CurrentBaseLine, DataSource[i]);
            }
        }

        private void InitSplitLine_YN(object? tag)
        {
            CurrentBaseLine = GetDefaultBaseLine();
            BaseLines.Add(CurrentBaseLine);
            var perVaule = yMax.Y / 100;
            //double m = dataSource.Take(dataSource.Length / 2).Min(v => v.Y);
            //for (int i = 0; i < dataSource.Length; i++)
            //    dataSource[i].Y -= m;
            var end = DataSource.Length / 3 * 2;
            while (end < DataSource.Length - 1 && DataSource[end].Y > 0)
                end++;
            var config = Config.GetConfig(exportType);
            var maxDots = new List<int>();
            var minDots = new List<int>();
            GetPoints(DataSource, 0, end, out var _minDots, out var _maxDots);

            for (var i = 0; i < _minDots.Length; i++)
            {
                var max = _maxDots[i];
                var min = _minDots[i];

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

            var dp6Index = maxDots.IndexOf(highestIndex);
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
            var startMin = dp6Index - 1;
            var endMin = dp6Index;
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

            var dp6Start = minDots[endMin];
            var dp6End = minDots[startMin];
            var peakCount = endMin - startMin;
            if (peakCount != 3)
            {
                var startT = minDots[startMin];
                var endT = minDots[endMin];
                var t = GetT(DataSource, startT, endT);
                GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                for (var i = 0; i < _minDots.Length; ++i)
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

            var dp4Start = minDots[endMin];
            peakCount = endMin - startMin;
            if (peakCount != 2)
            {
                var startT = minDots[startMin];
                var endT = minDots[endMin];
                var t = GetT(DataSource, startT, endT);
                GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                for (var i = 0; i < _minDots.Length; ++i)
                {
                    if (t[_maxDots[i]].Y < 0 || t[_minDots[i]].Y > 0)
                    {
                        var add = startT + _maxDots[i];
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

            var dp3Start = minDots[endMin];
            peakCount = endMin - startMin;
            if (peakCount != 2)
            {
                var startT = minDots[startMin];
                var endT = minDots[endMin];
                var t = GetT(DataSource, startT, endT);
                GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                for (var i = 0; i < _minDots.Length; ++i)
                {
                    if (t[_maxDots[i]].Y < 0 || t[_minDots[i]].Y > 0)
                    {
                        var add = startT + _maxDots[i];
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
            var dp2Start = minDots[endMin];
            peakCount = endMin - startMin;
            if (peakCount > 2)
            {
                minDots.RemoveAt(minDots.Count - 1);
                maxDots.RemoveAt(maxDots.Count - 1);
                --endMin;
                dp2Start = minDots[endMin];
            }

            minDots.Sort();
            var dp = 2;
            var sort = 1;
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

                AddSplitLine(CurrentBaseLine, DataSource[i]).DP = dp + "-" + sort;
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

        public BaseLine? GetBaseLine(Coordinates point)
        {
            return BaseLines.FirstOrDefault(v => v.Start.X <= point.X && v.End.X >= point.X);
        }

        public SplitLine AddSplitLine(Coordinates point)
        {
            var baseLine = GetBaseLine(point)!;
            return AddSplitLine(baseLine, point);
        }

        public SplitLine AddSplitLine(BaseLine baseLine, Coordinates point)
        {
            var chartLine = baseLine.CreateSplitLine(point);
            var line = new SplitLine(baseLine, chartLine);
            AddSplitLine(line);
            return line;
        }

        public BaseLine AddBaseLine(BaseLine baseLine)
        {
            BaseLines.Add(baseLine);
            return baseLine;
        }

        /// <summary>
        /// 添加分割线
        /// </summary>
        /// <param name="line"></param>
        private void AddSplitLine(SplitLine line)
        {
            line.SplitLineMoved += OnLineMoved;
            line.NextLineChanged += OnNextLineChanged;
            var index = SplitLines.BinaryInsert(line);
            line.Index = index + 1;
            for (var i = index + 1; i < SplitLines.Count; ++i)
            {
                ++SplitLines[i].Index;
            }

            if (index == 0)
            {
                line.NextLine = line.BaseLine;
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

        public static async Task<DraggableChartVm> CreateAsync(string filePath, ExportType exportType)
        {
            var (dataSource, saveLine) = await ReadCsv(filePath).ConfigureAwait(false);

            var res = new DraggableChartVm(filePath, dataSource, exportType);
            if (saveLine != null)
                res.ApplyResult(saveLine);
            return res;
        }

        private void ApplyResult(string[] lines)
        {
            var baseInfo = lines[0].Split(',');
            exportType = Enum.Parse<ExportType>(baseInfo[1]);
            if (SingleBaseLine)
            {
                CurrentBaseLine = GetDefaultBaseLine();
                BaseLines.Add(CurrentBaseLine);
                CurrentBaseLine.Start = new Coordinates(double.Parse(baseInfo[2]), double.Parse(baseInfo[3]));
                CurrentBaseLine.End = new Coordinates(double.Parse(baseInfo[4]), double.Parse(baseInfo[5]));
            }
            for (var i = 2; i < lines.Length; i++)
            {
                string[] data = lines[i].Split(',');
                var x = double.Parse(data[1]);
                var point = GetChartPoint(x);
                if (point.HasValue)
                    AddSplitLine(point.Value).DP = data[6][2..].TrimEnd('\r');
            }

            initialized = true;
        }

        public static DraggableChartVm Create(CacheContent cache)
        {
            var dataSource = Enumerable.Range(0, cache.X.Length).Select(i => new Coordinates(cache.X[i], cache.Y[i]))
                .ToArray();
            var vm = new DraggableChartVm(cache.FilePath, dataSource, ExportType.None);
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

        //ToDo: 多基线保存
        private string[] GetSaveRowContent()
        {
            var baseInfo =
                $"{FileName},{exportType},{CurrentBaseLine?.Start.X},{CurrentBaseLine?.Start.Y},{CurrentBaseLine?.End.X},{CurrentBaseLine?.End.Y},";
            var title = "Peak,Start X,End X,Center X,Area,Area Sum %,DP";
            IEnumerable<string> lines = SplitLines.Select(x =>
                $"{x.Index},{x.Start.X:f3},{x.NextLine.Start.X:f3},{x.RT:f3},{x.Area:f2},{x.AreaRatio * 100:f2},DP{x.DP}");
            return lines.Prepend(title).Prepend(baseInfo).ToArray();
        }

        public SaveRow[] GetSaveRow()
        {
            SaveRow baseInfo = new("",
                $"{FileName},{exportType},{CurrentBaseLine?.Start.X},{CurrentBaseLine?.Start.Y},{CurrentBaseLine?.End.X},{CurrentBaseLine?.End.Y}");
            SaveRow title = new("", "Peak,Start X,End X,Center X,Area,Area Sum %");
            var lines = SplitLines.Select(x =>
                new SaveRow(x.DP!,
                    $"{x.Index},{x.Start.X:f3},{x.NextLine.Start.X:f3},{x.RT:f3},{x.Area:f2},{x.AreaRatio * 100:f2}"));
            return lines.Prepend(title).Prepend(baseInfo).ToArray();
        }

        public async Task<Result<bool>> SaveToFile()
        {
            try
            {
                static string GetDataLine(string line)
                {
                    var index = 0;
                    for (var i = 0; i < 3; ++i)
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
                for (var i = 0; i < datas.Length; ++i)
                {
                    var line = datas[i];
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
            var lines = saveContent.Split(Environment.NewLine);
            ApplyResult(lines);
        }

        /// <summary>
        /// 分割线移动时，检测是否跨过相邻分割线
        /// </summary>
        private void OnLineMoving(SplitLine splitLine, CoordinateLine oldValue, CoordinateLine newValue)
        {
            if (oldValue.Start.X < newValue.Start.X)
            {
                var after = SplitLines.ElementAtOrDefault(draggedSplitLineIndex + 1);
                if (after == null || !(oldValue.Start.X >= after.Start.X)) return;
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
                if (before == null || !(oldValue.Start.X <= before.Start.X)) return;
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
            //当前是最后一个分割线时更新总面积及分割线的面积比例
            if (line.Index == SplitLines.Count)
            {
                SumArea += change;
                foreach (var temp in SplitLines)
                {
                    temp.AreaRatio = temp.Area / SumArea;
                }
            }

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



            //处理移动对右边一条线的影响
            var rightLine = SplitLines.ElementAtOrDefault(line.Index);
            if (rightLine is null)
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

        private static async Task<(Coordinates[], string[]?)> ReadCsv(string path)
        {
            char[] spe = [',', '\t'];
            string[] data;
            var hasResult = false;
            var fileName = Path.GetFileNameWithoutExtension(path);
            List<string> saveContent = new List<string>();

            static string? GetSaveLine(string line)
            {
                var index = 0;
                for (var i = 0; i < 4; ++i)
                {
                    index = line.IndexOf(',', index) + 1;
                    if (index == 0)
                        return null;
                }

                return line[index..];
            }

            using (StreamReader sr = new(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                var first = sr.ReadLine();
                if (first is not null)
                {
                    var temp = GetSaveLine(first);
                    if (temp is not null)
                    {
                        saveContent.Add(temp);
                        hasResult = true;
                    }
                }

                var second = sr.ReadLine();
                if (hasResult && second != null)
                    saveContent.Add(GetSaveLine(second)!);
                data = (await sr.ReadToEndAsync().ConfigureAwait(false)).Split(Environment.NewLine,
                    StringSplitOptions.RemoveEmptyEntries);
                if (hasResult)
                {
                    for (var i = 0; i < data.Length; ++i)
                    {
                        var temp = GetSaveLine(data[i]);
                        if (temp is null)
                            break;
                        saveContent.Add(temp);
                    }
                }
            }

            try
            {
                double[][] temp = data
                    .Select(v => v.Split(spe).Skip(1).Take(2).Select(v1 => double.Parse(v1)).ToArray())
                    .Where(v => v[0] >= 20 && v[0] <= 60).ToArray();
                return (temp.Select(v => new Coordinates(v[0], v[1])).ToArray(),
                    hasResult ? saveContent.ToArray() : null);
            }
            catch
            {
                throw new Exception(Path.GetFileNameWithoutExtension(path) + "数据格式错误");
            }
        }
    }
}
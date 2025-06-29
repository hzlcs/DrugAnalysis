﻿using ChartEditLibrary.Model;
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
using Range = System.Range;
using MathNet.Numerics.LinearAlgebra.Factorization;
using static ChartEditLibrary.Model.DescriptionManager;

namespace ChartEditLibrary.ViewModel
{
    public partial class DraggableChartVm : ObservableObject
    {
        public event Action<SplitLine>? DraggedLineChanged;

        public string Description { get; }

        /// <summary>
        /// x轴单位间距
        /// </summary>
        public double Unit { get; }

        private double HalfUnit { get; }

        public string FileName { get; }

        public string FilePath { get; }

        [ObservableProperty]
        private DraggedLineInfo? draggedLine;

        private int draggedSplitLineIndex;

        public Coordinates YMax { get; }

        public ObservableCollection<BaseLine> BaseLines { get; } = [];

        public BaseLine? CurrentBaseLine { get; private set; }

        public ObservableCollection<SplitLine> SplitLines { get; } = [];

        public CoordinateLine[]? CuttingLines { get; private set; }

        public Coordinates[] DataSource { get; }

        public ExportType exportType;

        /// <summary>
        /// 鼠标操作的敏感度
        /// </summary>
        public Vector2d Sensitivity { get; set; }

        private double SumArea { get; set; }

        private readonly int highestIndex;

        private readonly int lowestIndex;

        private int[] minDots;

        private int[] maxDots;

        /// <summary>
        /// 是否初始化
        /// </summary>
        private bool initialized = false;

        private static readonly LabelData[] commonTitles =
            [new("Peak", 52), new("Start X", 75), new("Center X", 75), new("End X", 70), new("Area", 60), new("Area Sum %", 95)];
        private static readonly LabelData[] twoDTiltes =
            [new("Peak", 52), new("Center X", 75), new("Area Sum %", 95), new("ASP", 70), new("TSP", 60), new("offset%", 75)];

        public LabelData[] ChartDataTitles { get; } = null!;

        private DraggableChartVm(string filePath, Coordinates[] dataSource, ExportType? exportType, string description)
        {
            this.FilePath = filePath;
            this.FileName = Path.GetFileNameWithoutExtension(filePath);
            Description = description;
            this.exportType = exportType ?? ExportType.Enoxaparin;
            this.DataSource = dataSource;
            for (int i = 0; i < 3; ++i)
                Unit += dataSource[i + 1].X - dataSource[i].X;
            Unit /= 3;
            HalfUnit = Unit / 2;

            if (exportType == ExportType.TwoDimension)
            {
                ChartDataTitles = twoDTiltes;
            }
            else
            {
                ChartDataTitles = commonTitles;
            }
            GetPoints(dataSource, 1, dataSource.Length - 1, out minDots, out maxDots);
            lowestIndex = minDots.MinBy(v => DataSource[v].Y);
            highestIndex = maxDots.MaxBy(v => DataSource[v].Y);
            YMax = DataSource[highestIndex];

            //this.DataSource = dataSource.Concat(GetT(dataSource, 0, dataSource.Length - 2)).ToArray();
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
        /// 基线变动时，更新所有分割线的面积
        /// </summary>
        /// <param name="_"></param>
        /// <param name="newValue"></param>
        public void UpdateBaseLine(BaseLine baseLine)
        {
            var newValue = baseLine.Line;
            var splitLines = baseLine.SplitLines;
            foreach (SplitLine i in splitLines)
            {
                var newY1 = newValue.Y(i.Start.X);
                i.Start = new Coordinates(i.Start.X, newY1);
            }
            foreach (SplitLine i in splitLines)
            {
                i.Area = GetArea(i);
            }
            UpdateArea();
        }

        /// <summary>
        /// 获取X在数据源中的索引,-1表示不在数据源中
        /// </summary>
        public int GetDateSourceIndex(double x)
        {
            if(x < DataSource[0].X || x > DataSource[^1].X)
                return -1;
            var index = (int)((x - DataSource[0].X) / Unit);
            Debug.Assert(index > 0 && index < DataSource.Length);
            double diff = x - DataSource[index].X;
            if(diff < 0)
            {
                if(-diff > HalfUnit)
                    return index - 1;
            }
            else
            {
                if(diff > HalfUnit)
                    return index + 1;
            }
            return index;
        }

        public Coordinates GetDataSource(double x)
        {
            return DataSource[GetDateSourceIndex(x)];
        }

        public BaseLine? GetBaseLine(double x)
        {
            return BaseLines.FirstOrDefault(v => (v.Start.X < x || Utility.ToleranceEqual(v.Start.X, x)) && (v.End.X > x || Utility.ToleranceEqual(v.End.X, x)));
        }

        public BaseLine? GetBaseLine(Coordinates point)
        {
            return GetBaseLine(point.X);
        }
        public BaseLine GetBaseLineOrNearest(Coordinates point)
        {
            var baseLine = GetBaseLine(point.X);
            if (baseLine is not null)
                return baseLine;
            Debug.Assert(BaseLines.Count > 0);
            double min = double.MaxValue;
            baseLine = BaseLines[0];
            foreach (var line in BaseLines)
            {
                double temp = Math.Min(Math.Abs(line.Start.X - point.X), Math.Abs(line.End.X - point.X));
                if (temp < min)
                {
                    min = temp;
                    baseLine = line;
                }
            }
            return baseLine;
        }


        public void RemoveLine(EditLineBase editLine)
        {
            if (editLine is BaseLine baseLine)
            {
                RemoveBaseLine(baseLine);
            }
            else if (editLine is SplitLine splitLine)
            {
                RemoveSplitLine(splitLine);
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
            var index = SplitLines.IndexOf(line);
            for (var i = index + 1; i < SplitLines.Count; ++i)
            {
                --SplitLines[i].Index;
            }
            SplitLines.Remove(line);
            line.BaseLine.RemoveSplitLine(line, this);
            if (line.BaseLine.SplitLines.Count == 0)
                BaseLines.Remove(line.BaseLine);
            UpdateArea();
            DraggedLine = null;
        }
        public void RemoveBaseLine(BaseLine baseLine)
        {
            BaseLines.Remove(baseLine);
            foreach (var i in baseLine.SplitLines.ToArray())
            {
                RemoveSplitLine(i);
            }
        }

        public BaseLine AddBaseLine(BaseLine baseLine)
        {
            if (IsEndPoint(baseLine.Start))
                baseLine.Start = GetDataSource(baseLine.Start.X);
            if (IsEndPoint(baseLine.End))
                baseLine.End = GetDataSource(baseLine.End.X);
            if (BaseLines.Count == 0)
            {
                BaseLines.Add(baseLine);
            }
            else
            {
                for (int i = 0; i < BaseLines.Count; ++i)
                {
                    if (Utility.ToleranceEqual(baseLine.Start.X, BaseLines[i].Start.X))
                    {
                        break;
                    }
                    if (baseLine.Start.X < BaseLines[i].Start.X)
                    {
                        BaseLines.Insert(i, baseLine);
                        break;
                    }
                    else if (i == BaseLines.Count - 1)
                    {
                        BaseLines.Add(baseLine);
                        break;
                    }
                }
            }

            return baseLine;
        }

        public SplitLine AddSplitLine(Coordinates point)
        {
            var baseLine = GetBaseLineOrNearest(point);
            return AddSplitLine(baseLine, point);
        }

        public SplitLine AddSplitLine(BaseLine baseLine, Coordinates point)
        {
            var chartLine = baseLine.CreateSplitLine(point);
            var line = new SplitLine(baseLine, chartLine);
            AddSplitLine(line);
            return line;
        }

        /// <summary>
        /// 添加分割线
        /// </summary>
        /// <param name="line"></param>
        private void AddSplitLine(SplitLine line)
        {
            if (SplitLines.Any(v => Utility.ToleranceEqual(v.Start.X, line.Start.X)))
                return;
            line.SplitLineMoved += OnLineMoved;
            line.NextLineChanged += OnNextLineChanged;
            var index = SplitLines.BinaryInsert(line);
            line.Index = index + 1;
            for (var i = index + 1; i < SplitLines.Count; ++i)
            {
                ++SplitLines[i].Index;
            }
            line.BaseLine.AddSplitLine(line, this);
            line.SetCuttingData(CuttingLines, this);
            UpdateArea();
        }

        public void UpdateArea()
        {
            SumArea = SplitLines.Sum(x => x.Area);
            foreach (var i in SplitLines)
            {
                i.AreaRatio = i.Area / SumArea;
            }
        }

        public void ResetArea()
        {
            foreach (var line in SplitLines)
                line.Area = GetArea(line);
            UpdateArea();
        }

        public void ApplyStandard(DraggableChartVm? std)
        {
            if (std is not null)
            {
                foreach (var line in std.SplitLines.Reverse())
                {
                    var near = SplitLines.MinBy(v => Math.Abs(v.RT - line.RT));
                    if (near != null && Math.Abs(near.RT - line.RT) < 0.5)
                    {
                        near.Description = line.Description;
                    }
                }
            }
            //s1
            var s1Index = SplitLines.FirstIndex(v => v.Description == GluDescription.S1);
            var s1_1 = SplitLines.ElementAtOrDefault(s1Index - 1);
            if (s1_1 is not null)
                s1_1.Description = GluDescription.S1_1;

            //s2
            var s2Index = SplitLines.FirstIndex(v => v.Description == GluDescription.S2);
            int peakCount = s2Index - s1Index - 1;
            if (peakCount == 2)
            {
                SplitLines[s2Index - 2].Description = GluDescription.S2_1;
                SplitLines[s2Index - 1].Description = GluDescription.S2_2;
            }
            else if (peakCount == 1)
            {
                double rate = (SplitLines[s2Index].RT - SplitLines[s2Index - 1].RT) / (SplitLines[s2Index].RT - SplitLines[s1Index].RT);
                if (rate > 0.3)
                    SplitLines[s2Index - 1].Description = GluDescription.S2_1;
                else
                    SplitLines[s2Index - 1].Description = GluDescription.S2_2;
            }

            //s5
            var s5Index = SplitLines.FirstIndex(v => v.Description == GluDescription.S5);
            var s5 = SplitLines[s5Index - 1];
            if (SplitLines[s5Index].RT - s5.RT < 1)
                s5.Description = GluDescription.S5_1;

            //s7
            var s7Index = SplitLines.FirstIndex(v => v.Description == GluDescription.S7);
            var s7 = SplitLines[s7Index - 1];
            if (SplitLines[s7Index].RT - s7.RT < 0.5)
                s7.Description = GluDescription.S7_1;
            else
            {
                s7 = SplitLines[s7Index + 1];
                if (SplitLines[s7Index].RT - s7.RT < 0.5)
                    s7.Description = GluDescription.S7_1;
            }

            //s8
            var s8Index = SplitLines.FirstIndex(v => v.Description == GluDescription.S8);
            peakCount = s8Index - s7Index - 1;
            if (peakCount == 2)
            {
                SplitLines[s8Index - 2].Description = GluDescription.S8_1;
                SplitLines[s8Index - 1].Description = GluDescription.S8_2;
            }
            string[] s8Descriptions = [GluDescription.f, GluDescription.g, GluDescription.h, GluDescription.i, GluDescription.j, GluDescription.k];
            int gluIndex = 0;
            double s8RT = SplitLines[s8Index].RT;
            for (int i = s8Index + 1; i < SplitLines.Count; ++i)
            {
                var line = SplitLines[i];
                double height = line.GetHeight(DataSource);
                double interval = line.RT - s8RT;
                if (gluIndex < 1 && interval <= 3 && height <= 5)
                {
                    line.Description = GluDescription.f;
                    gluIndex = 1;
                    continue;
                }
                if (gluIndex < 2 && interval <= 4 && Math.Abs(10 - height) <= 2)
                {
                    line.Description = GluDescription.g;
                    gluIndex = 2;
                    continue;
                }
                if (gluIndex < 3 && interval <= 6 && interval >= 3 && Math.Abs(5.5 - height) <= 1.5)
                {
                    line.Description = GluDescription.h;
                    gluIndex = 3;
                    continue;
                }
                if (gluIndex < 4 && interval >= 6 && Math.Abs(8 - height) <= 2)
                {
                    line.Description = GluDescription.i;
                    gluIndex = 4;
                    continue;
                }
                if (gluIndex < 5 && interval <= 8 && interval >= 7 && height <= 5)
                {
                    line.Description = GluDescription.j;
                    gluIndex = 5;
                    continue;
                }
                if (gluIndex < 6 && interval >= 8 && height <= 5)
                {
                    line.Description = GluDescription.k;
                    gluIndex = 6;
                    break;
                }

            }

        }

        public void ApplyTemplate(DraggableChartVm template, double? _xOffset = null)
        {
            var tHighest = template.YMax.X;
            double xOffset = _xOffset ?? YMax.X - tHighest;
            foreach (var baseline in template.BaseLines)
            {
                //尝试延申至端点
                CoordinateLine templateEndPoingLine = baseline.EndPointLine;
                //获取样品对应的延申至端点的基线
                CoordinateLine endPointLine;
                Coordinates start;
                var startX = templateEndPoingLine.Start.X + xOffset;
                var startIndex = GetDateSourceIndex(startX);
                if (template.IsEndPoint(templateEndPoingLine.Start)) //默认基线所有点都是谷点
                {
                    start = GetVstreetPoint(startIndex);
                    //校验找到的谷点是不是对应了模板的另一个右边的谷点
                    var templateX = start.X - xOffset;
                    var tVstreet = template.GetVstreetPoint(templateX);
                    if (tVstreet.X - templateEndPoingLine.Start.X > 3 * Unit)
                    {
                        int index = GetNearestMinDotIndex(startIndex) - 1;
                        start = DataSource[minDots[index]];
                    }

                }
                else
                {
                    start = DataSource[startIndex];
                    //如果模板不是端点，则采用相对高度
                    if (!template.IsEndPoint(templateEndPoingLine.Start))
                        start = new Coordinates(start.X, start.Y - template.GetYOffset(templateEndPoingLine.Start));
                }
                Coordinates end;
                var endX = templateEndPoingLine.End.X + xOffset;
                var endIndex = GetDateSourceIndex(endX);
                if (template.IsEndPoint(templateEndPoingLine.End))
                {
                    end = GetVstreetPoint(endIndex);
                }
                else
                {
                    end = DataSource[endIndex];
                    if (!template.IsEndPoint(templateEndPoingLine.End))
                        end = new Coordinates(end.X, end.Y - template.GetYOffset(templateEndPoingLine.End));
                }
                endPointLine = new CoordinateLine(start, end);
                //CheckCrossedBaseline(ref endPointLine);

                //检验基线间是否存在其它谷点
                CheckOtherVstreet(ref endPointLine);



                startX = baseline.Start.X + xOffset;
                if (template.IsVstreetPoint(baseline.Start) || template.IsEndPoint(baseline.Start))
                {
                    start = GetVstreetPoint(GetDateSourceIndex(startX));
                }
                else
                {
                    start = GetDataSource(startX);
                }
                start = new Coordinates(start.X, endPointLine.Y(start.X));

                endX = baseline.End.X + xOffset;
                if (template.IsVstreetPoint(baseline.End) || template.IsEndPoint(baseline.End))
                    end = GetVstreetPoint(GetDateSourceIndex(endX));
                else
                    end = GetDataSource(endX);
                end = new Coordinates(end.X, endPointLine.Y(end.X));

                var line = new CoordinateLine(start, end);
                if (IsVstreetPoint(start))
                {
                    CheckCrossedBaseline(ref line);
                }
                else
                {
                    if (template.IsEndPoint(baseline.Start))
                        TrySetVstreetBaseline(ref line);
                }

                AddBaseLine(new BaseLine(line));

            }
            List<double> stdOffset = [];
            foreach (var sl in template.SplitLines)
            {
                int index = GetDateSourceIndex(sl.Start.X + xOffset);
                var baseline = GetBaseLineOrNearest(DataSource[index]);
                var point = GetVstreetPoint(index);

                if (point.X < baseline.Start.X)
                {
                    continue;
                    //baseline.Start = new Coordinates(point.X, baseline.Line.Y(point.X));
                }
                else if (point.X > baseline.End.X)
                {
                    //continue;
                    if (point.X > baseline.End.X + Unit * 5)
                        continue;
                    double y = baseline.Line.Y(point.X);
                    var p = GetDataSource(point.X);
                    if (y >= p.Y)
                    {
                        baseline.End = p;
                    }
                    else
                    {
                        baseline.End = new Coordinates(point.X, baseline.Line.Y(point.X));
                    }

                }
                var line = AddSplitLine(point);
                if (DataSource[line.RTIndex].Y - line.BaseLine.Line.Y(line.RT) < MutiConfig.Instance.MinHeight)
                {
                    RemoveLine(line);
                    continue;
                }
                line.Description = sl.Description;

                if (GluDescription.StdDescriptions.Contains(line.Description))
                    stdOffset.Add(line.RT - sl.RT);
            }
            if (_xOffset is null && stdOffset.Count > 0)
            {
                var min = stdOffset.Min();
                var max = stdOffset.Max();
                var avgStd = (stdOffset.Sum() - min - max) / (stdOffset.Count - 2);
                if (Math.Abs(avgStd - xOffset) > 0.05)
                {
                    BaseLines.Clear();
                    SplitLines.Clear();
                    ApplyTemplate(template, avgStd);
                }
            }

            foreach (var line in BaseLines.ToArray())
                if (line.SplitLines.Count == 0)
                    RemoveBaseLine(line);

        }

        public void ApplyTemplateTwoD(DraggableChartVm template, double? _xOffset = null)
        {
            var tHighest = template.YMax.X;
            double xOffset = _xOffset ?? YMax.X - tHighest;
            foreach (var baseline in template.BaseLines)
            {
                //尝试延申至端点
                CoordinateLine templateEndPoingLine = baseline.EndPointLine;
                //获取样品对应的延申至端点的基线
                CoordinateLine endPointLine;
                Coordinates start;
                var startX = templateEndPoingLine.Start.X + xOffset;
                var startIndex = GetDateSourceIndex(startX);
                if (template.IsEndPoint(templateEndPoingLine.Start)) //默认基线所有点都是谷点
                {
                    start = GetVstreetPoint(startIndex);
                    //校验找到的谷点是不是对应了模板的另一个右边的谷点
                    var templateX = start.X - xOffset;
                    var tVstreet = template.GetVstreetPoint(templateX);
                    if (tVstreet.X - templateEndPoingLine.Start.X > 3 * Unit)
                    {
                        int index = GetNearestMinDotIndex(startIndex) - 1;
                        start = DataSource[minDots[index]];
                    }

                }
                else
                {
                    start = DataSource[startIndex];
                    //如果模板不是端点，则采用相对高度
                    if (!template.IsEndPoint(templateEndPoingLine.Start))
                        start = new Coordinates(start.X, start.Y - template.GetYOffset(templateEndPoingLine.Start));
                }
                Coordinates end;
                var endX = templateEndPoingLine.End.X + xOffset;
                var endIndex = GetDateSourceIndex(endX);
                if (template.IsEndPoint(templateEndPoingLine.End))
                {
                    end = GetVstreetPoint(endIndex);
                }
                else
                {
                    end = DataSource[endIndex];
                    if (!template.IsEndPoint(templateEndPoingLine.End))
                        end = new Coordinates(end.X, end.Y - template.GetYOffset(templateEndPoingLine.End));
                }
                endPointLine = new CoordinateLine(start, end);
                //CheckCrossedBaseline(ref endPointLine);

                //检验基线间是否存在其它谷点
                CheckOtherVstreet(ref endPointLine);



                startX = baseline.Start.X + xOffset;
                if (template.IsVstreetPoint(baseline.Start) || template.IsEndPoint(baseline.Start))
                {
                    start = GetVstreetPoint(GetDateSourceIndex(startX));
                }
                else
                {
                    start = GetDataSource(startX);
                }
                start = new Coordinates(start.X, endPointLine.Y(start.X));

                endX = baseline.End.X + xOffset;
                if (template.IsVstreetPoint(baseline.End) || template.IsEndPoint(baseline.End))
                    end = GetVstreetPoint(GetDateSourceIndex(endX));
                else
                    end = GetDataSource(endX);
                end = new Coordinates(end.X, endPointLine.Y(end.X));

                var line = new CoordinateLine(start, end);
                if (IsVstreetPoint(start))
                {
                    CheckCrossedBaseline(ref line);
                }
                else
                {
                    if (template.IsEndPoint(baseline.Start))
                        TrySetVstreetBaseline(ref line);
                }

                AddBaseLine(new BaseLine(line));

            }

            foreach (var sl in template.SplitLines)
            {
                int index = GetDateSourceIndex(sl.Start.X + xOffset);
                var baseline = GetBaseLineOrNearest(DataSource[index]);
                Coordinates point;
                bool vstreet = template.IsVstreetPoint(sl.Start);
                if (vstreet)
                {
                    point = GetVstreetPoint(index);
                    //var orgin = template.GetVstreetPoint(point.X - xOffset);
                    //if(orgin.X != sl.Start.X)
                    //{
                    //    if(orgin.X > sl.Start.X)
                    //        point = DataSource[minDots[GetNearestMinDotIndex(point.X) - 1]];
                    //    else 
                    //        point = DataSource[minDots[GetNearestMinDotIndex(point.X) + 1]];
                    //}
                }
                else
                    point = DataSource[index];
                if (point.X < baseline.Start.X)
                {
                    continue;
                    //baseline.Start = new Coordinates(point.X, baseline.Line.Y(point.X));
                }
                else if (point.X > baseline.End.X)
                {
                    //continue;
                    if (point.X > baseline.End.X + Unit * 5 && !vstreet)
                        continue;
                    double y = baseline.Line.Y(point.X);
                    var p = GetDataSource(point.X);
                    if (y >= p.Y)
                    {
                        baseline.End = p;
                    }
                    else
                    {
                        baseline.End = new Coordinates(point.X, baseline.Line.Y(point.X));
                    }

                }
                var line = AddSplitLine(point);
                if (DataSource[line.RTIndex].Y - line.BaseLine.Line.Y(line.RT) < (double)TwoDConfig.Instance.MinHeight)
                {
                    RemoveLine(line);
                    continue;
                }
                line.Description = sl.Description;

            }

            foreach (var line in BaseLines.ToArray())
                if (line.SplitLines.Count == 0)
                    RemoveBaseLine(line);

        }

        private void CheckOtherVstreet(ref CoordinateLine line)
        {
            var s = GetDateSourceIndex(line.Start.X);
            var e = GetDateSourceIndex(line.End.X);
            var mid = GetNearestMaxDot((s + e) / 2);
            var start = GetNearestMinDotIndex(s);
            var end = GetNearestMinDotIndex(e);
            while (true)
            {
                var index = minDots[start];
                ++start;
                if (index <= s)
                    continue;
                if (index > mid)
                    break;
                var point = DataSource[index];
                if (line.Y(point.X) >= point.Y)
                {
                    line = new CoordinateLine(point, line.End);
                }
            }
            while (true)
            {
                var index = minDots[end];
                --end;
                if (index >= e)
                    continue;
                if (index < mid)
                    break;
                var point = DataSource[index];
                if (line.Y(point.X) >= point.Y)
                {
                    line = new CoordinateLine(line.Start, point);
                }
            }

        }

        private void CheckCrossedBaseline(ref CoordinateLine line)
        {
            int start = GetDateSourceIndex(line.Start.X);
            int end = maxDots.First(v => v > start);
            int count = 0;
            double sum = 0;
            double max = double.MinValue;
            int maxX = start;
            for (int i = start; i < end; ++i)
            {
                var dot = DataSource[i];
                var x = line.Y(dot.X) - dot.Y;
                if (x > 0)
                {
                    ++count;
                    sum += x;
                    if (x > max)
                        max = x;
                    maxX = i;
                }
            }
            if (count <= 5)
            {
                if (new StackTrace().GetFrame(1)?.GetMethod()?.Name == nameof(CheckCrossedBaseline))
                {
                    if (!IsEndPoint(line.Start))
                    {
                        int index = GetDateSourceIndex(line.Start.X);
                        var temp = line;
                        var min = DataSource[index..(index + 10)].MinBy(v => Math.Abs(v.Y - temp.Y(v.X)));
                        line = new CoordinateLine(GetDataSource(min.X), line.End);
                    }
                }

                return;
            }
            line = new CoordinateLine(DataSource[(start + maxX) / 2], line.End);
            CheckCrossedBaseline(ref line);

        }

        private void TrySetVstreetBaseline(ref CoordinateLine line)
        {
            int index = GetDateSourceIndex(line.Start.X);
            int leftVstreet = minDots.Reverse().First(v => v < index);
            line = new CoordinateLine(DataSource[leftVstreet], line.End);
            CheckCrossedBaseline(ref line);
        }


    }
}
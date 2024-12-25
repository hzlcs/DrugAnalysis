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
using Range = System.Range;

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

        public string FileName { get; }

        public string FilePath { get; }

        [ObservableProperty]
        private DraggedLineInfo? draggedLine;

        private int draggedSplitLineIndex;

        private readonly Coordinates yMax;

        public ObservableCollection<BaseLine> BaseLines { get; } = [];

        public BaseLine? CurrentBaseLine { get; private set; }

        public ObservableCollection<SplitLine> SplitLines { get; } = [];

        public Coordinates[] DataSource { get; }

        public ExportType exportType;

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

        private DraggableChartVm(string filePath, Coordinates[] dataSource, ExportType? exportType, string description)
        {
            this.FilePath = filePath;
            this.FileName = Path.GetFileNameWithoutExtension(filePath);
            Description = description;
            this.exportType = exportType ?? ExportType.Enoxaparin;
            this.DataSource = dataSource;
            Unit = dataSource[1].X - dataSource[0].X;
            yMax = dataSource[0];
            double max = dataSource[0].Y;
            var span = dataSource.AsSpan();
            for (var i = 0; i < dataSource.Length; ++i)
            {
                if (span[i].Y > max)
                {
                    highestIndex = i;
                    max = span[i].Y;
                }
            }
            yMax = dataSource[highestIndex];
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
            var index = (int)((x - DataSource[0].X) / Unit);
            if (index < 0 || index >= DataSource.Length - 1)
                return -1;
            if (Math.Abs(DataSource[index].X - x) > Math.Abs(DataSource[index + 1].X - x))
                index += 1;
            return index;
        }

        public Coordinates GetDataSource(double x)
        {
            return DataSource[GetDateSourceIndex(x)];
        }

        public BaseLine? GetBaseLine(double x)
        {
            return BaseLines.FirstOrDefault(v => v.Start.X <= x && v.End.X >= x);
        }

        public BaseLine? GetBaseLine(Coordinates point)
        {
            return GetBaseLine(point.X);
        }
        public BaseLine GetBaseLineOrNearest(Coordinates point)
        {
            var baseLine = GetBaseLine(point);
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
            return baseLine!;
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
            if (BaseLines.Count == 0)
            {
                BaseLines.Add(baseLine);
            }
            for (int i = 0; i < BaseLines.Count; ++i)
            {
                if (baseLine.Start.X < BaseLines[i].Start.X)
                {
                    BaseLines.Insert(i, baseLine);
                    break;
                }
                else if(i == BaseLines.Count - 1)
                {
                    BaseLines.Add(baseLine);
                    break;
                }
            }
            return baseLine;
        }

        public SplitLine AddSplitLine(Coordinates point)
        {
            var baseLine = GetBaseLine(point);
            baseLine ??= BaseLines[0];
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
            line.SplitLineMoved += OnLineMoved;
            line.NextLineChanged += OnNextLineChanged;
            var index = SplitLines.BinaryInsert(line);
            line.Index = index + 1;
            for (var i = index + 1; i < SplitLines.Count; ++i)
            {
                ++SplitLines[i].Index;
            }
            line.BaseLine.AddSplitLine(line, this);
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

        public void ApplyStandard(DraggableChartVm std)
        {
            foreach (var line in std.SplitLines.Reverse())
            {
                var near = SplitLines.MinBy(v => Math.Abs(v.RT - line.RT));
                if (near != null)
                {
                    near.Description = line.Description;
                }
            }
        }

        public void ApplyTemplate(DraggableChartVm template)
        {
            var tHighest = template.SplitLines.MaxBy(v => DataSource[v.RTIndex].Y)!.RT;
            var offset = yMax.X - tHighest;
            foreach (var baseline in template.BaseLines)
            {
                double startYOffset = template.GetYOffset(baseline.Start);
                Coordinates start = GetDataSource(baseline.Start.X + offset);
                start = new Coordinates(start.X, start.Y - startYOffset);
                if (template.IsVstreetPoint(baseline.Start))
                {
                    var vstreet = GetVstreetPoint(start);
                    start = new Coordinates(vstreet.X, vstreet.Y - startYOffset);
                }

                double endYOffset = template.GetYOffset(baseline.End);
                Coordinates end = GetDataSource(baseline.End.X + offset);
                end = new Coordinates(end.X, end.Y - endYOffset);
                if (template.IsVstreetPoint(baseline.End))
                {
                    var vstreet = GetVstreetPoint(end);
                    end = new Coordinates(vstreet.X, vstreet.Y - endYOffset);
                }
                AddBaseLine(new BaseLine(start, end));
            }
            foreach (var sl in template.SplitLines)
            {
                int index = GetDateSourceIndex(sl.Start.X + offset);
                var baseline = GetBaseLineOrNearest(DataSource[index]);
                var point = GetVstreetPoint(index);

                if (point.X < baseline.Start.X)
                {
                    baseline.Start = new Coordinates(point.X, baseline.Line.Y(point.X));
                }
                else if (point.X > baseline.End.X)
                {
                    baseline.End = new Coordinates(point.X, baseline.Line.Y(point.X));
                }

                AddSplitLine(point).Description = sl.Description;
            }

        }

    }
}
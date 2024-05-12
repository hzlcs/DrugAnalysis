using CommunityToolkit.Mvvm.ComponentModel;
using OpenTK;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWinform.ChartCore.Entity
{
    public partial class DraggableChartVM : ObservableObject
    {
        public string FileName { get; init; }

        [ObservableProperty]
        private DraggedLineInfo? draggedLine;

        private int draggedSplitLineIndex;

        public Coordinates YMax { get; init; }

        public Coordinates YMinHalf { get; init; }

        public BaseLine BaseLine { get; init; }

        public ObservableCollection<SplitLine> SplitLines { get; init; } = [];

        public Coordinates[] DataSource { get; init; }

        public Vector2d Sensitivity { get; set; }

        public double SumArea { get; set; }

        public DraggableChartVM(string fileName)
        {
            this.FileName = Path.GetFileNameWithoutExtension(fileName);
            var dataSource = Utility.ReadCsv(fileName, 1);
            this.DataSource = dataSource;

            YMax = dataSource[0];
            YMinHalf = dataSource[0];
            int half = dataSource.Length / 2;
            for (int i = 0; i < dataSource.Length; ++i)
            {
                var data = dataSource[i];
                if (data.Y > YMax.Y)
                {
                    YMax = data;
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
            if (oldValue.HasValue && !oldValue.Value.IsBaseLine)
            {
                oldValue.Value.DraggedLine.PropertyChanged -= OnLineMoved;
            }
            //添加对当前线的移动监测
            if (newValue.HasValue && !newValue.Value.IsBaseLine)
            {
                draggedSplitLineIndex = SplitLines.IndexOf((SplitLine)newValue.Value.DraggedLine);
                newValue.Value.DraggedLine.PropertyChanged += OnLineMoved;
            }
        }

        /// <summary>
        /// 分割线移动时，检测是否跨过相邻分割线
        /// </summary>
        private void OnLineMoved(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not SplitLine line)
                return;
            if (e.PropertyName != nameof(EditLineBase.Line))
                return;

            var before = SplitLines.ElementAtOrDefault(draggedSplitLineIndex - 1);
            //跨过分割线后，交换其与跨过的线在集合中的位置
            if (before != null && line.Start.X < before.Start.X)
            {
                SplitLines[draggedSplitLineIndex] = before;
                SplitLines[draggedSplitLineIndex - 1] = line;
                return;
            }

            var after = SplitLines.ElementAtOrDefault(draggedSplitLineIndex + 1);
            if (after != null && line.Start.X > after.Start.X)
            {
                SplitLines[draggedSplitLineIndex] = after;
                SplitLines[draggedSplitLineIndex + 1] = line;
            }
        }

        /// <summary>
        /// 获取坐标点一定范围内的分割线或基线
        /// </summary>
        /// <param name="dataPoint"></param>
        /// <returns></returns>
        public DraggedLineInfo? GetDraggedLine(Coordinates dataPoint)
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
                var line = SplitLines.FirstOrDefault(x => x.Start.Y < dataPoint.Y && x.End.Y > dataPoint.Y && Math.Abs(x.Start.X - dataPoint.X) < Sensitivity.X);
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

            int index = (int)((x - DataSource[0].X) / Session.unit);
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
        /// 根据当前的基线创建该数据点的X在基线上的点
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
            return BaseLine.CreateSplitLinePoint(x);
        }

        public string GetDescription(out int? index)
        {
            string[] desc =
            [
                "基线:",
                $"起：({BaseLine.Start.X : 0.00}, {BaseLine.Start.Y : 0.00})" ,
                $"终：({BaseLine.End.X : 0.00}, {BaseLine.End.Y : 0.00})" ,
                $"斜率：{BaseLine.Line.Slope : 0.00}",
                "---------------------------------",
                "分割线：\n",
            ];
            string lineDesc = string.Join("\n", Enumerable.Range(1, SplitLines.Count)
                .Select(i => $"{i: 00}:({SplitLines[i - 1].End.X: 0.00}, {SplitLines[i - 1].End.Y: 0.0})"));
            if (BaseLine.IsSelected)
                index = 0;
            else
            {
                index = 0;
                while (index < SplitLines.Count && !SplitLines[index.Value].IsSelected)
                {
                    index++;
                }
                if (index.Value == SplitLines.Count)
                {
                    index = null;
                }
                else
                {
                    index = desc.Length + index.Value;
                }
            }

            return string.Join("\n", desc) + lineDesc;
        }

        /// <summary>
        /// 添加分割线
        /// </summary>
        /// <param name="line"></param>
        public void AddSplitLine(SplitLine line)
        {
            SplitLines.BinaryInsert(line);
        }

        /// <summary>
        /// 移除分割线
        /// </summary>
        /// <param name="line"></param>
        internal void RemoveSplitLine(SplitLine line)
        {
            SplitLines.Remove(line);
            DraggedLine = null;
        }

        public string GetSaveContent()
        {
            return GetDescription(out _);
        }

        public double GetArea(SplitLine line)
        {
            double unit = DataSource[1].X - DataSource[0].X;
            int index = SplitLines.IndexOf(line);
            int dataEnd = (int)((line.Start.X - DataSource[0].X) / unit);
            int dataStart;
            if (index == 0)
            {
                dataStart = (int)((BaseLine.Start.X - DataSource[0].X) / unit);
            }
            else
            {
                SplitLine left = SplitLines[index - 1];
                dataStart = (int)((left.Start.X - DataSource[0].X) / unit);
            }
            double area = 0;
            for (int i = dataStart; i < dataEnd; ++i)
            {
                area += DataSource[i].Y + DataSource[i + 1].Y;
            }
            return area * unit / 2;
        }
    }
}

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
        /// <summary>
        /// 通知前端数据发生变化
        /// </summary>
        public event Action? OnDataChanged;

        public string FileName { get; init; }

        [ObservableProperty]
        private DraggedLineInfo? draggedLine;

        private int draggedSplitLineIndex;

        public Coordinates YMax { get; init; }

        public Coordinates YMinHalf { get; init; }

        public BaseLine BaseLine { get; init; }

        public ObservableCollection<SplitLine> SplitLines { get; init; } = [];

        public Coordinates[] DataSource { get; init; }

        /// <summary>
        /// 鼠标操作的敏感度
        /// </summary>
        public Vector2d Sensitivity { get; set; }

        public double SumArea { get; set; }

        public DraggableChartVM(string fileName)
        {
            this.FileName = Path.GetFileNameWithoutExtension(fileName);
            var dataSource = Utility.ReadCsv(fileName, 1);
            this.DataSource = dataSource;
            Session.unit = dataSource[1].X - dataSource[0].X;
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
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// 分割线移动时，检测是否跨过相邻分割线
        /// </summary>
        private void OnLineMoving(EditLineBase sender, CoordinateLine oldValue, CoordinateLine newValue)
        {
            if (sender is not SplitLine line)
                return;
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

            OnDataChanged?.Invoke();


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
                SplitLine nearest = SplitLines.MinBy(x => Math.Abs(x.Start.X - dataPoint.X))!;
                SplitLine? line = null;
                if (Math.Abs(nearest.Start.X - dataPoint.X) < Sensitivity.X
                    && Math.Min(nearest.Start.Y, nearest.End.Y) < dataPoint.Y
                    && Math.Max(nearest.Start.Y, nearest.End.Y) > dataPoint.Y)
                {
                    line = nearest;
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
        /// 获取X在数据源中的索引
        /// </summary>
        public int GetDateSourceIndex(double x)
        {
            int index = (int)((x - DataSource[0].X) / Session.unit);
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
                }
            }
        }

        /// <summary>
        /// 移除分割线
        /// </summary>
        /// <param name="line"></param>
        internal void RemoveSplitLine(SplitLine line)
        {
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
                }
            }
            SplitLines.Remove(line);
            DraggedLine = null;
        }

        /// <summary>
        /// 获取保存于文件的内容
        /// </summary>
        /// <returns></returns>
        public string GetSaveContent()
        {
            string title = "Peak,Start X,End X,Center X,Area,Area Sum %,DP";
            IEnumerable<string> lines = SplitLines.Select(x => 
            $"{x.Index},{x.Start.X:f3},{x.NextLine.Start.X:f3},{x.RT:f3},{x.Area:f2},{x.AreaRatio * 100:f2},DP{x.DP}");
            return string.Join("\n", lines.Prepend(title));
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
            var res = area * Session.unit / 2;
            double y1 = 0 - BaseLine.GetY(DataSource[dataStart].X);
            double y2 = 0 - BaseLine.GetY(DataSource[dataEnd].X);
            res += (y1 + y2) * (DataSource[dataEnd].X - DataSource[dataStart].X) / 2;
            return res;
        }


    }

}

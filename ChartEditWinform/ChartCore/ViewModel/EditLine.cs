using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot;
using ScottPlot.Plottables;

namespace ChartEditWinform.ChartCore.Entity
{


    public abstract partial class EditLineBase : ObservableObject
    {
        protected delegate void SplitLineMovedEventHandler(SplitLine mover, CoordinateLine oldValue, CoordinateLine newValue);

        /// <summary>
        /// 分割线移动时（主要用于判断此次移动是否跨越其他线）
        /// </summary>
        public event LineMovingEventHandler? SplitLineMoving;
        /// <summary>
        /// 分割线移动后（主要用于处理移动变换，及处理对该线右边一条线的影响）
        /// </summary>
        protected event SplitLineMovedEventHandler? SplitLineMoved;


        public Coordinates Start
        {
            get => Line.Start;
            set
            {
                Line = new CoordinateLine(value, Line.End);
            }
        }

        public Coordinates End
        {
            get => Line.End;
            set
            {
                Line = new CoordinateLine(Line.Start, value);
            }
        }

        [ObservableProperty]
        private CoordinateLine line;

        /// <summary>
        /// 当前是否被选中
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 分割线移动时触发移动时事件
        /// </summary>
        partial void OnLineChanging(CoordinateLine oldValue, CoordinateLine newValue)
        {
            if (oldValue.Start.X == newValue.Start.X)
            {
                return;
            }
            SplitLineMoving?.Invoke(this, oldValue, newValue);
        }

        /// <summary>
        /// 分割线移动后触发移动后事件
        /// </summary>
        partial void OnLineChanged(CoordinateLine oldValue, CoordinateLine newValue)
        {
            if (this is not SplitLine line || oldValue.Start.X == newValue.Start.X)
            {
                return;
            }
            SplitLineMoved?.Invoke(line, oldValue, newValue);
        }

        public EditLineBase(CoordinateLine line)
        {
            this.line = line;
        }

        public EditLineBase(Coordinates start, Coordinates end)
        {
            line = new CoordinateLine(start, end);
        }

    }

    /// <summary>
    /// 分割线
    /// </summary>
    public partial class SplitLine : EditLineBase, IComparable<SplitLine>
    {
        /// <summary>
        /// 聚合度表
        /// </summary>
        private static readonly int[] DPTable = [2, 3, .. Enumerable.Range(2, 18).Select(v => v * 2)];
        /// <summary>
        /// 峰号
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// RT值在数据源中的索引
        /// </summary>
        public int RTIndex { get; set; }

        public double RT => bindChart.DataSource[RTIndex].X;

        public string? DP { get; set; }

        /// <summary>
        /// 用于显示在DataGridView中的Columns
        /// </summary>
        /// <param name="index">列号</param>
        public object this[int index]
        {
            get
            {
                return index switch
                {
                    0 => Index,
                    1 => RT,    //RT
                    2 => Area,  //面积
                    3 => NextLine.Start.X,  //开始
                    4 => Start.X,   //结束
                    5 => AreaRatio * 100,
                    6 => DP ?? "",
                    _ => "",
                };
            }
        }

        /// <summary>
        /// 当前绑定的数据源
        /// </summary>
        private readonly DraggableChartVM bindChart;

        public SplitLine(CoordinateLine line, DraggableChartVM bindChart) : base(line)
        {
            this.bindChart = bindChart;
            SplitLineMoved += OnLineMoved;
        }

        /// <summary>
        /// 处理分割线移动时的事件
        /// </summary>
        private void OnLineMoved(EditLineBase mover, CoordinateLine oldValue, CoordinateLine newValue)
        {
            //本次移动的范围
            int startIndex = bindChart.GetDateSourceIndex(oldValue.Start.X);
            int endIndex = bindChart.GetDateSourceIndex(newValue.Start.X);

            //向右移动
            int sign = 1;
            //向左移动
            if (startIndex > endIndex)
            {
                (endIndex, startIndex) = (startIndex, endIndex);
                sign = -1;  
            }
            //面积变化量
            double change = bindChart.GetArea(startIndex, endIndex) * sign;

            if (mover.Equals(this)) //移动的是本身
            {
                //更新面积
                Area += change;
                AreaRatio = Area / bindChart.SumArea;
                //当前是最后一个分割线时更新总面积及分割线的面积比例
                if (this.Equals(bindChart.SplitLines[^1]))
                {
                    bindChart.SumArea += change;
                    foreach (var line in bindChart.SplitLines)
                    {
                        line.AreaRatio = line.Area / bindChart.SumArea;
                    }
                }

                if (sign == -1)
                {
                    //向左移动时，若跨过RT值，则默认RT值为当前移动值，即默认递减
                    if (RTIndex > endIndex)
                    {
                        RTIndex = endIndex;
                    }
                }
                else
                {
                    //向右移动时，需先获取当前移动范围内的最大值，若大于RT值则更新RT值
                    var max = bindChart.DataSource[startIndex..endIndex].MaxBy(v => v.Y);
                    if (max.Y > bindChart.DataSource[RTIndex].Y)
                    {
                        RTIndex = bindChart.GetDateSourceIndex(max.X);
                    }
                }
            }
            else //处理移动对右边一条线的影响
            {
                //此时面积为减去变化量
                Area -= change;
                AreaRatio = Area / bindChart.SumArea;

                //RT值处理与本身相反
                if (sign == -1)
                {
                    var max = bindChart.DataSource[startIndex..endIndex].MaxBy(v => v.Y);
                    if (max.Y > bindChart.DataSource[RTIndex].Y)
                    {
                        RTIndex = bindChart.GetDateSourceIndex(max.X);
                    }
                }
                else
                {
                    if (RTIndex < endIndex)
                    {
                        RTIndex = endIndex;
                    }
                }
            }
        }

        public int CompareTo(SplitLine? other)
        {
            return Start.X.CompareTo(other!.Start.X);
        }

        /// <summary>
        /// 峰定义为该线与左边线的区间范围，若为第一个峰，则为该线与基线起始点的区间范围
        /// </summary>
        [ObservableProperty]
        private EditLineBase nextLine = null!;

        /// <summary>
        /// 设置左边的分割线（该线在最左边时为<see cref="BaseLine"/>）
        /// </summary>
        partial void OnNextLineChanged(EditLineBase? oldValue, EditLineBase newValue)
        {
            if (oldValue is null)//第一次设置时
            {
                //初始化面积和RT
                Area = bindChart.GetArea(this, newValue);
                int startIndex = bindChart.GetDateSourceIndex(newValue.Start.X);
                int endIndex = bindChart.GetDateSourceIndex(Start.X);
                RTIndex = bindChart.GetDateSourceIndex(bindChart.DataSource[startIndex..endIndex].MaxBy(v => v.Y).X);
            }
            //添加对下一条线移动的监听，以处理移动对本线的影响
            if (oldValue is SplitLine old)
                old.SplitLineMoved -= OnLineMoved;
            if (newValue is SplitLine newLine)
                newLine.SplitLineMoved += OnLineMoved;
        }

        public double Area { get; set; }

        public double AreaRatio { get; set; }

        public override string ToString()
        {
            return Start.X.ToString();
        }

        /// <summary>
        /// 设置本线的DP值
        /// <para>第一次设置时，若<paramref name="value"/>为整数且所有左边的线都没有设置DP值，则依次设置所有左边的线的DP值</para>
        /// <para>Todo: 修改DP时智能改动其他峰的DP</para>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TrySetDPIndex(string? value)
        {
            if (value is null)
            {
                DP = null;
                return true;
            }
            if (DP is null && int.TryParse(value, out int dp))
            {
                if (bindChart.SplitLines.Take(Index).All(v => v.DP is null))
                {
                    //获取该DP在聚合度表中的索引
                    int index = Array.IndexOf(DPTable, dp);
                    if (index >= 0)
                    {
                        for (int i = Index - 1; i >= 0; --i)
                        {
                            bindChart.SplitLines[i].DP = DPTable.ElementAtOrDefault(index).ToString();
                            ++index;
                        }
                        return true;
                    }
                }

            }
            DP = value;
            return true;
        }

        [Obsolete("智能设置DP值代开发")]
        private static void SetDPIndex(IList<SplitLine> sameLines, int index)
        {
            int dpValue = DPTable[index];
            if (sameLines.Count > 1)
            {
                for (int i = 0; i < sameLines.Count; i++)
                {
                    //sameLines[i].dpIndex = index;
                    sameLines[i].DP = $"{dpValue}-{i + 1}";
                }
            }
            else
            {
                //sameLines[0].dpIndex = index;
                sameLines[0].DP = dpValue.ToString();
            }
        }
    }

    /// <summary>
    /// 基线
    /// </summary>
    public partial class BaseLine(CoordinateLine line) : EditLineBase(line)
    {
        public BaseLine(Coordinates start, Coordinates end) : this(new CoordinateLine(start, end))
        {
        }

        /// <summary>
        /// 获取<paramref name="point"/>竖直于基线的线段
        /// </summary>
        public CoordinateLine CreateSplitLine(Coordinates point)
        {
            return new CoordinateLine(point.X, Line.Y(point.X), point.X, point.Y);
        }

        /// <summary>
        /// 获取指定X值在基线上的点
        /// </summary>
        public Coordinates CreateSplitLineStartPoint(double x)
        {
            return new Coordinates(x, Line.Y(x));
        }

        /// <summary>
        /// 获取指定X值在基线上的Y值
        /// </summary>
        public double GetY(double x)
        {
            return Line.Y(x);
        }
    }

    public readonly struct DraggedLineInfo(EditLineBase line, bool IsStart) : IEqualityComparer<DraggedLineInfo>, IEquatable<DraggedLineInfo>
    {
        public EditLineBase DraggedLine { get; init; } = line;

        public bool IsBaseLine { get; init; } = line is BaseLine;

        public bool IsStart { get; init; } = IsStart;

        public bool Equals(DraggedLineInfo x, DraggedLineInfo y)
        {
            return x.DraggedLine.Equals(y.DraggedLine);
        }

        public bool Equals(DraggedLineInfo other)
        {
            return object.Equals(DraggedLine, other.DraggedLine);
        }

        public int GetHashCode([DisallowNull] DraggedLineInfo obj)
        {
            return obj.DraggedLine.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is DraggedLineInfo info && Equals(info);
        }

        public static bool operator ==(DraggedLineInfo left, DraggedLineInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DraggedLineInfo left, DraggedLineInfo right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return DraggedLine.GetHashCode();
        }
    }

    public delegate void LineMovingEventHandler(EditLineBase line, CoordinateLine oldValue, CoordinateLine newValue);

}

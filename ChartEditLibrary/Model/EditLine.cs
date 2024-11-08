﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChartEditLibrary.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot;
using ScottPlot.Plottables;
using Version = System.Version;

namespace ChartEditLibrary.Model
{

    public abstract partial class EditLineBase(CoordinateLine line) : ObservableObject
    {
        public delegate void SplitLineMovedEventHandler(SplitLine mover, CoordinateLine oldValue, CoordinateLine newValue);
        public delegate void SplitLineMovingEventHandler(SplitLine line, CoordinateLine oldValue, CoordinateLine newValue);
        /// <summary>
        /// 分割线移动时（主要用于判断此次移动是否跨越其他线）
        /// </summary>
        public event SplitLineMovingEventHandler? SplitLineMoving;
        /// <summary>
        /// 分割线移动后（主要用于处理移动变换，及处理对该线右边一条线的影响）
        /// </summary>
        public event SplitLineMovedEventHandler? SplitLineMoved;


        public Coordinates Start
        {
            get => Line.Start;
            set => Line = new CoordinateLine(value, Line.End);
        }

        public Coordinates End
        {
            get => Line.End;
            set => Line = new CoordinateLine(Line.Start, value);
        }

        [ObservableProperty]
        private CoordinateLine line = line;

        /// <summary>
        /// 当前是否被选中
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 分割线移动时触发移动时事件
        /// </summary>
        partial void OnLineChanging(CoordinateLine oldValue, CoordinateLine newValue)
        {
            if (this is not SplitLine splitLine || Math.Abs(oldValue.Start.X - newValue.Start.X) < Utility.Tolerance)
                return;
            
            SplitLineMoving?.Invoke(splitLine, oldValue, newValue);
        }
        

        /// <summary>
        /// 分割线移动后触发移动后事件
        /// </summary>
        partial void OnLineChanged(CoordinateLine oldValue, CoordinateLine newValue)
        {
            if (this is not SplitLine splitLine || Math.Abs(oldValue.Start.X - newValue.Start.X) < Utility.Tolerance)
                return;
            
            // ReSharper disable once ExplicitCallerInfoArgument
            OnPropertyChanged("Item[]");
            SplitLineMoved?.Invoke(splitLine, oldValue, newValue);
        }

        
    }

    /// <summary>
    /// 分割线
    /// </summary>
    [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
    public partial class SplitLine(BaseLine baseLine, CoordinateLine line)
        : EditLineBase(line), IComparable<SplitLine>, IEnumerable<string?>
    {

        public BaseLine BaseLine { get; } = baseLine;

        public delegate void NextLineChangedEventHandler(SplitLine sender, EditLineBase? oldValue, EditLineBase newValue);
        public event NextLineChangedEventHandler? NextLineChanged;
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

        public double RT { get; set; }

        public string? DP { get; set; }

        /// <summary>
        /// 用于显示在DataGridView中的Columns
        /// </summary>
        /// <param name="index">列号</param>
        public string this[int index]
        {
            get
            {
                return index switch
                {
                    0 => Index.ToString(),
                    1 => RT.ToString("0.000"),    //RT
                    2 => Area.ToString("0.00"),  //面积
                    3 => NextLine.Start.X.ToString("0.000"),  //开始
                    4 => Start.X.ToString("0.000"),   //结束
                    5 => (AreaRatio * 100).ToString("0.00"),
                    6 => DP ?? "",
                    _ => "",
                };
            }
        }

        int IComparable<SplitLine>.CompareTo(SplitLine? other)
        {
            return other is null ? 0 : Start.X.CompareTo(other.Start.X);
        }

        /// <summary>
        /// 峰定义为该线与左边线的区间范围，若为第一个峰，则为该线与基线起始点的区间范围
        /// </summary>
        [ObservableProperty]
        private EditLineBase nextLine = null!;
        
        private double area;
        private double areaRatio;


        /// <summary>
        /// 设置左边的分割线（该线在最左边时为<see cref="BaseLine"/>）
        /// </summary>
        partial void OnNextLineChanged(EditLineBase? oldValue, EditLineBase newValue)
        {
            NextLineChanged?.Invoke(this, oldValue, newValue);
        }

        public double Area { get => area; set { area = value; UpdateUI(); } }

        public double AreaRatio { get => areaRatio; set { areaRatio = value; UpdateUI(); } }

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
        /// <param name="lines"></param>
        /// <returns></returns>
        public bool TrySetDPIndex(string? value, IList<SplitLine> lines)
        {
            if (value is null)
            {
                DP = null;
                return true;
            }
            if (DP is null && int.TryParse(value, out var dp))
            {
                if (lines.Take(Index).All(v => v.DP is null))
                {
                    //获取该DP在聚合度表中的索引
                    var index = Array.IndexOf(DPTable, dp);
                    if (index >= 0)
                    {
                        for (var i = Index - 1; i >= 0; --i)
                        {
                            lines[i].DP = DPTable.ElementAtOrDefault(index).ToString();
                            ++index;
                        }
                        return true;
                    }
                }

            }
            DP = value;
            UpdateUI();
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
                    sameLines[i].DP = $"{dpValue}-{i + 1}";
                }
            }
            else
            {
                //sameLines[0].dpIndex = index;
                sameLines[0].DP = dpValue.ToString();
            }
        }

        IEnumerator<string?> IEnumerable<string?>.GetEnumerator()
        {
            for (var i = 0; i <= 5; ++i)
                yield return this[i];
            yield return "DP" + DP;
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<string?>)this).GetEnumerator();

        /// <summary>
        /// 触发所有列的更新
        /// </summary>
        public void UpdateUI()
        {
            OnPropertyChanged("Item[]");
        }
    }

    /// <summary>
    /// 基线
    /// </summary>
    public class BaseLine(CoordinateLine line) : EditLineBase(line)
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

    /// <summary>
    /// 拖拽线信息
    /// </summary>
    /// <param name="line">线</param>
    /// <param name="isStart">当线是基线时，表示是基线的开始或结束</param>
    public readonly struct DraggedLineInfo(EditLineBase line, bool isStart) : IEqualityComparer<DraggedLineInfo>, IEquatable<DraggedLineInfo>
    {
        public EditLineBase DraggedLine { get; } = line;

        public bool IsBaseLine { get; } = line is BaseLine;

        public bool IsStart { get; } = isStart;

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

    

}

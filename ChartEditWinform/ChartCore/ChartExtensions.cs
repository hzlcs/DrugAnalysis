using ChartEditWinform.ChartCore.Entity;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWinform.ChartCore
{
    public static class ChartExtensions
    {
        private static readonly ConditionalWeakTable<EditLineBase, LinePlot> linePlotWeakTable = [];

        public static LinePlot AddEditLine(this FormsPlot chart, EditLineBase line)
        {
            var linePlot = chart.Plot.Add.Line(line.Line);
            linePlotWeakTable.TryAdd(line, linePlot);
            line.PropertyChanged += OnLineChanged;
            return linePlot;
        }

        public static void RemoveSplitLine(this FormsPlot chart, SplitLine line)
        {
            if (linePlotWeakTable.TryGetValue(line, out var linePlot))
                chart.Plot.Remove(linePlot);
        }

        private static void OnLineChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is null)
                return;
            EditLineBase line = (EditLineBase)sender;

            if (e.PropertyName != nameof(line.Line))
                return;
            if (!line.TryGetLinePlot(out var linePlot))
                return;
            linePlot!.Start = line.Start;
            linePlot!.End = line.End;
        }

        public static bool TryGetLinePlot(this EditLineBase line, out LinePlot? linePlot)
        {
            return linePlotWeakTable.TryGetValue(line, out linePlot);
        }

        public static Coordinates GetMarkPoint(this DraggedLineInfo info)
        {
            return info.IsStart ? info.DraggedLine.Start : info.DraggedLine.End;
        }



        public static int BinaryInsert<T>(this IList<T> list, T item, IComparer<T> comparer)
        {
            int l = 0;
            int r = list.Count - 1;
            while (r >= l)
            {
                int mid = (l + r) / 2;
                int com = comparer.Compare(item, list[mid]);
                if (com >= 0)
                    l = mid + 1;
                else
                    r = mid - 1;
            }
            list.Insert(l, item);
            return l;
        }
        public static int BinaryInsert<T>(this IList<T> list, T item) where T : IComparable<T>
        {
            return list.BinaryInsert(item, Comparer<T>.Create((l, r) => l.CompareTo(r)));
        }

        public static ScottPlot.Color ToScottColor(this System.Drawing.Color color)
        {
            return new ScottPlot.Color(color.R, color.G, color.B, color.A);
        }
    }
}

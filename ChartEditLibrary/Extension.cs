using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using ScottPlot;
using ScottPlot.Plottables;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ChartEditLibrary
{
    public static class Extension
    {
        private record BaseLineInfo(LinePlot LinePlot, DraggableChartVm Vm);

        private static readonly ConditionalWeakTable<EditLineBase, LinePlot> LinePlotWeakTable = [];
        private static readonly ConditionalWeakTable<BaseLine, BaseLineInfo> baseline_StartLineWeakTable = [];

        public static LinePlot AddBaseLine(this IPlotControl chart, BaseLine line, DraggableChartVm vm)
        {
            var linePlot = chart.Plot.Add.Line(line.Line);
            LinePlotWeakTable.AddOrUpdate(line, linePlot);
            line.PropertyChanged += OnLineChanged;

            var startLine = vm.GetChartPoint(line.Start.X);
            if (startLine.HasValue)
            {
                var baseLine_StartLine = chart.Plot.Add.Line(new CoordinateLine(line.Start, startLine.Value));
                baseLine_StartLine.LineStyle.Pattern = LinePattern.Dotted;
                baseline_StartLineWeakTable.AddOrUpdate(line, new BaseLineInfo(baseLine_StartLine, vm));
            }
            return linePlot;
        }

        public static LinePlot AddSplitLine(this IPlotControl chart, SplitLine line)
        {
            var linePlot = chart.Plot.Add.Line(line.Line);
            LinePlotWeakTable.AddOrUpdate(line, linePlot);
            line.PropertyChanged += OnLineChanged;
            return linePlot;
        }

        public static void RemoveEditLine(this IPlotControl chart, EditLineBase line)
        {
            if (LinePlotWeakTable.TryGetValue(line, out var linePlot))
                chart.Plot.Remove(linePlot);
            if (line is BaseLine baseLine && baseline_StartLineWeakTable.TryGetValue(baseLine, out var baseLine_StartLine))
                chart.Plot.Remove(baseLine_StartLine.LinePlot);
        }

        private static void OnLineChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not EditLineBase line)
                return;

            if (e.PropertyName != nameof(line.Line))
                return;
            if (!line.TryGetLinePlot(out var linePlot))
                return;
            linePlot.End = line.End;
            if(linePlot.Start != line.Start)
            {
                linePlot.Start = line.Start;
                if (line is BaseLine baseLine && baseline_StartLineWeakTable.TryGetValue(baseLine, out var baseLine_StartLine))
                {
                    baseLine_StartLine.LinePlot.Start = line.Start;
                    var end = baseLine_StartLine.Vm.GetChartPoint(baseLine.Start.X)!;
                    baseLine_StartLine.LinePlot.End = end.Value;
                }
            }
        }

        private static bool TryGetLinePlot(this EditLineBase line, [NotNullWhen(true)] out LinePlot? linePlot)
        {
            return LinePlotWeakTable.TryGetValue(line, out linePlot);
        }

        public static Coordinates GetMarkPoint(this DraggedLineInfo info)
        {
            return info.IsStart ? info.DraggedLine.Start : info.DraggedLine.End;
        }



        private static int BinaryInsert<T>(this IList<T> list, T item, IComparer<T> comparer)
        {
            var l = 0;
            var r = list.Count - 1;
            while (r >= l)
            {
                var mid = (l + r) / 2;
                var com = comparer.Compare(item, list[mid]);
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

        public static Color ToScottColor(this System.Drawing.Color color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }
    }
}

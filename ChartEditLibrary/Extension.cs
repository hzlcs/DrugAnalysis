using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using ScottPlot;
using ScottPlot.Plottables;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ChartEditLibrary
{
    public static class Extension
    {
        private record BaseLineInfo(LinePlot LinePlot, DraggableChartVm Vm);

        private static readonly ConditionalWeakTable<EditLineBase, LinePlot> LinePlotWeakTable = [];
        private static readonly ConditionalWeakTable<SplitLine, SplitLineLabelInfo> lineMark = [];
        private static readonly ConditionalWeakTable<BaseLine, BaseLineInfo> baseline_StartLineWeakTable = [];

        public static LinePlot AddBaseLine(this IPlotControl chart, BaseLine line, DraggableChartVm vm)
        {
            var linePlot = chart.Plot.Add.Line(line.Line);
            linePlot.LineWidth = 1.5f;
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
            linePlot.LineWidth = 1.5f;
            LinePlotWeakTable.AddOrUpdate(line, linePlot);
            var mark = chart.Plot.Add.Text(line.Description ?? "", line.End);
            mark.IsVisible = false;
            lineMark.AddOrUpdate(line, new SplitLineLabelInfo(mark, false));
            line.PropertyChanged += OnLineChanged;
            return linePlot;
        }

        public static void RemoveEditLine(this IPlotControl chart, EditLineBase line)
        {
            line.PropertyChanged -= OnLineChanged;
            if (LinePlotWeakTable.TryGetValue(line, out var linePlot))
                chart.Plot.Remove(linePlot);
            if (line is BaseLine baseLine && baseline_StartLineWeakTable.TryGetValue(baseLine, out var baseLine_StartLine))
                chart.Plot.Remove(baseLine_StartLine.LinePlot);
            if (line is SplitLine sl && lineMark.TryGetValue(sl, out var mark))
                chart.Plot.Remove(mark.Text);
        }

        private static void OnLineChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not EditLineBase line)
                return;

            if (e.PropertyName == nameof(line.Line))
            {
                if (line.TryGetLinePlot(out var linePlot))
                {
                    linePlot.End = line.End;
                    if (linePlot.Start != line.Start)
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
            }

            if (line is SplitLine sl && e.PropertyName == nameof(sl.Description))
            {
                if (lineMark.TryGetValue(sl, out var mark))
                    mark.Text.LabelText = sl.Description ?? "";
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

        private static SplitLine? currentMark;
        public static void ShowMark(this SplitLine line, Coordinates mouse, bool always = false)
        {
            if (!lineMark.TryGetValue(line, out var markInfo))
                return;
            if (always)
                markInfo.Always = true;
            if (markInfo.Always && !always)
                return;
            if (currentMark == line)
            {
                markInfo.Text.Location = new Coordinates(mouse.X, mouse.Y);
                return;
            }
            if (!always && currentMark is not null && lineMark.TryGetValue(currentMark, out var mark))
                mark.Text.IsVisible = false;
            currentMark = line;
            if (lineMark.TryGetValue(line, out mark))
            {
                mark.Text.Location = new Coordinates(mouse.X, mouse.Y);
                mark.Text.IsVisible = true;
            }
        }
        public static void HideMark(this SplitLine line, bool always = false)
        {
            if (!lineMark.TryGetValue(line, out var markInfo))
                return;
            if (always)
                markInfo.Always = false;
            else if (markInfo.Always)
                return;
            markInfo.Text.IsVisible = false;
            currentMark = null;
        }
        public static void HideMark()
        {
            if(currentMark != null && lineMark.TryGetValue(currentMark, out var mark) && !mark.Always)
                mark.Text.IsVisible = false;
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

        public static int FirstIndex<T>(this IEnumerable<T> values, Func<T, bool> predicate)
        {
            var index = 0;
            foreach (var value in values)
            {
                if (predicate(value))
                    return index;
                index++;
            }
            throw new InvalidOperationException("No element found");
        }

        public static int FirstOrDefaultIndex<T>(this IEnumerable<T> values, Func<T, bool> predicate)
        {
            var index = 0;
            foreach (var value in values)
            {
                if (predicate(value))
                    return index;
                index++;
            }
            return -1;
        }

        public static Color ToScottColor(this System.Drawing.Color color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }

        class SplitLineLabelInfo(Text text, bool always)
        {
            public Text Text { get; } = text;
            public bool Always { get; set; } = always;
        }
    }
}

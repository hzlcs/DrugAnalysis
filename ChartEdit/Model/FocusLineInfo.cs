using ScottPlot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.Model
{
    public readonly struct DraggedLineInfo(EditLine line, bool IsStart)
    {
        public EditLine DraggedLine { get; init; } = line;

        public bool IsBaseLine { get; init; } = line is EditBaseLine;

        public bool IsStart { get; init; } = IsStart;

        public Coordinates Mark => IsStart ? DraggedLine.Start : DraggedLine.End;

        public readonly void SetMovePoint(Coordinates? chartPoint, Coordinates mouseLocation, Func<double, double> BaseLineFunc, out Coordinates? mark)
        {
            mark = null;
            if (IsBaseLine)
            {
                chartPoint ??= mouseLocation;
                if (IsStart)
                {
                    if (DraggedLine.Start.Equals(chartPoint.Value))
                        return;
                    DraggedLine.Start = chartPoint.Value;
                    mark = DraggedLine.Start;
                }
                else
                {
                    if (DraggedLine.End.Equals(chartPoint.Value))
                        return;
                    DraggedLine.End = chartPoint.Value;
                    mark = DraggedLine.End;
                }
            }
            else
            {
                if (chartPoint.HasValue)
                {
                    if (chartPoint.Value.X == DraggedLine.Start.X)
                        return;
                    var value = chartPoint.Value;
                    var coordinateLine = new CoordinateLine(value.X, BaseLineFunc(value.X), value.X, value.Y);
                    DraggedLine.Start = coordinateLine.Start;
                    DraggedLine.End = coordinateLine.End;
                    mark = coordinateLine.End;
                }
            }
        }


    }
}

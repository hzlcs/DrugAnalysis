using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot;
using ScottPlot.Plottables;

namespace ChartEditWinform.ChartCore.Entity
{
    public abstract partial class EditLineBase : ObservableObject
    {

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

        public bool IsSelected { get; set; }

        public EditLineBase(CoordinateLine line)
        {
            this.line = line;
        }

        public EditLineBase(Coordinates start, Coordinates end)
        {
            line = new CoordinateLine(start, end);
        }

    }

    public partial class SplitLine(CoordinateLine line, DraggableChartVM bindChart) : EditLineBase(line), IComparable<SplitLine>
    {
        public SplitLine(Coordinates start, Coordinates end) : this(new CoordinateLine(start, end))
        {
        }

        public int CompareTo(SplitLine? other)
        {
            return Start.X.CompareTo(other!.Start.X);
        }

        public EditLineBase? NextLine { get; set; } 

    }

    public partial class BaseLine(CoordinateLine line) : EditLineBase(line)
    {
        public BaseLine(Coordinates start, Coordinates end) : this(new CoordinateLine(start, end))
        {
        }

        public CoordinateLine CreateSplitLine(Coordinates point)
        {
            return new CoordinateLine(point.X, Line.Y(point.X), point.X, point.Y);
        }

        public Coordinates CreateSplitLinePoint(double x)
        {
            return new Coordinates(x, Line.Y(x));
        }
    }

    public readonly struct DraggedLineInfo(EditLineBase line, bool IsStart)
    {
        public EditLineBase DraggedLine { get; init; } = line;

        public bool IsBaseLine { get; init; } = line is BaseLine;

        public bool IsStart { get; init; } = IsStart;

    }

}

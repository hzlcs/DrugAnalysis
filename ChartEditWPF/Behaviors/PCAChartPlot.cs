using ChartEditLibrary;
using ChartEditLibrary.Entitys;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using SkiaSharp;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using static ChartEditLibrary.Model.PCAManager;
using Rectangle = ScottPlot.Plottables.Rectangle;

namespace ChartEditWPF.Behaviors
{
    internal class PCAChartPlot : ScottPlot.WPF.WpfPlot
    {
        public double[] EigenVectors { get; set; } = null!;
        public double[] SingularValues { get; set; } = null!;

        public SamplePCA[] Samples
        {
            set { SampleChanged(value); }
        }

        private IPlotControl PlotControl => Plot.PlotControl!;

        public PCAChartPlot()
        {
            Plot.HideGrid();
            Plot.Legend.IsVisible = true;
            Plot.Font.Automatic();
            PlotControl.Menu!.Add("Auto Scale", AutoScale);
#if DEBUG
            Plot.PlotControl!.Menu!.Add("test", TestMethod);
#endif
        }

        private void AutoScale(Plot plot)
        {
            if (plot.Axes.Left.Max == yMax && plot.Axes.Left.Min == -yMax
                && plot.Axes.Bottom.Max == xMax && plot.Axes.Bottom.Min == -xMax)
            {
                return;
            }
            plot.Axes.SetLimits(-xMax, xMax, -yMax, yMax);
            foreach (var item in labels)
            {
                item.UpdateLine();
            }
            PlotControl.Refresh();
        }

        private void TestMethod(Plot plot)
        {
            SmartLabelHelper.SmartLabel(this, labels);
        }

        private SmartLabelHelper.Item? draggedItem;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.LeftButton != MouseButtonState.Pressed)
                return;
            var point = e.GetPosition(this);
            var mousePoint = Plot.GetCoordinates(new Pixel((float)point.X * DisplayScale, (float)point.Y * DisplayScale));
            double perX = Plot.LastRender.UnitsPerPxX;
            double perY = Plot.LastRender.UnitsPerPxY;
            foreach (var item in labels)
            {
                double xOffset = (mousePoint.X - item.Location.X) / perX;
                double yOffset = -(mousePoint.Y - item.Location.Y) / perY;
                if (Math.Abs(xOffset - item.Text.OffsetX) < item.halfTextWidth
                    && Math.Abs(yOffset - item.Text.OffsetY) < item.halfTextHeight)
                {
                    draggedItem = item;
                    item.Init();
                    PlotControl.UserInputProcessor.Disable();
                    break;
                }
            }

        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (draggedItem is null)
                return;
            var point = e.GetPosition(this);
            var mousePoint = Plot.GetCoordinates(new Pixel((float)point.X * DisplayScale, (float)point.Y * DisplayScale));
            draggedItem.MoveText(mousePoint);
            PlotControl.Refresh();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.LeftButton != MouseButtonState.Released)
                return;
            if (draggedItem is not null)
            {
                draggedItem = null;
                PlotControl.UserInputProcessor.Enable();
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            foreach (var item in labels)
            {
                item.UpdateLine();
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            foreach (var item in labels)
            {
                item.UpdateLine();
            }
        }

        int xMax;
        int yMax;
        SmartLabelHelper.Item[] labels = null!;
        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            Debug.WriteLine("OnRender");
            base.OnRender(drawingContext);
            SmartLabelHelper.SmartLabel(this, labels);
        }
        private void SampleChanged(SamplePCA[] samples)
        {
            Plot.Clear();
            var plot = Plot;
            xMax = (int)Math.Ceiling(SingularValues[0]) + 1;
            yMax = (int)Math.Ceiling(SingularValues[1]) + 1;
            Color lineColor = Colors.DarkGray;
            plot.Axes.SetLimits(-xMax, xMax, -yMax, yMax);
            plot.Axes.Bottom.TickLabelStyle.FontSize = 24;
            plot.Axes.Left.TickLabelStyle.FontSize = 24;
            plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(1);
            plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(1);
            //x
            var xLine = plot.Add.Line(-xMax, 0, xMax, 0);
            xLine.Color = lineColor;
            xLine.LineWidth = 2;
            //y
            var yLine = plot.Add.Line(0, -yMax, 0, yMax);
            yLine.Color = lineColor;
            yLine.LineWidth = 2;
            ScottPlot.Palettes.Category10 palette = new();
            List<SmartLabelHelper.Item> labels = [];
            var index = 0;
            foreach (var sample in samples)
            {
                plot.Legend.ManualItems.Add(new ScottPlot.LegendItem() { LabelText = sample.ClassName, FillColor = palette.GetColor(index) });
                for (var i = 0; i < sample.Points.Length; ++i)
                {
                    var mark = plot.Add.Marker(sample.Points[i].X, sample.Points[i].Y, color: palette.GetColor(index));
                    mark.MarkerSize = (float)CommonConfig.Instance.PCAMarkerSize;

                    var txt = plot.Add.Text(sample.SampleNames[i], sample.Points[i].X, sample.Points[i].Y);
                    txt.LabelAlignment = Alignment.MiddleCenter;
                    txt.LabelFontSize = CommonConfig.Instance.PCALableFontSize;
                    var line = plot.Add.Line(default);
                    line.IsVisible = false;
                    line.LineColor = ScottPlot.Colors.Black;
                    labels.Add(new SmartLabelHelper.Item(mark, txt, line));
                }
                ++index;
            }
            this.labels = labels.ToArray();

            var ellips = plot.Add.Ellipse(0, 0, SingularValues[0], SingularValues[1]);
            ellips.LineColor = lineColor;
            ellips.LineWidth = 2;
            plot.Legend.FontSize = 24;
            ++xMax;
            ++yMax;
            string text = $"R2x[1] = {EigenVectors[0]:F3}    R2x[2] = {EigenVectors[1]:F3}    Ellipse: Hotelling's T2 (95%)";
            var label = plot.Add.Text(text, 0, -yMax + 0.5);
            label.LabelFontSize = 36;
            label.Alignment = Alignment.MiddleCenter;
            label.LabelAlignment = Alignment.MiddleCenter;

        }


        static class SmartLabelHelper
        {
            public class Item
            {
                public Marker Marker { get; }
                public Text Text { get; private set; }
                public LinePlot Line { get; private set; }
                public Coordinates Location { get; }
                public Pixel Pixel { get; private set; }

                private SKRect textRect;
                public SKRect TextRect
                {
                    get => textRect;
                    set
                    {
                        textRect = value;
                        Text.OffsetX = value.MidX - Pixel.X;
                        Text.OffsetY = value.MidY - Pixel.Y;
                        TextCoordinateRect = GetCoordinateRect(TextRect);
                        Combine = SKRect.Union(MarkerRect, new SKRect(value.Left, value.Top + 2, value.Right, value.Bottom - 2));
                    }
                }
                public SKRect MarkerRect { get; private set; }
                public SKRect Combine { get; private set; }
                public CoordinateRect TextCoordinateRect { get; private set; }
                //public CoordinateRect MarkerCoordinateRect { get; private set; }
                public bool seted = false;

                public readonly float textHeight;
                public readonly float textWidth;
                public readonly float halfTextHeight;
                public readonly float halfTextWidth;
                public readonly float halfSize;

                public Item(Marker marker, Text text, LinePlot line)
                {
                    Marker = marker;
                    Text = text;
                    Line = line;
                    Location = marker.Location;

                    halfSize = Marker.MarkerSize / 2;
                    var size = Text.LabelStyle.Measure();
                    textHeight = size.Height;
                    textWidth = size.Width;
                    halfTextHeight = size.Height / 2;
                    halfTextWidth = size.Width / 2;
                    Text.OffsetX = halfSize + halfTextWidth;
                    Line.LineColor = Marker.MarkerFillColor;
                }

                public void Init()
                {
                    Pixel = Marker.Axes.GetPixel(Marker.Location);
                    MarkerRect = SKRect.Create(Pixel.X - halfSize, Pixel.Y - halfSize, Marker.MarkerSize, Marker.MarkerSize);
                    TextRect = SKRect.Create(Pixel.X + Text.OffsetX - halfTextWidth, Pixel.Y - halfTextHeight + Text.OffsetY,
                        textWidth, textHeight);
                }

                private CoordinateRect GetCoordinateRect(SKRect rect)
                {
                    var start = GetCoordinates(rect.Left, rect.Top);
                    var end = GetCoordinates(rect.Right, rect.Bottom);
                    return new CoordinateRect(start, end);
                }

                private Coordinates GetCoordinates(float x, float y) => Marker.Axes.GetCoordinates(new Pixel(x, y));

                public override string ToString()
                {
                    return Marker.Location.ToString();
                }

                public void MoveText(float angle, float offsetLength = 1)
                {
                    var distance = MathF.Sqrt(MathF.Pow(Text.OffsetX, 2) + MathF.Pow(Text.OffsetY, 2));
                    var radian = angle * Math.PI / 180;
                    var newX = Pixel.X + distance * (float)Math.Cos(radian) * offsetLength - halfTextWidth;
                    var newY = Pixel.Y + distance * (float)Math.Sin(radian) * offsetLength - halfTextHeight;
                    TextRect = SKRect.Create(new SKPoint(newX, newY), TextRect.Size);
                    UpdateMove();
                }

                public void MoveText(Coordinates point)
                {
                    var rect = Marker.Axes.GetPixel(point);
                    TextRect = SKRect.Create(rect.X - halfTextWidth, rect.Y - halfTextHeight, TextRect.Width, TextRect.Height);
                    UpdateMove();
                }

                public void MoveText(bool? top, bool? left)
                {
                    var vertical = top.HasValue ? (top.Value ? -1 : 1) : 0;
                    var horizontal = left.HasValue ? (left.Value ? -1 : 1) : 0;
                    var x = Pixel.X + horizontal * (halfSize + halfTextWidth);
                    var y = Pixel.Y + vertical * (halfSize + halfTextHeight);
                    TextRect = SKRect.Create(x - halfTextWidth, y - halfTextHeight, TextRect.Width, TextRect.Height);
                }



                private MathPoint GetTextRectSidePoint()
                {
                    MathPoint target = new(Text.OffsetX, -Text.OffsetY);
                    var xSign = Math.Sign(target.X);
                    var ySign = Math.Sign(target.Y);
                    var textAngleX = target.X - xSign * halfTextWidth;
                    var textAngleY = target.Y - ySign * halfTextHeight;
                    if (target.X == 0)
                        return new MathPoint(0, textAngleY);
                    if (target.Y == 0)
                        return new MathPoint(textAngleX, 0);
                    if (Math.Sign(textAngleY) != ySign)
                        textAngleY = 0;
                    if (Math.Sign(textAngleX) != xSign)
                        textAngleX = 0;
                    float slope = -Text.OffsetY / Text.OffsetX;

                    var sideX = textAngleY / slope;
                    if (Math.Sign(sideX) != xSign || Math.Abs(sideX) < Math.Abs(textAngleX))
                        sideX = textAngleX;
                    var sideY = textAngleX * slope;
                    if (Math.Sign(sideY) != ySign || Math.Abs(sideY) < Math.Abs(textAngleY))
                        sideY = textAngleY;
                    return new MathPoint(sideX, sideY);
                }

                private void UpdateMove()
                {
                    var textRectSidePoint = GetTextRectSidePoint();
                    var distance = MathF.Sqrt(MathF.Pow(textRectSidePoint.X, 2) + MathF.Pow(textRectSidePoint.Y, 2)) - halfSize;
                    if (distance < 10)
                    {
                        Line.IsVisible = false;
                    }
                    else
                    {
                        var x = Pixel.X + textRectSidePoint.X;
                        var y = Pixel.Y - textRectSidePoint.Y;
                        Line.Line = new CoordinateLine(Location, GetCoordinates(x, y));
                        Line.IsVisible = true;
                    }
                }

                public void UpdateLine()
                {
                    if (!Line.IsVisible)
                        return;
                    var textRectSidePoint = GetTextRectSidePoint();
                    var pixel = Marker.Axes.GetPixel(Marker.Location);
                    Line.End = GetCoordinates(pixel.X + textRectSidePoint.X, pixel.Y - textRectSidePoint.Y);
                }

                

                public readonly struct MathLine
                {
                    public readonly float X1;
                    public readonly float X2;
                    public readonly float Y1;
                    public readonly float Y2;
                    public float XSpan => X2 - X1;
                    public float YSpan => Y2 - Y1;
                    public float Slope => (X1 == X2) ? float.NaN : YSpan / XSpan;
                    public float SlopeRadians => MathF.Atan(Slope);
                    public float SlopeDegrees => SlopeRadians * 180 / MathF.PI;
                    public float YIntercept => Y1 - Slope * X1;
                    public float Length => (float)MathF.Sqrt(XSpan * XSpan + YSpan * YSpan);

                    public MathPoint Start => new(X1, Y1);
                    public MathPoint End => new(X2, Y2);
                    public MathPoint Center => new((X1 + X2) / 2, (Y1 + Y2) / 2);

                    public MathLine(float x1, float y1, float x2, float y2)
                    {
                        X1 = x1;
                        Y1 = y1;
                        X2 = x2;
                        Y2 = y2;
                    }

                    public MathLine(MathPoint pt1, MathPoint pt2)
                    {
                        X1 = pt1.X;
                        Y1 = pt1.Y;
                        X2 = pt2.X;
                        Y2 = pt2.Y;
                    }

                    public MathLine(float x, float y, float slope)
                    {
                        X1 = x;
                        Y1 = y;
                        X2 = x + 1;
                        Y2 = y + slope;
                    }

                    public MathLine(MathPoint point, float slope)
                    {
                        X1 = point.X;
                        Y1 = point.Y;
                        X2 = point.X + 1;
                        Y2 = point.Y + slope;
                    }

                    public override string ToString()
                    {
                        return $"CoordinateLine from ({X1}, {Y1}) to ({X2}, {Y2})";
                    }

                    /// <summary>
                    /// Return the X position on the line at the given Y
                    /// </summary>
                    public float X(float y = 0)
                    {
                        float dX = Y1 - y;
                        float x = X1 - dX * Slope;
                        return x;
                    }

                    /// <summary>
                    /// Return the Y position on the line at the given X
                    /// </summary>
                    public float Y(float x = 0)
                    {
                        float y = Slope * x + YIntercept;
                        return y;
                    }

                    public MathLine WithDelta(float dX, float dY)
                    {
                        return new MathLine(X1 + dX, Y1 + dY, X2 + dX, Y2 + dY);
                    }

                    public MathLine Reversed()
                    {
                        return new MathLine(X2, Y2, X1, Y1);
                    }
                }

                public readonly struct MathPoint(float x, float y)
                {
                    public readonly float X = x;
                    public readonly float Y = y;
                }
            }

            public static void SmartLabel(WpfPlot chart, Item[] items)
            {
                foreach (var item in items)
                {
                    item.Init();
                }
                List<SKRect> rects = [];
                List<GroupItem> temp = items.Select(v => new GroupItem(0, v)).ToList();
                int index = 0;
                for (int i = 0; i < temp.Count; i++)
                {
                    var cur = temp[i];
                    for (int j = i + 1; j < temp.Count; ++j)
                    {
                        var other = temp[j];
                        if (!cur.item.Combine.IntersectsWith(other.item.Combine))
                            continue;
                        if (other.index > 0)
                            cur.index = other.index;
                        else if (cur.index > 0)
                            other.index = cur.index;
                        else
                            other.index = cur.index = ++index;
                    }
                }

                var group = temp.Where(v => v.index > 0).GroupBy(v => v.index);
                foreach(var kv in group)
                {
                    var values = kv.Select(v => v.item).ToList();
                    var top = values.MinBy(v => v.Combine.Top)!;
                    var bottom = values.MaxBy(v => v.Combine.Bottom)!;
                    var right = values.MaxBy(v => v.Combine.Right)!;
                    top.MoveText(true, null);
                    bottom.MoveText(false, null);
                    if(right != top && right != bottom)
                        right.MoveText(null, false);
                }

                chart.Refresh();
            }

            private static bool Overlap(Item item, Item[] items)
            {
                foreach (var i in items)
                {
                    if (i == item)
                        continue;
                    if (item.MarkerRect.IntersectsWith(i.Combine))
                        return true;
                }
                return false;
            }

            private static void MatchItem(Item[] items, int index, int xSign, int ySign)
            {

            }

            [DebuggerDisplay("{index}:{item.Location}")]
            class GroupItem(int index, Item item)
            {
                public int index = index;
                public Item item = item;
            }




        }
    }

}

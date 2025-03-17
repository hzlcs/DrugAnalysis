using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using OpenTK.Mathematics;
using ScottPlot;
using ScottPlot.Plottables;
using System.Collections.Specialized;

namespace ChartEditLibrary.Interfaces
{
    public interface IChartControl
    {
        event Action<IChartControl>? ChartAreaChanged;

        IPlotControl PlotControl { get; }

        System.Drawing.Color? SerialColor { get; set; }

        DraggableChartVm ChartData { get; set; }

        void MouseDown(Coordinates mousePoing, bool left);

        void MouseMove(Coordinates mousePoing);

        void MouseUp();

        void KeyDown(string key);

        void AutoFit();

        byte[] GetImage();

        void BindControl(IPlotControl chartPlot);

        void ChangeActived(bool actived);

        void UpdateChartArea(IChartControl chartControl);

        void OnFocusChanged(bool focused);
    }

    public abstract class ChartControl(IMessageBox messageBox, IFileDialog dialog, IInputForm inputForm) : IChartControl
    {
        private bool focused;
        public IPlotControl PlotControl { get; set; } = null!;
        public DraggableChartVm ChartData { get; set; } = null!;
        private System.Drawing.Color? serialColor;
        public System.Drawing.Color? SerialColor
        {
            get => serialColor;
            set
            {

            }
        }

        protected bool mouseDown;
        protected Text? MyHighlightText;
        protected Marker? vstreetMarker;
        protected Coordinates mouseCoordinates;
        protected DraggedLineInfo? draggedLine;
        protected Vector2d sensitivity;
        protected readonly IMessageBox _messageBox = messageBox;
        protected readonly IFileDialog _dialog = dialog;
        protected readonly IInputForm _inputForm = inputForm;


        public event Action<IChartControl>? ChartAreaChanged;

        protected Vector2d GetSensitivity()
        {
            sensitivity = new Vector2d(PlotControl.Plot.Axes.Bottom.Width / 50, PlotControl.Plot.Axes.Left.Height / 50);
            return sensitivity;
        }

        public void AutoFit()
        {
            PlotControl.Plot.Axes.AutoScale();
            PlotControl.Refresh();
        }

        public virtual void BindControl(IPlotControl chartPlot)
        {
            PlotControl = chartPlot;

            chartPlot.Plot.Clear();
            chartPlot.Plot.XLabel(ChartData.FileName);
            MyHighlightText = chartPlot.Plot.Add.Text("", 0, 0);
            MyHighlightText.IsVisible = false;
            vstreetMarker = chartPlot.Plot.Add.Marker(0, 0, MarkerShape.FilledCircle, 3.5f, 
                Color.FromColor(System.Drawing.Color.Red));
            vstreetMarker.IsVisible = false;
            GetSensitivity();
            var source = chartPlot.Plot.Add.ScatterPoints(ChartData.DataSource);
            source.Color = ScottPlot.Color.FromColor(System.Drawing.Color.DodgerBlue);
            source.MarkerSize = 2;
            foreach (var baseLine in ChartData.BaseLines)
            {
                chartPlot.AddBaseLine(baseLine, ChartData);
            }
            ChartData.BaseLines.CollectionChanged += BaseLines_CollectionChanged;
            foreach (var i in ChartData.SplitLines)
            {
                chartPlot.AddSplitLine(i);
            }
            ChartData.SplitLines.CollectionChanged += VerticalLines_CollectionChanged;
            if(ChartData.CuttingLines is not null)
            {
                foreach(var l in ChartData.CuttingLines)
                {
                    chartPlot.Plot.Add.Line(l);
                }
            }
            var plot = chartPlot.Plot;
            plot.FigureBackground.Color = System.Drawing.Color.White.ToScottColor();
            plot.Grid.IsVisible = false;
            chartPlot.Plot.Axes.AutoScale();
            chartPlot.Refresh();
            chartPlot.Menu.Clear();

        }

        private void BaseLines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (BaseLine item in e.NewItems!)
                {
                    PlotControl.AddBaseLine(item, ChartData);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (BaseLine item in e.OldItems!)
                {
                    PlotControl.RemoveEditLine(item);
                }
            }
        }

        private void VerticalLines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (SplitLine item in e.NewItems!)
                {
                    PlotControl.AddSplitLine(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (SplitLine item in e.OldItems!)
                {
                    PlotControl.RemoveEditLine(item);
                }
            }
        }

        public byte[] GetImage()
        {
            PixelSize size = new(1920, 1080);
            return PlotControl.Plot.GetImageBytes((int)size.Width, (int)size.Height, ImageFormat.Png);
        }

        public void KeyDown(string key)
        {
            Coordinates? markPoint = null;
            if (key == "A")
            {
                if (!ChartData.DraggedLine.HasValue || ChartData.DraggedLine.Value.DraggedLine is not SplitLine sl)
                    return;
                var point = ChartData.GetChartPoint(sl.Start.X - ChartData.Unit);
                if (point.HasValue && point.Value.X >= sl.BaseLine.Start.X)
                {
                    ChartData.DraggedLine.Value.DraggedLine.Line = sl.BaseLine.CreateSplitLine(point.Value);
                    markPoint = point;
                }
            }
            else if (key == "D")
            {
                if (!ChartData.DraggedLine.HasValue || ChartData.DraggedLine.Value.DraggedLine is not SplitLine sl)
                    return;
                var point = ChartData.GetChartPoint(sl.Start.X + ChartData.Unit);
                if (point.HasValue && point.Value.X < sl.BaseLine.End.X)
                {
                    ChartData.DraggedLine.Value.DraggedLine.Line = sl.BaseLine.CreateSplitLine(point.Value);
                    markPoint = point;
                }
            }
            else
            {
                return;
            }
            if (markPoint.HasValue && MyHighlightText is not null)
            {
                MyHighlightText.IsVisible = true;
                var value = markPoint.Value;
                MyHighlightText.Location = value;
                MyHighlightText.LabelText = $"({value.X: 0.000}, {value.Y: 0.000})";
            }
            PlotControl.Refresh();
        }

        public virtual void MouseDown(Coordinates mousePoint, bool left)
        {
            ArgumentNullException.ThrowIfNull(ChartData);
            mouseDown = true;
            mouseCoordinates = mousePoint;
            ChartData.Sensitivity = GetSensitivity();
            if (!left)
                return;
            draggedLine = ChartData.GetDraggedLine(mouseCoordinates);

            if (draggedLine is not null)
            {
                if (MyHighlightText is not null)
                {
                    var value = draggedLine.Value.GetMarkPoint();
                    MyHighlightText.IsVisible = true;
                    MyHighlightText.Location = value;
                    MyHighlightText.LabelText = $"({value.X: 0.000}, {value.Y: 0.000})";
                    PlotControl.Refresh();
                }
                if (vstreetMarker is not null)
                {
                    var value = draggedLine.Value.GetMarkPoint();
                    vstreetMarker.Location = ChartData.GetVstreetPoint(value.X);
                    vstreetMarker.IsVisible = true;
                    PlotControl.Refresh();
                }

                PlotControl.Interaction.Disable();
            }

        }

        public virtual void MouseMove(Coordinates mousePoint)
        {
            ArgumentNullException.ThrowIfNull(ChartData);
            if (!mouseDown)
            {
                var index = ChartData.GetDateSourceIndex(mousePoint.X);
                SplitLine? sl = null;
                if (draggedLine is not null && draggedLine.Value.DraggedLine is SplitLine s)
                {
                    sl = s;
                }
                else
                {
                    if (index > 0)
                        sl = ChartData.SplitLines.FirstOrDefault(v => v.Start.X > mousePoint.X);
                }

                if (sl is not null)
                {
                    var baseline = sl.BaseLine;
                    if (baseline.Line.Y(mousePoint.X) < mousePoint.Y && ChartData.DataSource[index].Y > mousePoint.Y)
                        sl.ShowMark(new Coordinates(mousePoint.X, mousePoint.Y + sensitivity.Y * 2));
                    else
                        sl.HideMark();

                }
                else
                {
                    Extension.HideMark();
                }
            }
            else
            {
                if (draggedLine is not null)
                {
                    var markPoint = draggedLine.Value.GetMarkPoint();
                    if (MyHighlightText is not null)
                    {
                        MyHighlightText.Location = markPoint;
                        MyHighlightText.LabelText = $"({markPoint.X: 0.000}, {markPoint.Y: 0.000})";
                    }
                    if (vstreetMarker is not null)
                    {
                        var location = ChartData.GetVstreetPoint(markPoint.X);
                        if (vstreetMarker.Location != location)
                        {
                            vstreetMarker.Location = location;
                        }
                    }
                }
                Extension.HideMark();
            }

            PlotControl.Refresh();
        }

        public virtual void MouseUp()
        {
            ArgumentNullException.ThrowIfNull(ChartData);
            mouseDown = false;
            if (MyHighlightText is not null)
                MyHighlightText.IsVisible = false;
            if (vstreetMarker is not null)
                vstreetMarker.IsVisible = false;
            PlotControl.Interaction.Enable();
        }

        public virtual void ChangeActived(bool actived)
        {

        }

        public void UpdateChartArea(IChartControl chartControl)
        {
            PlotControl.Plot.Axes.Left.Min = chartControl.PlotControl.Plot.Axes.Left.Min;
            PlotControl.Plot.Axes.Left.Max = chartControl.PlotControl.Plot.Axes.Left.Max;
            PlotControl.Plot.Axes.Bottom.Min = chartControl.PlotControl.Plot.Axes.Bottom.Min;
            PlotControl.Plot.Axes.Bottom.Max = chartControl.PlotControl.Plot.Axes.Bottom.Max;
            PlotControl.Refresh();
        }

        double[] axes = [];
        public async void OnFocusChanged(bool focused)
        {
            this.focused = focused;
            if (!focused)
                return;
            axes = GetAxeValue().ToArray();
            while (this.focused)
            {
                await Task.Delay(1000);
                if (axes.SequenceEqual(GetAxeValue()))
                {
                    continue;
                }
                GetSensitivity();
                axes = GetAxeValue().ToArray();
                ChartAreaChanged?.Invoke(this);
            }
        }

        private IEnumerable<double> GetAxeValue()
        {
            yield return PlotControl.Plot.Axes.Left.Min;
            yield return PlotControl.Plot.Axes.Left.Max;
            yield return PlotControl.Plot.Axes.Bottom.Min;
            yield return PlotControl.Plot.Axes.Bottom.Max;
        }
    }

}

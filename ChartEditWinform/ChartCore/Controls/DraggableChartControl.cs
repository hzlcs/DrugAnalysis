using ScottPlot.Plottables;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScottPlot.Hatches;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ScottPlot.Colormaps;
using ChartEditWinform.ChartCore.Entity;
using ChartEditWinform.ChartCore.Interface;
using ScottPlot.Palettes;
using System.Collections.Specialized;
using SplitLine = ChartEditWinform.ChartCore.Entity.SplitLine;
using Color = System.Drawing.Color;
using System.Numerics;
using OpenTK;

namespace ChartEditWinform.ChartCore
{
    public partial class DraggableChartControl : UserControl
    {
        private DraggableChartVM chartData = null!;

        public DraggableChartVM ChartData
        {
            get => chartData;
            set
            {
                if (value is null)
                    return;
                chartData = value;


                chartPlot.Plot.Clear();

                MyHighlightText = chartPlot.Plot.Add.Text("", 0, 0);
                MyHighlightText.IsVisible = false;

                chartPlot.Plot.Add.ScatterPoints(chartData.DataSource).Color = ScottPlot.Color.FromARGB((uint)Color.SkyBlue.ToArgb());

                perVaule = value.YMax.Y / 100;
                chartPlot.AddEditLine(value.BaseLine);
                value.SplitLines.CollectionChanged += VerticalLines_CollectionChanged;
                InitSplitLine();
                chartPlot.Plot.Axes.AutoScale();
                chartPlot.Refresh();
            }
        }

        private void VerticalLines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (SplitLine item in e.NewItems!)
                {
                    chartPlot.AddEditLine(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (SplitLine item in e.OldItems!)
                {
                    chartPlot.RemoveSplitLine(item);
                }
            }
        }

        double perVaule = 0.1f;
        private Text? MyHighlightText;

        private Coordinates[] DataSource => chartData.DataSource;

        private DraggedLineInfo? draggedLine;
        private Coordinates mouseCoordinates;
        readonly SaveFileDialog dialog;
        public DraggableChartControl()
        {
            InitializeComponent();

            dialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "csv文件(*.txt)|*.csv",
            };

            chartPlot.MouseDown += FormsPlot1_MouseDown;
            chartPlot.MouseUp += FormsPlot1_MouseUp;
            chartPlot.MouseMove += FormsPlot1_MouseMove;
            chartPlot.KeyDown += FormsPlot1_KeyDown;

            chartPlot.Menu.Add("Add Line", AddLineMenu);
            chartPlot.Menu.Add("Remove Line", RemoveLineMenu);
            chartPlot.Menu.Add("Clear These Line", ClearLineMenu);
            chartPlot.Menu.Add("Save Data", SaveDataMenu);
        }

        private void ClearLineMenu(IPlotControl control)
        {
            var range = chartPlot.Plot.Axes.Bottom.Range;
            var lines = chartData.SplitLines.Where(v => v.Start.X > range.Min && v.Start.X < range.Max).ToArray();
            if (MessageBox.Show($"是否删除这{lines.Length}条分割线?", "", MessageBoxButtons.YesNo) == DialogResult.No)
                return;
            foreach (var line in lines)
                chartData.RemoveSplitLine(line);
            chartPlot.Refresh();
        }

        private void SaveDataMenu(IPlotControl control)
        {
            dialog.FileName = chartData.FileName + ".csv";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = dialog.FileName;
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                string dir = Path.GetDirectoryName(fileName)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(fileName, chartData.GetSaveContent(), Encoding.UTF8);
            }
        }

        private void RemoveLineMenu(IPlotControl control)
        {
            //var chartPoint = chartData.GetChartPoint(mouseCoordinates.X);
            //if (chartPoint is null)
            //{
            //    MessageBox.Show("Can't remove line here");
            //}
            //else
            {
                var lineInfo = chartData.GetDraggedLine(mouseCoordinates);
                if (!lineInfo.HasValue)
                {
                    MessageBox.Show("Can't remove line here");
                }
                else if (lineInfo.Value.IsBaseLine)
                {
                    MessageBox.Show("Can't remove baseLine");
                }
                else
                {
                    SplitLine line = (SplitLine)lineInfo.Value.DraggedLine;
                    if (MessageBox.Show($"确认移除分割线：({line.End.X}, {line.End.Y})?", "", MessageBoxButtons.YesNo) == DialogResult.No)
                        return;
                    chartData.RemoveSplitLine(line);
                    chartPlot.Refresh();
                }
            }
        }

        private void AddLineMenu(IPlotControl control)
        {
            var chartPoint = chartData.GetChartPoint(mouseCoordinates.X);
            if (chartPoint is null)
            {
                MessageBox.Show("Can't add line here");
            }
            else
            {
                var line = AddSplitLine(chartPoint.Value);
                chartData.DraggedLine = DraggableChartVM.GetFocusLineInfo(line);
                chartPlot.Refresh();
            }
        }

        public DraggableChartControl(DraggableChartVM chartData) : this()
        {
            ChartData = chartData;
        }

        private void FormsPlot1_KeyDown(object? sender, KeyEventArgs e)
        {
            Coordinates? markPoint = null;
            if (e.KeyCode == Keys.A)
            {
                if (!chartData.DraggedLine.HasValue || chartData.DraggedLine.Value.IsBaseLine)
                    return;
                var x = chartData.DraggedLine.Value.DraggedLine.Start.X;
                var point = chartData.GetChartPoint(x - Session.unit);
                if (point.HasValue)
                {
                    chartData.DraggedLine.Value.DraggedLine.Line = chartData.CreateSplitLine(point.Value);
                    markPoint = point;
                }
            }
            else if (e.KeyCode == Keys.D)
            {
                if (!chartData.DraggedLine.HasValue || chartData.DraggedLine.Value.IsBaseLine)
                    return;
                var x = chartData.DraggedLine.Value.DraggedLine.Start.X;
                var point = chartData.GetChartPoint(x + Session.unit);
                if (point.HasValue)
                {
                    chartData.DraggedLine.Value.DraggedLine.Line = chartData.CreateSplitLine(point.Value);
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
            chartPlot.Refresh();
        }

        private Vector2d GetSensitivity()
        {
            return new Vector2d(chartPlot.Plot.Axes.Bottom.Width / 50, chartPlot.Plot.Axes.Left.Height / 50);
        }

        private void FormsPlot1_MouseDown(object? sender, MouseEventArgs e)
        {
            if (chartData is null)
                return;

            Pixel mousePixel = new(e.Location.X, e.Location.Y);
            mouseCoordinates = chartPlot.Plot.GetCoordinates(mousePixel);

            chartData.Sensitivity = GetSensitivity();



            if (e.Button == MouseButtons.Left)
            {
                draggedLine = chartData.GetDraggedLine(mouseCoordinates);
                if (draggedLine is not null)
                {
                    var value = draggedLine.Value.GetMarkPoint();
                    if (MyHighlightText is not null)
                    {
                        MyHighlightText.IsVisible = true;
                        MyHighlightText.Location = value;
                        MyHighlightText.LabelText = $"({value.X: 0.000}, {value.Y: 0.000})";
                    }
                    chartPlot.Refresh();
                    chartPlot.Interaction.Disable();
                }
            }
        }

        private void FormsPlot1_MouseUp(object? sender, MouseEventArgs e)
        {
            if (chartData is null)
                return;
            if (draggedLine is null)
                return;
            var line = draggedLine.Value;
            if (line.IsBaseLine)
            {
                foreach (var i in chartData.SplitLines)
                {
                    i.Start = chartData.BaseLine.CreateSplitLinePoint(i.Start.X);
                }
            }

            draggedLine = null;
            if (MyHighlightText is not null)
                MyHighlightText.IsVisible = false;
            chartPlot.Interaction.Enable();
            chartPlot.Refresh();
        }

        private void FormsPlot1_MouseMove(object? sender, MouseEventArgs e)
        {
            if (chartData is null)
                return;
            if (draggedLine is null)
                return;
            Pixel mousePixel = new(e.Location.X, e.Location.Y);
            Coordinates mouseLocation = chartPlot.Plot.GetCoordinates(mousePixel);
            var line = draggedLine.Value;
            var chartPoint = chartData.GetChartPoint(mouseLocation.X, line.IsBaseLine ? mouseLocation.Y : null);
            MoveLine(chartPoint, mouseLocation);
            var markPoint = draggedLine.Value.GetMarkPoint();
            if (MyHighlightText is not null)
            {
                MyHighlightText.Location = markPoint;
                MyHighlightText.LabelText = $"({markPoint.X: 0.000}, {markPoint.Y: 0.000})";
            }

            chartPlot.Refresh();
        }

        private void MoveLine(Coordinates? chartPoint, Coordinates mouseLocation)
        {
            if (!draggedLine.HasValue)
                return;
            var line = draggedLine.Value;
            var editLine = line.DraggedLine;
            if (line.IsBaseLine)
            {
                chartPoint ??= mouseLocation;
                if (line.IsStart)
                {
                    if (editLine.Start.Equals(chartPoint.Value))
                        return;
                    editLine.Start = chartPoint.Value;
                }
                else
                {
                    if (editLine.End.Equals(chartPoint.Value))
                        return;
                    editLine.End = chartPoint.Value;
                }
            }
            else
            {
                chartPoint ??= editLine.End;
                if (chartPoint.Value.X == editLine.Start.X)
                    return;
                editLine.Line = ChartData.CreateSplitLine(chartPoint.Value);
            }
        }

        private Entity.SplitLine? AddSplitLine(double x)
        {

            var point = chartData.GetChartPoint(x);
            if (point is null)
                return null;
            return AddSplitLine(point.Value);
        }

        private SplitLine AddSplitLine(Coordinates point)
        {
            CoordinateLine chartLine = chartData.CreateSplitLine(point);
            var line = new Entity.SplitLine(chartLine);
            chartData.AddSplitLine(line);
            return line;
        }

        public void RemoveLine(double x)
        {
            var verticalLines = ChartData.SplitLines;
            for (int i = 0; i < verticalLines.Count; ++i)
            {
                if (verticalLines[i].Start.X <= x && verticalLines[i].End.X >= x)
                {
                    verticalLines.RemoveAt(i);
                    return;
                }
            }
        }

        private void InitSplitLine()
        {
            //double m = dataSource.Take(dataSource.Length / 2).Min(v => v.Y);
            //for (int i = 0; i < dataSource.Length; i++)
            //    dataSource[i].Y -= m;
            int end = DataSource.Length / 3 * 2;
            while (end < DataSource.Length - 1 && DataSource[end].Y > 0)
                end++;

            int inter = 15;
            List<int> maxDots = new List<int>();
            List<int> minDots = new List<int>();

            for (int i = 0; i < end; i++)
            {
                if (maxDots.Count > 5)
                    inter = 30;
                int max = i;
                for (int j = i; j < end; j++)
                {
                    if (DataSource[j].Y > DataSource[max].Y)
                        max = j;
                    else if (j - max > inter)
                        break;
                }

                if (DataSource[max].Y > 1 * perVaule)
                    maxDots.Add(max);


                if (maxDots.Count > 1 && DataSource[max].X - DataSource[maxDots[maxDots.Count - 2]].X < 0.3)
                    maxDots.Remove(max);

                int min = max;
                for (int j = max + 1; j < end; j++)
                {
                    if (DataSource[j].Y < DataSource[min].Y)
                        min = j;
                    else if (j - min > inter)
                        break;
                }
                if (maxDots.Count > 0 && DataSource[min].X > 10 && DataSource[min].X < 50)
                    minDots.Add(min);

                if (maxDots.Count == 1 && DataSource[max].Y < 10 * perVaule)
                {
                    maxDots.Clear();
                    minDots.Clear();
                }


                i = min;
            }
            foreach (var i in minDots)
            {
                AddSplitLine(DataSource[i].X);
            }
        }


    }


}

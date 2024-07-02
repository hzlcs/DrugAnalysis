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
using ScottPlot.Palettes;
using System.Collections.Specialized;
using SplitLine = ChartEditWinform.ChartCore.Entity.SplitLine;
using Color = System.Drawing.Color;
using System.Numerics;
using OpenTK;
using SkiaSharp.Views.Desktop;
using ChartEditWinform.ChartCore.UserForms;
using System.Diagnostics;
using System.Reflection.Emit;

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
                chartPlot.Plot.XLabel(value.FileName);
                MyHighlightText = chartPlot.Plot.Add.Text("", 0, 0);
                MyHighlightText.IsVisible = false;

                var source = chartPlot.Plot.Add.ScatterPoints(chartData.DataSource);
                source.Color = ScottPlot.Color.FromARGB((uint)Color.SkyBlue.ToArgb());
                source.MarkerSize = 3;

                chartPlot.AddEditLine(value.BaseLine);
                foreach (var i in value.SplitLines)
                {
                    chartPlot.AddEditLine(i);
                }
                value.SplitLines.CollectionChanged += VerticalLines_CollectionChanged;
                //chartData.SplitLines[chartData.SplitLines.Count - 1].TrySetDPIndex(2);
                chartPlot.PerformAutoScale();
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


            InitStyle();

            chartPlot.MouseDown += FormsPlot1_MouseDown;
            chartPlot.MouseUp += FormsPlot1_MouseUp;
            chartPlot.MouseMove += FormsPlot1_MouseMove;
            chartPlot.KeyDown += FormsPlot1_KeyDown;

            chartPlot.Menu.Add("Add Line", AddLineMenu);
            chartPlot.Menu.Add("Remove Line", RemoveLineMenu);
            chartPlot.Menu.Add("Clear These Line", ClearLineMenu);
            chartPlot.Menu.Add("Save Data", SaveDataMenu);
            chartPlot.Menu.Add("Set DP", SetDPMenu);
        }



        private void InitStyle()
        {
            var plot = chartPlot.Plot;
            plot.FigureBackground.Color = Color.White.ToScottColor();
            plot.Grid.IsVisible = false;
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
                var point = chartData.GetChartPoint(x - ChartData.Unit);
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
                var point = chartData.GetChartPoint(x + ChartData.Unit);
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

        private CoordinateLine oldBaseLine;


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
            return chartData.AddSplitLine(point.Value);
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



        #region Mouse
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
                    if (draggedLine.Value.IsBaseLine)
                        oldBaseLine = draggedLine.Value.DraggedLine.Line;
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
                chartData.UpdateBaseLine(oldBaseLine, line.DraggedLine.Line);
            }
            draggedLine = null;
            //ChartData.DraggedLine = null;
            if (MyHighlightText is not null)
                MyHighlightText.IsVisible = false;
            chartPlot.Interaction.Enable();
            chartPlot.Refresh();
        }
        [DebuggerHidden]
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
        #endregion

        #region Menu
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
                var line = chartData.AddSplitLine(chartPoint.Value);
                chartData.DraggedLine = DraggableChartVM.GetFocusLineInfo(line);
                chartPlot.Refresh();
            }
        }

        private void SetDPMenu(IPlotControl control)
        {
            var lineInfo = chartData.GetDraggedLine(mouseCoordinates, true);
            if (!lineInfo.HasValue)
            {
                MessageBox.Show("Can't set dp here");
            }
            else if (lineInfo.Value.IsBaseLine)
            {
                MessageBox.Show("Can't set baseLine");
            }
            else
            {
                SplitLine line = (SplitLine)lineInfo.Value.DraggedLine;
                var form = new InputDPForm(line.DP);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    if (!line.TrySetDPIndex(form.DPValue))
                    {
                        MessageBox.Show("无效的DP值");
                    }
                    else
                    {
                        chartData.DraggedLine = null;
                        chartData.DraggedLine = DraggableChartVM.GetFocusLineInfo(line);
                    }
                }

            }
        }

        internal void AutoFit()
        {
            chartPlot.Plot.Axes.AutoScale();
            chartPlot.Refresh();
        }

        internal byte[] GetImage()
        {
            //PixelSize size = chartPlot.Plot.RenderManager.LastRender.FigureRect.Size;
            PixelSize size = new(1920, 1080);
            return chartPlot.Plot.GetImageBytes((int)size.Width, (int)size.Height, ImageFormat.Png);
        }
        #endregion
    }


}

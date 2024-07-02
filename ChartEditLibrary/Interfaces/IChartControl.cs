using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using OpenTK.Mathematics;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Interfaces
{
    public interface IChartControl
    {
        IPlotControl PlotControl { get; set; }

        DraggableChartVM? ChartData { get; set; }

        void MouseDown(object? sender, Point mousePoint, bool left);
    }

    public class ChartControl : IChartControl
    {
        public IPlotControl PlotControl { get; set; }
        public DraggableChartVM? ChartData { get; set; }
        private Text? MyHighlightText;
        private Coordinates mouseCoordinates;
        private DraggedLineInfo? draggedLine;
        private CoordinateLine oldBaseLine;
        public ChartControl(IPlotControl plotControl)
        {
            PlotControl = plotControl;
        }
        private Vector2d GetSensitivity()
        {
            return new Vector2d(PlotControl.Plot.Axes.Bottom.Width / 50, PlotControl.Plot.Axes.Left.Height / 50);
        }
        public void MouseDown(object? sender, Point mousePoint, bool left)
        {
            if (ChartData is null)
                return;

            Pixel mousePixel = new(mousePoint.X, mousePoint.Y);
            mouseCoordinates = PlotControl.Plot.GetCoordinates(mousePixel);

            ChartData.Sensitivity = GetSensitivity();



            if (left)
            {
                draggedLine = ChartData.GetDraggedLine(mouseCoordinates);
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
                    PlotControl.Refresh();
                    PlotControl.Interaction.Disable();
                }
            }
        }
    }

}

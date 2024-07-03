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
using ScottPlot.Palettes;
using System.Collections.Specialized;
using Color = System.Drawing.Color;
using System.Numerics;
using OpenTK;
using SkiaSharp.Views.Desktop;
using ChartEditWinform.ChartCore.UserForms;
using System.Diagnostics;
using System.Reflection.Emit;
using ChartEditLibrary.ViewModel;
using ChartEditLibrary.Model;
using OpenTK.Mathematics;
using ChartEditLibrary;
using ChartEditLibrary.Interfaces;

namespace ChartEditWinform.ChartCore
{
    public partial class DraggableChartControl : UserControl
    {
        private IChartControl? chartControl;
        public IChartControl ChartControl
        {
            get => chartControl!;
            set
            {
                chartControl = value;
                chartControl.BindControl(chartPlot);
                chartPlot.Menu.Add("Add Line", ChartControl.AddLineMenu);
                chartPlot.Menu.Add("Remove Line", ChartControl.RemoveLineMenu);
                chartPlot.Menu.Add("Clear These Line", ChartControl.ClearLineMenu);
                chartPlot.Menu.Add("Save Data", ChartControl.SaveDataMenu);
                chartPlot.Menu.Add("Set DP", ChartControl.SetDPMenu);
            }
        }

        public DraggableChartControl()
        {
            InitializeComponent();

            chartPlot.MouseDown += FormsPlot1_MouseDown;
            chartPlot.MouseUp += FormsPlot1_MouseUp;
            chartPlot.MouseMove += FormsPlot1_MouseMove;
            chartPlot.KeyDown += FormsPlot1_KeyDown;
            
        }
        


        private void FormsPlot1_KeyDown(object? sender, KeyEventArgs e)
        {
            ChartControl.KeyDown(sender, e.KeyCode.ToString());
        }

        #region Mouse
        private void FormsPlot1_MouseDown(object? sender, MouseEventArgs e)
        {
            ChartControl.MouseDown(sender, e.Location, e.Button == MouseButtons.Left);
        }

        private void FormsPlot1_MouseUp(object? sender, MouseEventArgs e)
        {
            ChartControl.MouseUp(sender);
        }
        [DebuggerHidden]
        private void FormsPlot1_MouseMove(object? sender, MouseEventArgs e)
        {
            ChartControl?.MouseMove(sender, e.Location);
        }
        #endregion

        internal void AutoFit()
        {
            ChartControl.AutoFit();
        }

        internal byte[] GetImage()
        {
            return ChartControl.GetImage();
        }

    }


}

using LanguageExt.ClassInstances.Pred;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static ChartEditLibrary.Model.PCAManager;

namespace ChartEditWPF.Windows
{
    /// <summary>
    /// PCAWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PCAWindow : Window
    {

        public PCAWindow()
        {
            InitializeComponent();
        }

        public PCAWindow(Result result) : this(result.Samples, result.SingularValues, result.EigenVectors)
        {
        }

        public PCAWindow(SamplePCA[] data, double[] singularValues, double[] eigenVectors) : this()
        {
            chart.EigenVectors = eigenVectors;
            chart.SingularValues = singularValues;
            chart.Samples = data;
        }

        internal byte[] GetResult()
        {
            PixelSize size = new(1920, 1080);
            //size = chart.Plot.RenderManager.LastRender.FigureRect.Size;
            return chart.Plot.GetImageBytes((int)size.Width, (int)size.Height, ImageFormat.Png);
        }
    }
}

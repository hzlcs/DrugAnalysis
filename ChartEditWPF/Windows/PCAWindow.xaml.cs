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
            r2x1.Text = eigenVectors[0].ToString("F3");
            r2x2.Text = eigenVectors[1].ToString("F3");
            chart.SingularValues = singularValues;
            chart.Samples = data;
        }
    }
}

using ChartEditLibrary.Interfaces;
using ChartEditWPF.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
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
using static ChartEditLibrary.Model.HotChartManager;
using Path = System.IO.Path;

namespace ChartEditWPF.Windows
{
    /// <summary>
    /// HotChartWindow.xaml 的交互逻辑
    /// </summary>
    public partial class HotChartWindow : Window
    {
        public HotChartWindow()
        {
            InitializeComponent();
            charts ??= [];
        }

        readonly HotChartPlot[] charts;

        public HotChartWindow(HotChartData data) : this()
        {
            charts = [chart1, chart2, chart3, chart4];
            var main = data.MainData;
            var baseValue = main.Max(v => v.Value.GetValueOrDefault());
            var sum = main.Sum(v => v.Value.GetValueOrDefault());
            int index = 0;
            //chart1.Detail2 = (data.MainData, data.Datas);

            foreach (var detail in data.Datas1)
            {
                var value = main.First(v => v.Description == detail.Key.ToString()).Value.GetValueOrDefault();
                var scale = value / baseValue;
                var area = value / sum;
                charts[index].Detail = new HotChartDataDetail(data.MainData[index].Description, area, scale, detail.Value);
                index++;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var c in charts)
                c.AutoScale();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = App.ServiceProvider.GetRequiredService<IFileDialog>();
            if (!fileDialog.ShowDialog(null, out var files))
                return;
            var message = App.ServiceProvider.GetRequiredService<IMessageBox>();
            try
            {
                string fileName = Path.Combine(Path.GetDirectoryName(files[0])!, Path.GetFileNameWithoutExtension(files[0]));
                foreach (var c in charts)
                {
                    string sFile = fileName + "-" + c.Detail.Description + ".png";
                    PixelSize size = chart1.Plot.RenderManager.LastRender.FigureRect.Size;
                    File.WriteAllBytes(sFile, c.Plot.GetImageBytes((int)size.Width, (int)size.Height, ImageFormat.Png));
                }
            }
            catch (Exception ex)
            {
                message.Popup(ex.Message, NotificationType.Error);
            }
            message.Popup("导出成功", NotificationType.Success);

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var c in charts)
                c.Dispose();
        }
    }
}

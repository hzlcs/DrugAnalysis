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

namespace ChartEditWPF.Windows
{
    /// <summary>
    /// SelectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SelectOneWindow : Window
    {
        private readonly Action<object> callback;

        public SelectOneWindow(string title, Array options, Action<object> callback)
        {
            InitializeComponent();
            Title = title;
            foreach (var option in options)
            {
                combox.Items.Add(option);
            }
            combox.SelectedIndex = 0;
            this.callback = callback;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            callback?.Invoke(combox.SelectedItem);
            DialogResult = true;
        }
    }
}

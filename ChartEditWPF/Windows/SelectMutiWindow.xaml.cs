using ChartEditWPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
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
    /// SelectMutiWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SelectMutiWindow : Window
    {
        private readonly Action<object[]>? action;

        public SelectMutiWindow()
        {
            InitializeComponent();
        }

        public SelectMutiWindow(string title, string itemName, Array data, Action<object[]> action) : this()
        {
            Title = title;
            column.Header = itemName;
            column.Binding = new Binding("Item");
            checkBox.Binding = new Binding("IsSelected");
            SelectItem[] items = new SelectItem[data.Length];
            for (var i = 0; i < data.Length; i++)
            {
                items[i] = new SelectItem(data.GetValue(i)!);
                
            }
            grid.ItemsSource = items;
            this.action = action;
        }

        public SelectMutiWindow(string title, string itemName, SelectItem[] items, Action<object[]> action) : this()
        {
            Title = title;
            column.Header = itemName;
            column.Binding = new Binding("Item");
            checkBox.Binding = new Binding("IsSelected");
            grid.ItemsSource = items;
            this.action = action;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            action?.Invoke(grid.ItemsSource.Cast<SelectItem>().Where(v => v.IsSelected).Select(v => v.Item).ToArray());
            DialogResult = true;
        }

        

        private bool editing = false;

        private void headerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            editing = true;
            foreach (var i in grid.ItemsSource)
            {
                if (i is SelectItem item)
                {
                    item.IsSelected = headerCheckBox.IsChecked.GetValueOrDefault();
                }
            }
            editing = false;
        }

        private void grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (editing)
                return;
            foreach (SelectItem i in e.AddedItems)
            {
                i.IsSelected = !i.IsSelected;
            }
        }
    }
}

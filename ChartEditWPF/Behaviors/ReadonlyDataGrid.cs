using HandyControl.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChartEditWPF.Behaviors
{
    internal class ReadonlyDataGrid
    {


        public static bool GetDefault(DependencyObject obj)
        {
            return (bool)obj.GetValue(DefaultProperty);
        }

        public static void SetDefault(DependencyObject obj, bool value)
        {
            obj.SetValue(DefaultProperty, value);
        }

        // Using a DependencyProperty as the backing store for Default.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultProperty =
            DependencyProperty.RegisterAttached("Default", typeof(bool), typeof(ReadonlyDataGrid), new PropertyMetadata(false, ValueChanged));

        private static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is DataGrid dataGrid)
            {
                dataGrid.IsReadOnly = true;
                dataGrid.CanUserAddRows = false;
                dataGrid.CanUserDeleteRows = false;
                dataGrid.CanUserReorderColumns = false;
                dataGrid.CanUserResizeColumns = false;
                dataGrid.CanUserResizeRows = false;
                dataGrid.CanUserSortColumns = false;
                dataGrid.AutoGenerateColumns = false;
            }
        }
    }
}

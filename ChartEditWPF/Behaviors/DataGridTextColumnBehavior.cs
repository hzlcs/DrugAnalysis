using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChartEditWPF.Behaviors
{
    internal class DataGridTextColumnBehavior
    {


        public static bool GetStyle(DependencyObject obj)
        {
            return (bool)obj.GetValue(StyleProperty);
        }

        public static void SetStyle(DependencyObject obj, bool value)
        {
            obj.SetValue(StyleProperty, value);
        }

        // Using a DependencyProperty as the backing store for Style.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StyleProperty =
            DependencyProperty.RegisterAttached("Style", typeof(bool), typeof(DataGridTextColumnBehavior), new PropertyMetadata(false, ValueChanged));

        private static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)d;
            if((bool)e.NewValue)
            {
                dataGrid.Columns.CollectionChanged += CollectionChanged;
            }
            else
            {
                dataGrid.Columns.CollectionChanged -= CollectionChanged;
            }
        }

        private static void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                if(e.NewItems is not null)
                {
                    foreach(var item in e.NewItems)
                    {
                        if(item is DataGridTextColumn column)
                        {
                            column.IsReadOnly = true;
                            column.CanUserResize = false;
                        }
                    }
                }
            }
        }
    }
}

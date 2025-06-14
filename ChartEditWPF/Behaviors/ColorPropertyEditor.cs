using ChartEditLibrary.Entitys;
using ChartEditWPF.ViewModels;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChartEditWPF.Behaviors
{
    public class ColorPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            var control = new Controls.ColorEditControl() { DataContext = new ColorPropertyEditVM() };
            return control;
        }

        public override DependencyProperty GetDependencyProperty()
        {
            return ColorEditProperty;
        }

        public static readonly DependencyProperty ColorEditProperty =
            DependencyProperty.Register("ColorEdit", typeof(ColorPropertyEditVM), typeof(ColorPropertyEditor), new PropertyMetadata(null));
    }
}

using ChartEditLibrary.Entitys;
using HandyControl.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChartEditWPF.Behaviors
{
    public class RangePropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            var control = new ChartEditWPF.Controls.RangeControl();
            control.SetBinding(UserControl.DataContextProperty, new System.Windows.Data.Binding($"Value.{propertyItem.PropertyName}")
            { Source = propertyItem, UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
            return control;
        }

        public override DependencyProperty GetDependencyProperty()
        {
            return RangeProperty;
        }

        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register("Range", typeof(TwoDConfig.Range), typeof(RangePropertyEditor), new PropertyMetadata(default(TwoDConfig.Range)));

    }
}

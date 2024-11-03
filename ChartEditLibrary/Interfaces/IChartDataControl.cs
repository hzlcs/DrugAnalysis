using ChartEditLibrary.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Interfaces
{
    public interface IChartDataControl
    {
        void BindData(DraggableChartVm vm);

        void SplitLines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e);

        void DraggedLineChanged(object? sender, PropertyChangedEventArgs e);

        void BaseLineDataChanged();

        void CurrentSplitLine_PropertyChanged(object? sender, PropertyChangedEventArgs e);
    }

}

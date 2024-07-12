using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ChartEditWPF.ViewModels
{
    public partial class ShowControlViewModel : ObservableObject
    {
        public ShowControlViewModel(IChartControl chartControl, DraggableChartVM draggableChartVM)
        {
            ChartControl = chartControl;
            DraggableChartVM = draggableChartVM;
            draggableChartVM.DraggedLineChanged += DraggableChartVM_DraggedLineChanged;
        }

        private void DraggableChartVM_DraggedLineChanged(SplitLine obj)
        {
            DraggedLine = obj;
        }

        public IChartControl ChartControl { get; set; }

        public DraggableChartVM DraggableChartVM { get; set; }

        [ObservableProperty]
        private SplitLine? draggedLine;

        [RelayCommand]
        void CopyData()
        {
            string data = string.Join("\n", DraggableChartVM.SplitLines.Select(v => string.Join("\t", v)));
            Clipboard.Clear();
            Clipboard.SetText(data, TextDataFormat.Text);
            Clipboard.Flush();
        }
    }
}

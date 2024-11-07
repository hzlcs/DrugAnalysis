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
        public ShowControlViewModel(IChartControl chartControl, DraggableChartVm draggableChartVM)
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

        public DraggableChartVm DraggableChartVM { get; set; }

        [ObservableProperty]
        private bool showData = true;

        [ObservableProperty]
        private SplitLine? draggedLine;

        [RelayCommand]
        private void CopyData()
        {
            var data = string.Join("\n", DraggableChartVM.SplitLines.Select(v => string.Join("\t", v)).Prepend("Peak\tStart X\tEnd X\tCenter X\tArea\tArea Sum %\tDP"));
            Clipboard.Clear();
            Clipboard.SetText(data, TextDataFormat.Text);
            Clipboard.Flush();
        }
    }
}

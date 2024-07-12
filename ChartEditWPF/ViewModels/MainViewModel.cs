using ChartEditLibrary.Interfaces;
using ChartEditLibrary.ViewModel;
using ChartEditWPF.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ChartEditWPF.ViewModels
{
    internal partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private DataTableList dataTable;

        [ObservableProperty]
        IPage? content;

        private readonly IServiceProvider serviceProvider;

        public MainViewModel(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            dataTable = new DataTableList(["col1", "col2"], ["row1", "row2", "row3", "row4", "row5", "row6", "row7", "row8", "row9"]);
        }

        public ObservableCollection<ShowControlViewModel> DataSources { get; set; } = [];

        [RelayCommand]
        void Add()
        {
            var vm = DraggableChartVM.CreateAsync(@"D:\DragAnalysis\原研\9S361-1.csv", ChartEditLibrary.Entitys.ExportType.Enoxaparin).Result;
            vm.InitSplitLine(null);
            IChartControl chartControl = serviceProvider.GetRequiredService<IChartControl>();
            chartControl.ChartData = vm;
            ShowControlViewModel svm = new ShowControlViewModel(chartControl, chartControl.ChartData);
            DataSources.Add(svm);
        }

        [RelayCommand]
        void ButtonClick(object tag)
        {
            if(tag is not null)
                Content = serviceProvider.GetRequiredKeyedService<IPage>(tag);
        }
    }

}

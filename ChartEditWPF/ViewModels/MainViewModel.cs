using ChartEditLibrary.Interfaces;
using ChartEditLibrary.ViewModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.ViewModels
{
    internal partial class MainViewModel
    {
        private readonly IServiceProvider serviceProvider;

        public MainViewModel(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
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
    }
}

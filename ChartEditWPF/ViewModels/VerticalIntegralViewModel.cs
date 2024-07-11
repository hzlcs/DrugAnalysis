using ChartEditLibrary.Entitys;
using ChartEditLibrary.Interfaces;
using ChartEditLibrary.ViewModel;
using ChartEditWPF.Services;
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
    public partial class VerticalIntegralViewModel
    {
        static readonly List<ShowControlViewModel> vms = [];
        static readonly ISelectDialog selectDialog = App.ServiceProvider.GetRequiredService<ISelectDialog>();
        static readonly IFileDialog fileDialog = App.ServiceProvider.GetRequiredService<IFileDialog>();

        readonly ExportType[] exportTypes = Enum.GetValues<ExportType>();

        public ObservableCollection<ShowControlViewModel> DataSources { get; set; } = [];

        public VerticalIntegralViewModel()
        {
            foreach(var vm in vms)
            {
                DataSources.Add(vm);
            }
        }

        [RelayCommand]
        void Import()
        {
            ExportType type =  (ExportType)selectDialog.ShowDialog("选择导入类型", exportTypes);
            if (!fileDialog.ShowDialog())
                return;
            foreach(var file in fileDialog.FileNames!)
            {
                var vm = DraggableChartVM.CreateAsync(file, type).Result;
                vm.InitSplitLine(null);
                IChartControl chartControl = App.ServiceProvider.GetRequiredService<IChartControl>();
                chartControl.ChartData = vm;
                ShowControlViewModel svm = new ShowControlViewModel(chartControl, chartControl.ChartData);
                DataSources.Add(svm);
                vms.Add(svm);
            }
            
        }
    }
}

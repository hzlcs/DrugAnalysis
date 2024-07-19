using ChartEditLibrary.Entitys;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.ViewModels
{
    internal partial class VerticalIntegralConfigViewModel : ObservableObject
    {
        [ObservableProperty]
        private Config config;

        public VerticalIntegralConfigViewModel()
        {
            Config = Config.GetConfig(ExportType.Enoxaparin);
        }

        [RelayCommand]
        void SelectConfig(ExportType type)
        {
            Config = Config.GetConfig(type);
        }
    }
}

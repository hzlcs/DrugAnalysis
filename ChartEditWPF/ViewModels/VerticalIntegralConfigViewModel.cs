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

        public ExportType[] ExportTypes { get; } = Enum.GetValues(typeof(ExportType)).Cast<ExportType>().ToArray();

        [ObservableProperty]
        private Config config;

        [ObservableProperty]
        private ExportType currentType;

        public VerticalIntegralConfigViewModel()
        {
            Config.LoadConfig();
            currentType = ExportType.Enoxaparin;
            Config = Config.GetConfig(ExportType.Enoxaparin);
        }

        partial void OnCurrentTypeChanged(ExportType value)
        {
            Config = Config.GetConfig(value);
        }

        [RelayCommand]
        void SelectConfig(ExportType type)
        {
            Config = Config.GetConfig(type);
        }

        [RelayCommand]
        void SaveConfig()
        {
            Config.SaveConfig();
        }

    }
}

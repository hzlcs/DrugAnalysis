using ChartEditLibrary.Entitys;
using ChartEditLibrary.Interfaces;
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
        private readonly IMessageBox messageBox;

        public VerticalIntegralConfigViewModel(IMessageBox messageBox)
        {
            Config.LoadConfig();
            currentType = ExportType.Enoxaparin;
            Config = Config.GetConfig(ExportType.Enoxaparin);
            this.messageBox = messageBox;
        }

        partial void OnCurrentTypeChanged(ExportType value)
        {
            Config = Config.GetConfig(value);
        }

        [RelayCommand]
        private void SaveConfig()
        {
            Config.SaveConfig();
            messageBox.Popup("保存成功", NotificationType.Success);
        }

    }
}

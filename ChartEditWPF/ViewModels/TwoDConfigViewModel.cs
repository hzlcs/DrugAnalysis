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
    public partial class TwoDConfigViewModel(IMessageBox messageBox) : ObservableObject
    {
        public TwoDConfig TwoDConfig { get; } = TwoDConfig.Instance;

        [RelayCommand]
        void SaveConfig()
        {
            TwoDConfig.SaveConfig();
            messageBox.Popup("保存成功", NotificationType.Success);
        }
    }
}

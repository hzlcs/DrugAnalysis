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
    internal partial class MutiConfigPageVM(IMessageBox messageBox) : ObservableObject
    {
        public MutiConfig MutiConfig { get; } = MutiConfig.Instance;

        [RelayCommand]
        void SaveConfig()
        {
            MutiConfig.SaveConfig();
            messageBox.Popup("保存成功", NotificationType.Success);
        }
    }
}

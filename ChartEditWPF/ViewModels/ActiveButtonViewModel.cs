using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ChartEditWPF.ViewModels
{
    public partial class ActiveButtonViewModel(Action<bool> activeChanged) : ObservableObject
    {
        public bool IsActive { get; private set; }

        [ObservableProperty]
        private Brush background = Brushes.White;

        [RelayCommand]
        private void ChangeActive()
        {
            IsActive = !IsActive;
            Background = IsActive ? Brushes.LightGreen : Brushes.White;
            activeChanged(IsActive);
        }
    }
}

using ChartEditLibrary.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChartEditWPF.ViewModels
{
    internal partial class ChartEditViewModel
    {
        IChartControl? chartControl;
        public IChartControl? ChartControl { get; set; }

    }
}

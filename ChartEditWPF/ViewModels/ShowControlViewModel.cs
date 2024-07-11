using ChartEditLibrary.Interfaces;
using ChartEditLibrary.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChartEditWPF.ViewModels
{
    public class ShowControlViewModel
    {
        public ShowControlViewModel(IChartControl chartControl, DraggableChartVM draggableChartVM)
        {
            ChartControl = chartControl;
            DraggableChartVM = draggableChartVM;
            
        }

        public IChartControl ChartControl { get; set; }
        
        public DraggableChartVM DraggableChartVM { get; set; }


    }
}

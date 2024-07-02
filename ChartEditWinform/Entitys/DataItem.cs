using ChartEditLibrary.ViewModel;
using ChartEditWinform.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWinform.Entitys
{
    internal class DataItem
    {
        public bool Selected { get; set; }
        public string FileName { get; set; } = null!;
        public DraggableChartVM DraggableChartVM { get; set; } = null!;
        public ShowControl Control { get; set; } = null!;
    }
}

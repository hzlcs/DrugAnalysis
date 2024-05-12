using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScottPlot;
using ScottPlot.Plottables;

namespace ChartEditWPF.Model
{


    public class EditLine 
    { 
        public Coordinates Start { get; set; }
        public Coordinates End { get; set; }
    }

    public class EditBaseLine :EditLine
    {

    }

}

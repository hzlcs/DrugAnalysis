using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt.Pretty;

namespace ChartEditLibrary
{
    public static class Utility
    {
        public const double Tolerance = 1e-6;

        public static bool ToleranceEqual(double a, double b) => Math.Abs(a - b) <= Tolerance;

    }
}

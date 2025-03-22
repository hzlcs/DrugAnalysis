using ScottPlot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Entitys
{


    public class CacheContent
    {
        private static readonly string CacheFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ChartEdit");
        public static string SingleCacheFile => CacheFile + "\\single\\cache.json";
        public static string MutiCacheFile => CacheFile + "\\muti\\cache.json";
        public static string TwoDCacheFile => CacheFile + "\\TwoD\\cache.json";


        public string FilePath { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string? ExportType { get; set; }
        public string Description { get; set; } = null!;
        public double[] X { get; set; } = null!;
        public double[] Y { get; set; } = null!;
        public string SaveContent { get; set; } = null!;
        public Line[]? CuttingLines { get; set; }

        public struct Point(double x, double y)
        {
            public double X { get; set; } = x;
            public double Y { get; set; } = y;
        }
        public struct Line(CoordinateLine line)
        {
            public Point Start { get; set; } = new Point(line.X1, line.Y1);
            public Point End { get; set; } = new Point(line.X2, line.Y2);
        }
    }

   
}

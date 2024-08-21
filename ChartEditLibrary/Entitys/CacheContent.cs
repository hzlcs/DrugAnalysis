using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Entitys
{


    public class CacheContent
    {
        public static readonly string cacheFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ChartEdit\\cache.json");

        public string FilePath { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public double[] X { get; set; } = null!;
        public double[] Y { get; set; } = null!;
        public string SaveContent { get; set; } = null!;
    }
}

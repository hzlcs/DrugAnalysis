using System;
using System.Collections.Generic;
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


        public string FilePath { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public double[] X { get; set; } = null!;
        public double[] Y { get; set; } = null!;
        public string SaveContent { get; set; } = null!;
    }
}

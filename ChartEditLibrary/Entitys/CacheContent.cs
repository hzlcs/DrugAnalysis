using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Entitys
{
    public class CacheContent
    {
        public string FileName { get; set; } = null!;
        public double[] X { get; set; } = null!;
        public double[] Y { get; set; } = null!;
        public string SaveContent { get; set; } = null!;
    }
}

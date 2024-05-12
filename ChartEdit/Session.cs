using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF
{
    internal static class Session
    {
        public static float unit = 0.0066666667f;

    }

    public static class Utility 
    {
        public static Coordinates[] ReadCsv(string path, int skip = 0)
        {
            char[] separator = new char[] { '\n' };
            char[] spe = new char[] { ',', '\t' };
            string[] data;
            using (StreamReader sr = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                sr.ReadLine();
                sr.ReadLine();
                data = sr.ReadToEnd().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                sr.Close();
            }

            double[][] temp = data.Select(v => v.Split(spe).Skip(skip).Select(v1 => double.Parse(v1)).ToArray())
                .Where(v => v[0] >= 20 && v[0] <= 60).ToArray();

            return temp.Select(v => new Coordinates(v[0], v[1])).ToArray();
        }
    }

}

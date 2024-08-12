using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChartEditLibrary.Model.AreaDatabase;

namespace ChartEditLibrary.Model
{
    public static class SampleManager
    {
        private static readonly char[] lineSeparator = ['\n', '\r'];

        public static async Task<SampleArea[]> GetSampleAreasAsync(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException("未找到样品文件", fileName);
            using StreamReader sr = new(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            string[][] text = (await sr.ReadToEndAsync()).Split(lineSeparator, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Split(',')).ToArray();
            var title = text[0];
            SampleArea[] sampleAreas = new SampleArea[(title.Length - 1) / 6];
            string[] dps = text.Skip(2).Select(v => v[0][2..]).ToArray();
            for (int i = 0; i < sampleAreas.Length; ++i)
            {
                sampleAreas[i] = new SampleArea(title[1 + i * 7], dps, text.Skip(2).Select(v =>
                {
                    string value = v[1 + i * 7 + 5];
                    if (string.IsNullOrEmpty(value))
                        return null;
                    return new float?(float.Parse(value));
                }).ToArray());
            }
            return sampleAreas;
        }

        /// <summary>
        /// 读取相对面积数据库
        /// </summary>
        /// <param name="fileName">文件路径</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">文件不存在</exception>
        /// <exception cref="Exception">读取数据库发生错误</exception>
        public static async Task<AreaDatabase> GetDatabaseAsync(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("文件不存在", fileName);
            }
            using FileStream fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(fileStream, Encoding.UTF8);
            string text = (await reader.ReadToEndAsync()).Replace("��n=3��", "(n=3)");
            try
            {
                string[][] lines = text.Split(lineSeparator, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Split(',')).ToArray();
                string[] sampleNames = lines[0].Skip(1).ToArray();
                string[] dps = lines.Skip(1).Select(v => v[0][2..]).ToArray();
                AreaRow[] rows = new AreaRow[dps.Length];
                for (int i = 0; i < dps.Length; i++)
                {
                    float?[] areas = new float?[sampleNames.Length];
                    for (int j = 0; j < sampleNames.Length; j++)
                    {
                        if (float.TryParse(lines[i + 1][j + 1], out float value))
                        {
                            areas[j] = value;
                        }
                    }
                    rows[i] = new AreaRow(dps[i], areas);
                }
                return new AreaDatabase(Path.GetFileNameWithoutExtension(fileName), sampleNames, dps, rows);
            }
            catch (Exception ex)
            {
                throw new Exception("读取数据库失败:" + ex.Message);
            }

        }

        public static string[] MergeDP(IEnumerable<string[]> dps)
        {
            string[] res = dps.SelectMany(v => v).Distinct().ToArray();
            Array.Sort(res, DPCompare);
            return res;
        }

        private static int DPCompare(string l, string r)
        {
            int[] ls = l.Split('-').Select(int.Parse).ToArray();
            int[] rs = r.Split('-').Select(int.Parse).ToArray();
            if (ls[0] != rs[0])
                return rs[0] - ls[0];
            if (ls.Length == rs.Length && ls.Length == 2)
            {
                return rs[1] - ls[1];
            }
            if (ls.Length == 1)
                return 1;
            return -1;
        }

        public static double TCheck(float[] left, float[] right)
        {
            return TTest(left, right);
        }

        public static double TTest(float[] x, float[] y)
        {
            if (x.Length == 1)
            {
                x = [x[0], x[0]];
            }
            if (y.Length == 1)
            {
                y = [y[0], y[0]];
            }
            double sumX = 0.0;
            double sumY = 0.0;
            for (int i = 0; i < x.Length; ++i)
                sumX += x[i];
            for (int i = 0; i < y.Length; ++i)
                sumY += y[i];
            int n1 = x.Length;
            int n2 = y.Length;
            double meanX = sumX / n1;
            double meanY = sumY / n2;
            double sumXminusMeanSquared = 0.0; // Calculate variances
            double sumYminusMeanSquared = 0.0;
            for (int i = 0; i < n1; ++i)
                sumXminusMeanSquared += (x[i] - meanX) * (x[i] - meanX);
            for (int i = 0; i < n2; ++i)
                sumYminusMeanSquared += (y[i] - meanY) * (y[i] - meanY);
            double varX = sumXminusMeanSquared / (n1 - 1);
            double varY = sumYminusMeanSquared / (n2 - 1);
            double top = (meanX - meanY);
            double bot = Math.Sqrt((varX / n1) + (varY / n2));
            double t = top / bot;
            double num = ((varX / n1) + (varY / n2)) * ((varX / n1) + (varY / n2));
            double denomLeft = ((varX / n1) * (varX / n1)) / (n1 - 1);
            double denomRight = ((varY / n2) * (varY / n2)) / (n2 - 1);
            double denom = denomLeft + denomRight;
            double df = num / denom;
            double p = Student(t, 8); // Cumulative two-tail density
            return p;
            //Console.WriteLine("mean of x = " + meanX.ToString("F3"));
            //Console.WriteLine("mean of y = " + meanY.ToString("F3"));
            //Console.WriteLine("t = " + t.ToString("F5"));
            //Console.WriteLine("df = " + df.ToString("F4"));
            //Console.WriteLine("p-value = " + p.ToString("F6"));
            //Explain();
        }

        public static double Student(double t, double df)
        {
            // for large integer df or double df
            // adapted from ACM algorithm 395
            // returns 2-tail p-value
            double n = df; // to sync with ACM parameter name
            double a, b, y;
            t = t * t;
            y = t / n;
            b = y + 1.0;
            if (y > 1.0E-6) y = Math.Log(b);
            a = n - 0.5;
            b = 48.0 * a * a;
            y = a * y;
            y = (((((-0.4 * y - 3.3) * y - 24.0) * y - 85.5) /
            (0.8 * y * y + 100.0 + b) + y + 3.0) / b + 1.0) *
            Math.Sqrt(y);
            return 2.0 * Gauss(-y); // ACM algorithm 209
        }

        public static double Gauss(double z)
        {
            // input = z-value (-inf to +inf)
            // output = p under Standard Normal curve from -inf to z
            // e.g., if z = 0.0, function returns 0.5000
            // ACM Algorithm #209
            double y; // 209 scratch variable
            double p; // result. called 'z' in 209
            double w; // 209 scratch variable
            if (z == 0.0)
                p = 0.0;
            else
            {
                y = Math.Abs(z) / 2;
                if (y >= 3.0)
                {
                    p = 1.0;
                }
                else if (y < 1.0)
                {
                    w = y * y;
                    p = ((((((((0.000124818987 * w
                    - 0.001075204047) * w + 0.005198775019) * w
                    - 0.019198292004) * w + 0.059054035642) * w
                    - 0.151968751364) * w + 0.319152932694) * w
                    - 0.531923007300) * w + 0.797884560593) * y * 2.0;
                }
                else
                {
                    y = y - 2.0;
                    p = (((((((((((((-0.000045255659 * y
                    + 0.000152529290) * y - 0.000019538132) * y
                    - 0.000676904986) * y + 0.001390604284) * y
                    - 0.000794620820) * y - 0.002034254874) * y
                    + 0.006549791214) * y - 0.010557625006) * y
                    + 0.011630447319) * y - 0.009279453341) * y
                    + 0.005353579108) * y - 0.002141268741) * y
                    + 0.000535310849) * y + 0.999936657524;
                }
            }
            if (z > 0.0)
                return (p + 1.0) / 2;
            else
                return (1.0 - p) / 2;
        }
    }

    public class AreaDatabase
    {
        public string ClassName { get; }
        public string[] SampleNames { get; }
        public string[] DP { get; }
        public AreaRow[] Rows { get; }

        public AreaRow this[string dp]
        {
            get
            {
                int index = Array.IndexOf(DP, dp);
                if (index == -1)
                {
                    throw new IndexOutOfRangeException("DP not found");
                }
                return Rows[index];
            }
        }

        public AreaDatabase(string className, string[] sampleNames, string[] dps, AreaRow[] rows)
        {
            this.ClassName = className;
            this.SampleNames = sampleNames;
            this.DP = dps;
            this.Rows = rows;
        }

        protected AreaDatabase(AreaDatabase database)
        {
            ClassName = database.ClassName;
            SampleNames = database.SampleNames;
            DP = database.DP;
            Rows = database.Rows;
        }

        public bool TryGetRow(string dp, [MaybeNullWhen(false)] out AreaRow row)
        {
            int index = Array.IndexOf(DP, dp);
            if (index == -1)
            {
                row = null;
                return false;
            }
            row = Rows[index];
            return true;
        }



        public class AreaRow
        {
            public string DP { get; }
            public float?[] Areas { get; }
            public float? Average { get; }
            public double StdDev { get; }
            public double RSD { get; }

            public AreaRow(string dp, float?[] areas)
            {
                this.DP = dp;
                this.Areas = areas;
                float[] values = areas.Where(v => v.HasValue).Select(v => v!.Value).ToArray();
                if (values.Length > 0)
                {
                    Average = values.Average();
                    StdDev = CalculateStdDev(values);
                    RSD = StdDev / Average.Value;
                }
            }
        }

        public static double CalculateStdDev(float?[] values)
        {
            var temp = values.Where(v => v.HasValue).Select(v => v!.Value).ToArray();
            return CalculateStdDev(temp);
        }

        public static double CalculateStdDev(float[] values)
        {
            double ret = 0;
            if (values.Length > 0)
            {
                //  计算平均数   
                double avg = values.Average();
                //  计算各数值与平均数的差值的平方，然后求和 
                double sum = values.Sum(d => Math.Pow(d - avg, 2));
                //  除以数量，然后开方
                ret = Math.Sqrt(sum / values.Length);
            }
            return ret;
        }
    }

    public record SampleArea(string SampleName, string[] DP, float?[] Area);
}

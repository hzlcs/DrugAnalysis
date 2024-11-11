using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChartEditLibrary.Model.AreaDatabase;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChartEditLibrary.Model
{
    public static class SampleManager
    {

        public static async Task<SampleResult> GetSampleAreasAsync(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException("未找到样品文件", fileName);
            using StreamReader sr = new(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            var text = (await sr.ReadToEndAsync()).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Split(',')).ToArray();
            var title = text[0];
            string description = text[1][0];
            var sampleAreas = new SampleArea[(title.Length - 1) / 6];
            var descriptions = text.Skip(2).Select(v => v[0][description.Length..]).ToArray();
            for (var i = 0; i < sampleAreas.Length; ++i)
            {
                sampleAreas[i] = new SampleArea(title[1 + i * 7], descriptions, text.Skip(2).Select(v =>
                {
                    var value = v[1 + i * 7 + 5];
                    return string.IsNullOrEmpty(value) ? null : new float?(float.Parse(value));
                }).ToArray());
            }
            return new SampleResult(description, sampleAreas);
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
            using var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(fileStream, Encoding.UTF8);
            var text = (await reader.ReadToEndAsync()).Replace("��n=3��", "(n=3)");
            try
            {
                string[][] lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Split(',')).ToArray();
                string[] sampleNames = lines[0].Skip(1).ToArray();
                string description = new string(lines[1][0].TakeWhile(v => !char.IsDigit(v)).ToArray());
                string[] descriptions = lines.Skip(1).Select(v =>
                {
                    var t = v[0];
                    return t[(t.IndexOf(description) + description.Length)..];

                }).ToArray();
                AreaRow[] rows = new AreaRow[descriptions.Length];
                for (var i = 0; i < descriptions.Length; i++)
                {
                    var areas = new float?[sampleNames.Length];
                    for (var j = 0; j < sampleNames.Length; j++)
                    {
                        if (float.TryParse(lines[i + 1][j + 1], out var value))
                        {
                            areas[j] = value;
                        }
                    }
                    rows[i] = new AreaRow(descriptions[i], areas);
                }
                return new AreaDatabase(Path.GetFileNameWithoutExtension(fileName), sampleNames, descriptions, rows);
            }
            catch (Exception ex)
            {
                throw new Exception("读取数据库失败:" + ex.Message);
            }

        }

        public static string[] MergeDescription(IEnumerable<string[]> descriptions)
        {
            var temp = descriptions.SelectMany(v => v).Distinct().ToList();
            var single = temp.Where(v => v is not null && !v.Contains('-')).ToArray();
            foreach (var s in single)
            {
                if (temp.Contains(s + "-1"))
                {
                    temp.Remove(s);
                }
            }
            string[] res = [.. temp];
            Array.Sort(res, DescriptionCompare);
            return res;
        }

        private static int DescriptionCompare(string l, string r)
        {
            var ls = l.Split('-').Select(int.Parse).ToArray();
            var rs = r.Split('-').Select(int.Parse).ToArray();
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
            if (left.Length == 0 || right.Length == 0 || left.Length + right.Length <= 2)
                return double.NaN;
            return TTest(right, left);
        }

        private static double TTest(float[] x, float[] y)
        {
            double sumX = x.Sum();
            double sumY = y.Sum();
            var n1 = x.Length;
            var n2 = y.Length;
            var meanX = sumX / n1;
            var meanY = sumY / n2;
            //double sumXminusMeanSquared = 0.0; // Calculate variances
            //double sumYminusMeanSquared = 0.0;
            //for (int i = 0; i < n1; ++i)
            //    sumXminusMeanSquared += (x[i] - meanX) * (x[i] - meanX);
            //for (int i = 0; i < n2; ++i)
            //    sumYminusMeanSquared += (y[i] - meanY) * (y[i] - meanY);

            var s1 = x.Sum(v => Math.Pow(v, 2)) - Math.Pow(sumX, 2) / n1;
            var s2 = y.Sum(v => Math.Pow(v, 2)) - Math.Pow(sumY, 2) / n2;
            //if (n1 == n2)
            //    se = Math.Sqrt(((n1 - 1) * sumXminusMeanSquared + (n2 - 1) * sumYminusMeanSquared) / (n1 + n2 - 2) * (1.0 / n1 + 1.0 / n2)) / 2;
            //else

            //double n = sumXminusMeanSquared / n1 < sumYminusMeanSquared / n2 ? n1 : n2;
            //se = Math.Sqrt(((n - 1) * sumXminusMeanSquared + (n - 1) * sumYminusMeanSquared) / (n1 + n2 - 2) * (1.0 / n1 + 1.0 / n2)) / 2;
            var se = Math.Sqrt((s1 + s2) / (n1 + n2 - 2) * (1.0 / n1 + 1.0 / n2));


            //double varX = sumXminusMeanSquared / (n1 - 1);
            //double varY = sumYminusMeanSquared / (n2 - 1);
            var top = (meanX - meanY);
            //double bot = Math.Sqrt((varX / n1) + (varY / n2));
            var t = top / se;
            //double num = ((varX / n1) + (varY / n2)) * ((varX / n1) + (varY / n2));
            //double denomLeft = ((varX / n1) * (varX / n1)) / (n1 - 1);
            //double denomRight = ((varY / n2) * (varY / n2)) / (n2 - 1);
            //double denom = denomLeft + denomRight;
            //double df = num / denom;
            double df = x.Length + y.Length - 2;
            var p = Student(t, df); // Cumulative two-tail density 
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
            var n = df; // to sync with ACM parameter name
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
        public string Description { get; }
        private readonly List<string> descriptions;
        public IReadOnlyList<string> Descriptions => descriptions;
        private IReadOnlyList<DescriptionString> DescriptionString { get; }
        public AreaRow[] Rows { get; }

        public AreaRow this[string description]
        {
            get
            {

                var index = this.descriptions.IndexOf(description);
                if (index == -1)
                {
                    throw new IndexOutOfRangeException("Description not found");
                }
                return Rows[index];
            }
        }

        public AreaDatabase(string className, string[] sampleNames, string[] descriptions, AreaRow[] rows)
        {
            this.ClassName = className;
            this.SampleNames = sampleNames;
            this.Description = new string(descriptions[0].TakeWhile(v => !char.IsDigit(v)).ToArray());
            this.descriptions = new List<string>(descriptions);
            this.DescriptionString = this.Descriptions.Select(v => new DescriptionString(v)).ToList();
            this.Rows = rows;
        }

        protected AreaDatabase(AreaDatabase database)
        {
            ClassName = database.ClassName;
            SampleNames = database.SampleNames;
            descriptions = database.descriptions;
            Description = database.Description;
            DescriptionString = database.DescriptionString;
            Rows = database.Rows;
        }

        public bool TryGetRow(string description, [MaybeNullWhen(false)] out AreaRow row)
        {
            DescriptionString descriptionString = new(description);
            for (int i = 0; i < DescriptionString.Count; i++)
            {
                if (DescriptionString[i] == descriptionString)
                {
                    row = Rows[i];
                    return true;
                }
            }
            row = null;
            return false;
        }



        public class AreaRow
        {
            public string Description { get; }
            public float?[] Areas { get; }
            public float? Average { get; }
            public double StdDev { get; }
            public double RSD { get; }

            public AreaRow(string description, float?[] areas)
            {
                this.Description = description;
                this.Areas = areas;
                var values = areas.Where(v => v.HasValue).Select(v => v!.Value).ToArray();
                if (values.Length <= 0)
                    return;
                Average = (float)Math.Round(values.Average(), 2);
                StdDev = CalculateStdDev(values);
                RSD = StdDev / Average.Value;
            }

            public string GetRange(float average)
            {
                var _range = Math.Round((average - Average.GetValueOrDefault()) / StdDev, 2);
                var range = _range switch
                {
                    > 0 => (int)Math.Ceiling(_range),
                    < 0 => (int)Math.Floor(_range),
                    _ => 0
                };
                return "AVG" + (range >= 0 ? "+" : "") + range + "SD";
            }
        }

        public static double CalculateStdDev(float?[] values)
        {
            var temp = values.Where(v => v.HasValue).Select(v => v!.Value).ToArray();
            return CalculateStdDev(temp);
        }

        public static double CalculateStdDev(float[] values)
        {
            if (values.Length <= 0)
                return 0;
            double ret = 0;
            //  计算平均数   
            double avg = values.Average();
            //  计算各数值与平均数的差值的平方，然后求和 
            var sum = values.Sum(d => Math.Pow(d - avg, 2));
            //  除以数量，然后开方
            ret = Math.Round(Math.Sqrt(sum / (values.Length - 1)), 2);
            return ret;
        }
    }

    public record SampleResult(string Description, SampleArea[] SampleAreas);

    public record SampleArea(string SampleName, string[] Descriptions, float?[] Area);
}

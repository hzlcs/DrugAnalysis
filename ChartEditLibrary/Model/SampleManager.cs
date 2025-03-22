using LanguageExt;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChartEditLibrary.Model.AreaDatabase;
using static System.Runtime.InteropServices.JavaScript.JSType;
using GluDescription = ChartEditLibrary.Model.DescriptionManager.GluDescription;

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
            var descriptions = text.Skip(2).Select(v => v[0].Replace(description, "")).ToArray();
            for (var i = 0; i < sampleAreas.Length; ++i)
            {
                sampleAreas[i] = new SampleArea(title[1 + i * 7], descriptions, text.Skip(2).Select(v =>
                {
                    var value = v[1 + i * 7 + 5];
                    return string.IsNullOrEmpty(value) ? null : new double?(double.Parse(value));
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
            var text = (await reader.ReadToEndAsync());
            try
            {
                string[][] lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Split(',')).ToArray();
                string[] sampleNames = lines[0].Skip(1).Select(v => v.Trim('\"').Replace("��n=3��", "(n=3)")).ToArray();
                string description = lines[0][0];
                if (string.IsNullOrWhiteSpace(description))
                {
                    if (lines[0][1].Contains(DescriptionManager.DP))
                        description = DescriptionManager.DP;
                    else
                        description = DescriptionManager.Glu;
                }
                string[] descriptions = lines.Skip(1).Select(v => v[0].Replace(description, "").Replace("A��", "A，").Replace("S��", "S，").Replace("1��6", "1，6").Replace("��", "Δ")).ToArray();
                descriptions = GluDescription.GetLongGluDescription(descriptions);
                AreaRow[] rows = new AreaRow[descriptions.Length];
                for (var i = 0; i < descriptions.Length; i++)
                {
                    var areas = new double?[sampleNames.Length];
                    for (var j = 0; j < sampleNames.Length; j++)
                    {
                        if (double.TryParse(lines[i + 1][j + 1], out var value))
                        {
                            areas[j] = value;
                        }
                    }
                    rows[i] = new AreaRow(descriptions[i], areas);
                }
                return new AreaDatabase(Path.GetFileNameWithoutExtension(fileName), sampleNames, description, descriptions, rows);
            }
            catch (Exception ex)
            {
                throw new Exception("读取数据库失败:" + ex.Message);
            }

        }

        public static SampleResult ChangeToGroup(SampleResult sampleResult)
        {
            if (sampleResult.Description != DescriptionManager.DP)
                return sampleResult;
            var sampleAreas = sampleResult.SampleAreas;
            SampleResult res = new SampleResult(sampleResult.Description, new SampleArea[sampleAreas.Length]);
            for (int i = 0; i < sampleAreas.Length; ++i)
            {
                var sampleArea = sampleAreas[i];
                var descriptions = sampleArea.Descriptions.Select(v => new DescriptionString(v)).ToArray();
                var areas = sampleArea.Area;

                (var newDesc, var newAreas) = DescriptionManager.ChangeToGroup(descriptions, areas);
                res.SampleAreas[i] = new SampleArea(sampleArea.SampleName, newDesc, newAreas);
            }
            return res;
        }

        public static AreaDatabase ChangeToGroup(AreaDatabase database)
        {
            if (database.Description != DescriptionManager.DP)
                return database;
            var descriptions = database.Descriptions.Select(v => new DescriptionString(v)).ToArray();
            var areas = database.Rows.Select(v => v.Areas).ToArray();
            (var newDesc, var newAreas) = DescriptionManager.ChangeToGroup(descriptions, areas);
            var rows = new AreaRow[newAreas.Length];
            for (int i = 0; i < rows.Length; ++i)
            {
                rows[i] = new AreaRow(newDesc[i], newAreas[i]);
            }
            return new AreaDatabase(database.ClassName, database.SampleNames, database.Description, newDesc, rows);
        }

        public static string[] MergeDescription(IEnumerable<string[]> descriptions, string description = "DP")
        {
            var temp = descriptions.SelectMany(v => v).Distinct().ToList();
            if (description == "DP")
            {
                var single = temp.Where(v => v is not null && !v.Contains('-')).ToArray();
                foreach (var s in single)
                {
                    if (temp.Contains(s + "-1"))
                    {
                        temp.Remove(s);
                    }
                }
            }
            string[] res = [.. temp];

            Array.Sort(res, description == "DP" ? DescriptionManager.DPComparer : DescriptionManager.GluComparer);
            return res;
        }

        public static double? TCheck(double[] left, double[] right)
        {
            if (left.Length == 0 || right.Length == 0 || left.Length + right.Length <= 2)
                return null;
            return TTest(right, left);
        }

        private static double TTest(double[] x, double[] y)
        {
            double sumX = x.Sum();
            double sumY = y.Sum();
            var n1 = x.Length;
            var n2 = y.Length;
            var meanX = sumX / n1;
            var meanY = sumY / n2;

            var s1 = x.Sum(v => Math.Pow(v, 2)) - Math.Pow(sumX, 2) / n1;
            var s2 = y.Sum(v => Math.Pow(v, 2)) - Math.Pow(sumY, 2) / n2;
            
            var se = Math.Sqrt((s1 + s2) / (n1 + n2 - 2) * (1.0 / n1 + 1.0 / n2));


            var top = (meanX - meanY);
            var t = top / se;
            double df = x.Length + y.Length - 2;
            var p = Student(t, df); // Cumulative two-tail density 
            return p;
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
        private readonly string[]? shortGluDescriptions;
        public IReadOnlyList<string> Descriptions => descriptions;
        private IReadOnlyList<DescriptionString>? DescriptionString { get; }
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

        public AreaDatabase(string className, string[] sampleNames, string description, string[] descriptions, AreaRow[] rows)
        {
            this.ClassName = className;
            this.SampleNames = sampleNames;
            this.Description = description;
            if (description == DescriptionManager.Glu)
            {
                shortGluDescriptions = GluDescription.GetShortGluDescription(descriptions);
                this.descriptions = GluDescription.GetLongGluDescription(descriptions).ToList();
            }
            else
            {
                this.descriptions = new List<string>(descriptions);
                this.DescriptionString = this.Descriptions.Select(v => new DescriptionString(v)).ToList();
            }

            this.Rows = rows;
        }

        protected AreaDatabase(AreaDatabase database)
        {
            ClassName = database.ClassName;
            SampleNames = database.SampleNames;
            descriptions = database.descriptions;
            shortGluDescriptions = database.shortGluDescriptions;
            Description = database.Description;
            DescriptionString = database.DescriptionString;
            Rows = database.Rows;
        }

        public bool TryGetRow(string description, [MaybeNullWhen(false)] out AreaRow row)
        {
            if (Description == DescriptionManager.Glu)
            {
                Debug.Assert(shortGluDescriptions is not null);
                string temp = GluDescription.GetShortGluDescription(description);
                int index = Array.IndexOf(shortGluDescriptions, temp);
                if (index == -1)
                {
                    row = null;
                    return false;
                }
                row = Rows[index];
                return true;
            }
            else
            {
                Debug.Assert(DescriptionString is not null);
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
        }



        public class AreaRow
        {
            public string Description { get; set; }
            public double?[] Areas { get; }
            public double? Average { get; }
            public double StdDev { get; }
            public double RSD { get; }

            public AreaRow(string description, double?[] areas)
            {
                this.Description = description;
                this.Areas = areas;
                var values = areas.Where(v => v.HasValue).Select(v => v!.Value).ToArray();
                if (values.Length <= 0)
                    return;
                Average = Math.Round(values.Average(), 2, MidpointRounding.AwayFromZero);
                StdDev = CalculateStdDev(values);
                RSD = StdDev / Average.Value;
            }

            public string GetRange(double average)
            {
                var _range = Math.Round((average - Average.GetValueOrDefault()) / StdDev, 2, MidpointRounding.AwayFromZero);
                var range = _range switch
                {
                    > 0 => Math.Round(_range, 2, MidpointRounding.AwayFromZero),
                    < 0 => Math.Round(_range, 2, MidpointRounding.AwayFromZero),
                    _ => 0
                };
                return "AVG" + (range >= 0 ? "+" : "") + range.ToString("F2") + "SD";
            }
        }

        public static double CalculateStdDev(double?[] values)
        {
            var temp = values.Where(v => v.HasValue).Select(v => v!.Value).ToArray();
            return CalculateStdDev(temp);
        }

        public static double CalculateStdDev(double[] values)
        {
            if (values.Length <= 0)
                return 0;
            double ret = 0;
            //  计算平均数   
            double avg = values.Average();
            //  计算各数值与平均数的差值的平方，然后求和 
            var sum = values.Sum(d => Math.Pow(d - avg, 2));
            //  除以数量，然后开方
            ret = Math.Round(Math.Sqrt(sum / (values.Length - 1)), 2, MidpointRounding.AwayFromZero);
            return ret;
        }
    }

    public record SampleResult(string Description, SampleArea[] SampleAreas);

    public record SampleArea(string SampleName, string[] Descriptions, double?[] Area);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Model
{
    public static class HotChartManager
    {
        public static async Task<SampleData?> Create(TwoDFileInfo fileInfo)
        {
            if (fileInfo.Extension == 0)
                return null;
            var data = await SampleManager.GetSampleAreasAsync(fileInfo.FilePath);
            int degree = fileInfo.Extension is null ? 0 : fileInfo.Extension.Value;
            SampleData res = new SampleData(fileInfo.SampleName, degree, data.SampleAreas[0].GetDescData());
            return res;
        }

        public static HotChartData MergeSampleData1(SampleData[] datas)
        {
            var main = datas.Where(v => v.Degree == 0).ToArray();
            var mainData = main.OrderBy(v => v.Degree).SelectMany(v => v.Datas).GroupBy(v => int.Parse(v.Description))
                .Where(v=>v.Key >= 4 && v.Key <= 10).Select(v => new DescData(v.Key.ToString(), v.Sum(x => x.Value.GetValueOrDefault()))).ToArray();

            var detail = datas.Where(v => v.Degree > 0).ToArray();

            Dictionary<string, Dictionary<int, DescData[]>> res = [];
            var samples = detail.GroupBy(x => x.SampleName).ToDictionary(v => v.Key, v => v.GroupBy(x => x.Degree).ToDictionary(x => x.Key, x => x.First().Datas.ToDictionary(x => x.Description)));
            var degrees = detail.Select(v => v.Degree).Distinct().Order().ToArray();
            var descriptions = detail.GroupBy(v => v.Degree).ToDictionary(v => v.Key, v => v.SelectMany(x => x.Datas).Select(x => x.Description).Distinct().Order(Numeric).ToArray());
            foreach (var sample in samples)
            {
                var degreeRes = new Dictionary<int, DescData[]>();
                res.Add(sample.Key, degreeRes);
                var degreeData = sample.Value;
                foreach (var d in degrees)
                {
                    var descs = descriptions[d];
                    if (!degreeData.TryGetValue(d, out var value))
                    {
                        degreeRes.Add(d, descs.Select(v => new DescData(v, null)).ToArray());
                    }
                    else
                    {

                        DescData[] temp = new DescData[descs.Length];
                        for (int i = 0; i < temp.Length; i++)
                        {
                            var desc = descs[i];
                            if (value.TryGetValue(desc, out var val))
                                temp[i] = val;
                            else
                                temp[i] = new DescData(desc, null);
                        }
                        degreeRes.Add(d, temp.ToArray());
                    }
                }
            }
            return new HotChartData(mainData, res, null!);
        }

        public static HotChartData MergeSampleData(SampleData[] datas)
        {
            var main = datas.Where(v => v.Degree == 0).ToArray();
            var mainData = main.OrderBy(v => v.Degree).SelectMany(v => v.Datas).GroupBy(v => int.Parse(v.Description))
                .Where(v => v.Key >= 4 && v.Key <= 10).Select(v => new DescData(v.Key.ToString(), v.Sum(x => x.Value.GetValueOrDefault()))).ToArray();
            Array.Sort(mainData, MainComparison);
            var detail = datas.Where(v => v.Degree > 0).ToArray();

            Dictionary<int, Dictionary<string, DescData[]>> res = [];
            var degrees = detail.GroupBy(x => x.Degree).ToDictionary(v => v.Key, v => v.GroupBy(x => x.SampleName).ToDictionary(x => x.Key, x => x.First().Datas.ToDictionary(x => x.Description)));
            var samples = detail.Select(v => v.SampleName).Distinct().ToArray();
            var degreeDesc = detail.GroupBy(v => v.Degree).ToDictionary(v => v.Key, v => v.SelectMany(x => x.Datas).Select(x => x.Description).Distinct().Order(Numeric).ToArray());

            foreach (var degree in degrees)
            {
                var sampleRes = new Dictionary<string, DescData[]>();
                res.Add(degree.Key, sampleRes);
                var sampleData = degree.Value;
                var descriptions = degreeDesc[degree.Key];
                foreach (var sample in samples)
                {
                    if (!sampleData.TryGetValue(sample, out var value))
                    {
                        sampleRes.Add(sample, descriptions.Select(v=>new DescData(v, null)).ToArray());
                    }
                    else
                    {
                        DescData[] temp = new DescData[descriptions.Length];
                        for(int i = 0; i < temp.Length; i++)
                        {
                            var desc = descriptions[i];
                            if (value.TryGetValue(desc, out var val))
                                temp[i] = val;
                            else
                                temp[i] = new DescData(desc, null);
                        }
                        sampleRes.Add(sample, temp);
                    }
                }
            }
            
            
            return new HotChartData(mainData, null!, res);
        }

        private static int MainComparison(DescData x, DescData y)
        {
            return int.Parse(x.Description) - int.Parse(y.Description);
        }

        private static List<DescData> GetDescData(this SampleArea area)
        {
            DescData[] res = new DescData[area.Descriptions.Length];
            for (int i = 0; i < area.Descriptions.Length; i++)
            {
                res[i] = new DescData(area.Descriptions[i], area.Area[i]);
            }
            return [.. res];
        }

        public record SampleData(string SampleName, int Degree, List<DescData> Datas);

        public record DescData(string Description, double? Value);

        private static readonly IComparer<string> Numeric = new NumericComparer();

        private class NumericComparer : IComparer<string>
        {
            private static readonly Dictionary<string, int> temp = [];

            public int Compare(string? x, string? y)
            {
                if (string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y))
                    return string.Compare(x, y);
                if (temp.TryGetValue(x, out int x1))
                {
                    if (char.IsDigit(x[0]))
                        x1 = int.Parse(x);
                    else
                        x1 = int.Parse(x[1..]);
                    temp.Add(x, x1);
                }
                if (temp.TryGetValue(y, out int y1))
                {
                    if (char.IsDigit(y[0]))
                        y1 = int.Parse(y);
                    else
                        y1 = int.Parse(y[1..]);
                    temp.Add(y, y1);
                }
                return x1.CompareTo(y1);
            }
        }

        public class HotChartData(DescData[] mainData, Dictionary<string, Dictionary<int, DescData[]>> datas
            , Dictionary<int, Dictionary<string, DescData[]>> datas1)
        {
            public Dictionary<string, Dictionary<int, DescData[]>> Datas { get; } = datas;

            public Dictionary<int, Dictionary<string, DescData[]>> Datas1 { get; } = datas1;

            public DescData[] MainData { get; } = mainData;
        }

        public record HotChartDataDetail(string Description, double area, double Scale, Dictionary<string, DescData[]> Datas);



    }
}

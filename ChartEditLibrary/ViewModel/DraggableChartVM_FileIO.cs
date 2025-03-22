using ChartEditLibrary.Entitys;
using ChartEditLibrary.Model;
using LanguageExt.Common;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.ViewModel
{
    public partial class DraggableChartVm
    {
        private static readonly char[] spe = [',', '\t'];

        private static async Task<(Coordinates[], string[][]?)> ReadCsv(string path, float start, float end)
        {
            string[][] data;
            var hasResult = false;
            var fileName = Path.GetFileNameWithoutExtension(path);
            List<string[]> saveContent = new List<string[]>();

            static string[]? GetSaveLine(string[] line, int index)
            {
                if (line.Length <= index || string.IsNullOrWhiteSpace(line[index]))
                    return null;
                return line.Skip(index).ToArray();
            }

            using (StreamReader sr = new(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                data = (await sr.ReadToEndAsync().ConfigureAwait(false)).Split(Environment.NewLine,
                    StringSplitOptions.RemoveEmptyEntries).Select(v => v.Split(spe)).ToArray();
            }

            try
            {
                var second = data[1] ?? throw new Exception("数据格式错误");
                int index = Array.IndexOf(second, "Peak");
                hasResult = index > 0;
                if (hasResult)
                {
                    for (var i = 0; i < data.Length; ++i)
                    {
                        var t = GetSaveLine(data[i], index);
                        if (t is null)
                            break;
                        saveContent.Add(t);
                    }
                }
                int dataRow = -1, dataCol = -1;
                for (int i = 0; i < data.Length; ++i)
                {
                    var row = data[i];
                    if (!double.TryParse(row[0], out double t))
                        continue;
                    dataRow = i;
                    if (row.Length > 2 && double.TryParse(row[2], out t))
                        dataCol = 1;
                    else
                        dataCol = 0;
                    break;
                }
                if (dataRow == -1)
                    throw new Exception();
                double[][] temp = data.Skip(dataRow)
                    .Select(v => v.Skip(dataCol).Take(2).Select(double.Parse).ToArray())
                    .Where(v => v[0] >= start && v[0] <= end).ToArray();
                return (temp.Select(v => new Coordinates(v[0], v[1])).ToArray(),
                    hasResult ? [.. saveContent] : null);
            }
            catch
            {
                throw new Exception(Path.GetFileNameWithoutExtension(path) + "数据格式错误");
            }
        }

        public async Task ApplyCuttingLine(string path)
        {
            using StreamReader sr = new(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            string txt = await sr.ReadToEndAsync();
            double[][] datas = txt.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Skip(2)
                .Select(v => v.Split(spe).Skip(1).Select(double.Parse).ToArray()).ToArray();
            double common = datas[0][1];
            List<CoordinateLine> lines = [];
            for (var i = 0; i < datas.Length; ++i)
            {
                var data = datas[i];
                if (data[1] == common)
                    continue;
                int j = i + 1;
                while (datas[j][1] != common)
                    ++j;
                var end = datas[j - 1];
                if (lines.Count > 0)
                {
                    var last = lines[^1];
                    if (data[0] - last.End.X < 0.1)
                    {
                        lines[^1] = new CoordinateLine(last.Start, new Coordinates(end[0], end[1]));
                        continue;
                    }
                }
                lines.Add(new CoordinateLine(data[0], data[1], end[0], end[1]));
            }
            ApplyCuttingLine(lines.ToArray());
        }

        public void ApplyCuttingLine(CoordinateLine[] lines)
        {
            CuttingLines = lines;
            if (SplitLines.Count > 0)
            {
                foreach (var i in SplitLines)
                {
                    i.SetCuttingData(CuttingLines, this);
                }
            }
        }

        public static async Task<DraggableChartVm> CreateAsync(string filePath, ExportType? exportType, string description
            , bool @new = false)
        {
            float start = 0, end = float.MaxValue;
            if (exportType is not null)
            {
                start = 20;
                end = 60;
            }
            if (exportType != ExportType.Enoxaparin)
            {
                start = 0;
                end = 100;
            }
            if (exportType == ExportType.TwoDimension)
            {
                (start, end) = TwoDConfig.Instance.D1;

            }
            if (exportType == ExportType.TwoDimensionDP)
            {
                (start, end) = TwoDConfig.Instance.GetRange(filePath);
            }
            var (dataSource, saveLine) = await ReadCsv(filePath, start, end).ConfigureAwait(false);

            var res = new DraggableChartVm(filePath, dataSource, exportType, description);
            if (!@new && saveLine != null)
                res.ApplyResult(saveLine);
            return res;
        }

        public static DraggableChartVm Create(CacheContent cache)
        {
            var dataSource = Enumerable.Range(0, cache.X.Length).Select(i => new Coordinates(cache.X[i], cache.Y[i]))
                .ToArray();
            ExportType? type = null;
            if (cache.ExportType is not null && Enum.TryParse<ExportType>(cache.ExportType, out var t))
                type = t;
            var vm = new DraggableChartVm(cache.FilePath, dataSource, type, cache.Description);
            vm.ApplyResult(cache.SaveContent);
            return vm;
        }

        internal void ApplyResult(string saveContent)
        {
            var lines = saveContent.Split(Environment.NewLine).Select(v => v.Split(',')).ToArray();
            ApplyResult(lines);
        }

        private string SaveDescription => Description == DescriptionManager.DP ? Description : "";

        private void ApplyResult(string[][] lines)
        {
            var baseInfo = lines[0];
            BaseLine[] baseLines = SaveManager.GetBaseLine(baseInfo);
            if (baseLines.Length == 0)
                return;
            try
            {
                foreach (var i in baseLines)
                {
                    AddBaseLine(i);
                }
                CurrentBaseLine = BaseLines[BaseLines.Count - 1];
                string desc = SaveDescription;
                for (var i = 2; i < lines.Length; i++)
                {
                    string[] data = lines[i];
                    var x = double.Parse(data[1]);
                    var point = GetChartPoint(x);
                    if (point.HasValue)
                    {
                        var line = AddSplitLine(point.Value);
                        if (!line.BaseLine.Include(line.Start.X))
                        {
                            if (Math.Abs(line.Start.X - line.BaseLine.Start.X) < Math.Abs(line.Start.X - line.BaseLine.End.X))
                                line.End = line.Start = line.BaseLine.Start;
                            else
                                line.End = line.Start = line.BaseLine.End;
                        }
                        line.Description = data[6][desc.Length..].TrimEnd('\r');
                        if (line.Description == "DP")
                            line.Description = "";
                    }
                }
            }
            catch
            {
                foreach (var b in BaseLines.ToArray())
                    RemoveBaseLine(b);
            }

            initialized = true;
        }
        /// <summary>
        /// 获取保存于文件的内容
        /// </summary>
        /// <returns></returns>
        public string GetSaveContent()
        {
            return string.Join(Environment.NewLine, GetSaveRowContent());
        }

        private string[] GetSaveRowContent()
        {
            ResetArea();
            var baseInfo =
                $"{FileName},{exportType},{SaveManager.GetBaseLineStr(BaseLines)},,,,";
            var title = $"{string.Join(',', GetSaveTitle())},{Description}";
            string description = Description == DescriptionManager.DP ? Description : "";
            IEnumerable<string> lines = SplitLines.Select(x =>
                $"{string.Join(',', x.GetSaveData())},{description}{x.Description}");
            return lines.Prepend(title).Prepend(baseInfo).ToArray();
        }

        public SaveRow[] GetSaveRow()
        {
            ResetArea();
            var baseLineStr = SaveManager.GetBaseLineStr(BaseLines);
            SaveRow baseInfo = new("",
                $"{FileName},{exportType},{baseLineStr},,,");
            SaveRow title = new("", string.Join(',', GetSaveTitle()));
            var lines = SplitLines.Select(x =>
                new SaveRow(x.Description!,
                    $"{string.Join(',', x.GetSaveData())}"));
            return lines.Prepend(title).Prepend(baseInfo).ToArray();
        }

        public async Task<Result<bool>> SaveToFile()
        {
            if (BaseLines.Count == 0)
                return new Result<bool>(true);
            ResetArea();
            foreach (var i in BaseLines)
            {
                foreach (var line in i.SplitLines)
                {
                    if (!i.Include(line.Start.X))
                    {
                        return new Result<bool>(new Exception($"样品'{FileName}'的分割线'x={line.Start.X:F2}'不在基线范围内!"));
                    }
                }
            }
            try
            {
                string[] save = GetSaveRowContent();
                string[] datas = File.ReadAllLines(FilePath);
                File.Delete(FilePath);
                using StreamWriter writer = new(File.Create(FilePath), Encoding.UTF8);
                bool hasResult = datas[1].Contains("Peak");
                int count = hasResult ? Array.IndexOf(datas[1].Split(spe), "Peak") - 2 : datas.Take(5).Max(v => v.Count(c => c == ','));
                for (var i = 0; i < datas.Length; ++i)
                {
                    var line = datas[i];
                    if (i < save.Length)
                    {
                        line = SaveManager.GetDataLine(line, count) + ",," + save[i];
                    }
                    else if (hasResult && i < 60)
                        line = SaveManager.GetDataLine(line);
                    await writer.WriteLineAsync(line);
                }

                return new Result<bool>(true);
            }
            catch (UnauthorizedAccessException ue)
            {
                return new Result<bool>(new UnauthorizedAccessException($"无法修改只读的源文件:'{FilePath}'", ue));
            }
            catch (IOException e)
            {
                return new Result<bool>(new IOException($"文件'{FilePath}'已被占用，请先关闭文件", e));
            }
        }

        private readonly static string[] commonTitle = ["Peak", "Start X", "Center X", "End X", "Area", "Area Sum %"];
        private readonly static string[] twoDTitle = ["Peak", "Center X", "Area Sum %", "ASP", "TSP", "offset%"];
        public string[] GetShowTitle()
        {
            if (exportType == ExportType.TwoDimension)
            {
                return twoDTitle;
            }
            else
            {
                return commonTitle;
            }
        }

        public string[] GetSaveTitle()
        {
            return commonTitle;
        }

        public string[][] GetShowData()
        {
            ResetArea();
            return SplitLines.Select(v => v.GetShowData()).ToArray();
        }

        public readonly struct SaveRow(string description, string line)
        {
            public readonly string description = description;
            public readonly string line = line;
        }

        class SaveManager
        {
            public static BaseLine[] GetBaseLine(string[] columns)
            {

                var baseLineStrs = columns[2];
                if (string.IsNullOrWhiteSpace(baseLineStrs))
                    baseLineStrs = columns[1];
                try
                {
                    if (!baseLineStrs.StartsWith('('))
                    {
                        double[] line = columns[2..6].Select(double.Parse).ToArray();
                        return [new BaseLine(new Coordinates(line[0], line[1]), new Coordinates(line[2], line[3]))];
                    }
                    return baseLineStrs.Split(';').Select(v =>
                    {
                        var temp = v.Split(':');
                        var start = temp[0][1..^1].Split(' ').Select(double.Parse).ToArray();
                        var end = temp[1][1..^1].Split(' ').Select(double.Parse).ToArray();
                        return new BaseLine(new Coordinates(start[0], start[1]),
                            new Coordinates(end[0], end[1]));
                    }).ToArray();
                }
                catch
                {
                    return [];
                }
            }

            public static string GetBaseLineStr(IEnumerable<BaseLine> baseLines)
            {
                return string.Join(";", baseLines.Select(x => $"({x.Start.X} {x.Start.Y}):({x.End.X} {x.End.Y})").ToArray());
            }

            public static string GetDataLine(string line)
            {
                int index = line.IndexOf(',', StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    return line;
                while (true)
                {
                    int next = line.IndexOf(',', index + 1);
                    if (next == -1)
                    {
                        return line;
                    }
                    int length = next - index;
                    if (length == 1 || (length == 2 && (line[index + 1] == '\t')))
                        return line[..index];
                    index = next;
                }
            }



            public static string GetDataLine(string line, int count)
            {
                int index = -1;
                while (true)
                {
                    index = line.IndexOf(',', index + 1);
                    if (index == -1)
                        break;
                    if (--count == 0)
                    {
                        index = line.IndexOf(',', index + 1);
                        if (index == -1)
                            return line;
                        return line[..index];
                    }
                }
                if (count > 0)
                    return line + new string(Enumerable.Repeat(',', count).ToArray());
                return line;
            }
        }
    }
}

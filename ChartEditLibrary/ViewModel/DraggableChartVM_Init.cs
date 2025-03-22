using ChartEditLibrary.Entitys;
using ChartEditLibrary.Model;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChartEditLibrary.Model.DescriptionManager;

namespace ChartEditLibrary.ViewModel
{
    public partial class DraggableChartVm
    {
        public void InitSplitLine(object? tag = null)
        {
            if (initialized)
                return;
            initialized = true;
            switch (exportType)
            {
                case ExportType.Standard:
                    InitSplitLine_Standard();
                    break;
                case ExportType.Enoxaparin:
                    InitSplitLine_YN(tag);
                    break;
                case ExportType.TwoDimension:
                    InitSplitLine_TwoD(tag);
                    break;
                case ExportType.Other:
                    InitSplitLine_Other();
                    break;
                default:
                    break;
            }

            if (SplitLines.Count > 0)
            {
                var lastLine = SplitLines.Last();
                var y = lastLine.BaseLine.End.Y;
                var last = GetDateSourceIndex(lastLine.End.X);
                for (var i = last; i < DataSource.Length; ++i)
                {
                    if (DataSource[i].Y < y)
                    {
                        lastLine.BaseLine.End = new Coordinates(DataSource[i].X, DataSource[i].Y);
                        UpdateBaseLine(lastLine.BaseLine);
                        break;
                    }
                }
            }
        }

        private void InitSplitLine_Standard()
        {
            CurrentBaseLine = AddBaseLine(GetDefaultBaseLine());
            GetPoints(DataSource, 0, DataSource.Length - 1, out var minDots, out var maxDots);
            List<int> lines = [];
            for (int i = 0; i < minDots.Length; i++)
            {
                int min = minDots[i];
                int max = maxDots[i];
                if (lines.Count == 0)
                {
                    if (DataSource[max].Y < 10)
                        continue;
                    lines.Add(min);
                }
                else
                {
                    int preMin = minDots[i - 1];
                    if (DataSource[max].Y - DataSource[preMin].Y < 5)
                        continue;
                    lines.Add(min);
                }
            }
            for (int i = 0; i < lines.Count; ++i)
            {
                AddSplitLine(CurrentBaseLine, DataSource[lines[i]]).Description = GluDescription.StdDescriptions[i];
            }
        }

        private void InitSplitLine_Other()
        {
            CurrentBaseLine = GetDefaultBaseLine();
            BaseLines.Add(CurrentBaseLine);
            var perVaule = yMax.Y / 100;
            //double m = dataSource.Take(dataSource.Length / 2).Min(v => v.Y);
            //for (int i = 0; i < dataSource.Length; i++)
            //    dataSource[i].Y -= m;
            var end = DataSource.Length / 3 * 2;
            while (end < DataSource.Length - 1 && DataSource[end].Y > 0)
                end++;
            var config = Config.GetConfig(exportType);
            List<int> maxDots = [];
            List<int> minDots = [];
            GetPoints(DataSource, 0, end, out var _minDots, out var _maxDots);
            for (var i = 0; i < _minDots.Length; i++)
            {
                var max = _maxDots[i];
                var min = _minDots[i];
                if (DataSource[max].X <= config.XMin)
                    continue;
                if (DataSource[max].X >= config.XMax)
                    break;
                if (minDots.Count == 0 && DataSource[max].Y < config.FirstYMin * perVaule)
                    continue;
                if (DataSource[max].Y < config.YMin * perVaule)
                    continue;
                if (DataSource[max].Y - DataSource[min].Y < config.MinHeight * perVaule)
                    continue;
                if (minDots.Count > 0 && DataSource[max].Y - DataSource[minDots[^1]].Y < config.MinHeight * perVaule)
                    continue;
                if (DataSource[min].X <= CurrentBaseLine.Start.X)
                    continue;

                maxDots.Add(max);
                minDots.Add(min);

                //if (maxDots.Count > 1 && DataSource[max].X - DataSource[maxDots[^2]].X < 0.3) //若两个顶点峰太近,移除后一个
                //    maxDots.Remove(max);
            }

            foreach (var i in minDots)
            {
                AddSplitLine(CurrentBaseLine, DataSource[i]);
            }
        }

        internal void GenerateSplitLine(BaseLine baseLine)
        {
            int start = GetDateSourceIndex(baseLine.Start.X) + 1;
            int end = GetDateSourceIndex(baseLine.End.X) - 1;

            //Coordinates[] range = new Coordinates[end - start + 1];
            //for (int i = 0; i < range.Length; i++)
            //{
            //    Coordinates point = DataSource[start + i];
            //    range[i] = new Coordinates(point.X, point.Y - baseLine.GetY(point.X));
            //}
            //int startRange = 1;
            //for (; startRange < range.Length; ++startRange)
            //{
            //    if (range[startRange].Y > range[startRange - 1].Y)
            //        break;
            //}
            //GetPoints(range, startRange, range.Length, out var minDots, out var maxDots);



            List<int> tPoints = [];

            for (int index = 0; index < minDots.Length; ++index)
            {
                int i = minDots[index];
                if(i <= start || i >= end)
                    continue;
                var point = DataSource[i];
                if (point.Y < MutiConfig.Instance.MinHeight || baseLine.SplitLines.Any(v => Math.Abs(v.Start.X - point.X) < Unit * 5))
                    continue;
                AddSplitLine(baseLine, point);
            }
        }

        private void InitSplitLine_YN(object? tag)
        {
            CurrentBaseLine = GetDefaultBaseLine();
            BaseLines.Add(CurrentBaseLine);
            var perVaule = yMax.Y / 100;
            //double m = dataSource.Take(dataSource.Length / 2).Min(v => v.Y);
            //for (int i = 0; i < dataSource.Length; i++)
            //    dataSource[i].Y -= m;
            var end = DataSource.Length / 3 * 2;
            while (end < DataSource.Length - 1 && DataSource[end].Y > 0)
                end++;
            var config = Config.GetConfig(exportType);
            var maxDots = new List<int>();
            var minDots = new List<int>();
            GetPoints(DataSource, 0, end, out var _minDots, out var _maxDots);

            for (var i = 0; i < _minDots.Length; i++)
            {
                var max = _maxDots[i];
                var min = _minDots[i];

                if (DataSource[max].X <= config.XMin)
                    continue;
                if (DataSource[max].X >= config.XMax)
                    break;
                if (minDots.Count == 0 && DataSource[max].Y < config.FirstYMin * perVaule)
                    continue;
                if (DataSource[max].Y < config.YMin * perVaule)
                    continue;
                if (DataSource[max].Y - DataSource[min].Y < config.MinHeight * perVaule)
                    continue;
                if (minDots.Count > 0 && DataSource[max].Y - DataSource[minDots[^1]].Y < config.MinHeight * perVaule)
                    continue;

                maxDots.Add(max);
                minDots.Add(min);

                //if (maxDots.Count > 1 && DataSource[max].X - DataSource[maxDots[^2]].X < 0.3) //若两个顶点峰太近,移除后一个
                //    maxDots.Remove(max);
            }

            var dp6Index = maxDots.IndexOf(highestIndex);
            Debug.Assert(dp6Index != -1);
            if (4.Equals(tag))
            {
                if (DataSource[maxDots[dp6Index]].Y - DataSource[minDots[dp6Index - 1]].Y > 10)
                {
                    dp6Index -= 2;
                }
                else
                {
                    dp6Index -= 1;
                }
            }

            //dp6
            var startMin = dp6Index - 1;
            var endMin = dp6Index;
            if (DataSource[maxDots[dp6Index]].Y - DataSource[minDots[dp6Index - 1]].Y < 10) //若左边还有一个顶点峰
            {
                --startMin;
            }
            else if (DataSource[maxDots[dp6Index]].Y - DataSource[minDots[dp6Index]].Y < 10) //若右边还有一个顶点峰
            {
                ++endMin;
            }

            if (DataSource[maxDots[endMin]].X - DataSource[minDots[endMin]].X > -0.7)
            {
                ++endMin;
            }

            var dp6Start = minDots[endMin];
            var dp6End = minDots[startMin];
            var peakCount = endMin - startMin;
            if (peakCount != 3)
            {
                var startT = minDots[startMin];
                var endT = minDots[endMin];
                var t = GetT(DataSource, startT, endT);
                GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                for (var i = 0; i < _minDots.Length; ++i)
                {
                    int? add = null;
                    if (t[_maxDots[i]].Y < 2)
                    {
                        if ((endT - startT - _maxDots[i]) * Unit > 0.3)
                        {
                            add = startT + _maxDots[i];
                        }
                    }
                    else if (t[_minDots[i]].Y > -2.5)
                    {
                        add = startT + _minDots[i];
                    }

                    if (add.HasValue)
                    {
                        if (minDots.Contains(add.Value))
                            continue;
                        minDots.Add(add.Value);
                        maxDots.Add(add.Value);
                        ++endMin;
                        if (add.Value > dp6Start)
                        {
                            dp6Start = add.Value;
                        }
                    }
                }
            }

            minDots.Sort();
            maxDots.Sort();
            //dp4
            startMin = endMin;
            endMin = startMin + 1;
            if (DataSource[maxDots[endMin]].Y < DataSource[minDots[endMin]].Y * 3.5)
            {
                ++endMin;
            }
            else if (DataSource[maxDots[endMin]].X - DataSource[maxDots[endMin + 1]].X > -0.7)
                ++endMin;
            if (DataSource[minDots[endMin + 1]].X - DataSource[minDots[endMin]].X < 0.5)
                ++endMin;
            int dp4Start;
            if (DataSource[minDots[endMin + 1]].X - DataSource[minDots[endMin]].X < 1)
            {
                dp4Start = minDots[endMin];
                peakCount = endMin - startMin;
                if (peakCount != 2)
                {
                    var startT = minDots[startMin];
                    var endT = minDots[endMin];
                    var t = GetT(DataSource, startT, endT);
                    GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                    for (var i = 0; i < _minDots.Length; ++i)
                    {
                        if (t[_maxDots[i]].Y < 0 || t[_minDots[i]].Y > 0)
                        {
                            var add = startT + _maxDots[i];
                            if (minDots.Contains(add))
                                continue;
                            minDots.Add(add);
                            maxDots.Add(add);
                            ++endMin;
                            if (add > dp4Start)
                            {
                                dp4Start = add;
                            }
                        }
                    }
                }
            }
            else
            {
                if (DataSource[maxDots[endMin]].Y - DataSource[minDots[endMin]].Y * 5 < 0)
                    ++endMin;
                dp4Start = minDots[endMin];
                var startT = maxDots[endMin];
                var endT = minDots[endMin];
                    
                var t = GetT(DataSource, startT, endT);
                GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                for (var i = 0; i < _minDots.Length; ++i)
                {
                    if (t[_maxDots[i]].Y < 0 || t[_minDots[i]].Y > 0)
                    {
                        var add = startT + _maxDots[i];
                        if (minDots.Contains(add) || maxDots.Contains(add))
                            continue;
                        minDots.Add(add);
                        maxDots.Add(add);
                        ++endMin;
                        if (endMin - startMin == 4)
                        {
                            --endMin;
                            dp4Start = add;
                            break;
                        }
                    }
                }
            }
            minDots.Sort();
            maxDots.Sort();



            //dp3
            startMin = endMin;
            endMin = startMin + 1;
            if (DataSource[minDots[endMin]].X - DataSource[minDots[startMin]].X < 1)
            {
                ++endMin;
            }

            var dp3Start = minDots[endMin];
            peakCount = endMin - startMin;
            if (peakCount != 2)
            {
                var startT = minDots[startMin];
                var endT = minDots[endMin];
                var t = GetT(DataSource, startT, endT);
                GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                for (var i = 0; i < _maxDots.Length; ++i)
                {
                    if (t[_maxDots[i]].Y < 0 || t[_minDots[i]].Y > 0)
                    {
                        var add = startT + _maxDots[i];
                        if (minDots.Contains(add))
                            continue;
                        minDots.Add(add);
                        maxDots.Add(add);
                        ++endMin;
                        if (add > dp3Start)
                        {
                            dp3Start = add;
                        }

                        break;
                    }
                }
            }

            minDots.Sort();
            maxDots.Sort();

            //dp2
            startMin = endMin;
            endMin = minDots.Count - 1;
            var dp2Start = minDots[endMin];
            peakCount = endMin - startMin;
            if (peakCount > 2)
            {
                minDots.RemoveAt(minDots.Count - 1);
                maxDots.RemoveAt(maxDots.Count - 1);
                --endMin;
                dp2Start = minDots[endMin];
            }

            minDots.Sort();
            maxDots.Sort();
            var dp = 2;
            var sort = 1;
            foreach (var i in minDots.Distinct().Reverse())
            {
                if (i <= dp6End)
                {
                    dp += 2;
                    sort = 1;
                }
                else if (i <= dp6Start)
                {
                    if (dp != 6)
                    {
                        dp = 6;
                        sort = 1;
                    }
                }
                else if (i <= dp4Start)
                {
                    if (dp != 4)
                    {
                        dp = 4;
                        sort = 1;
                    }
                }
                else if (i <= dp3Start)
                {
                    if (dp != 3)
                    {
                        dp = 3;
                        sort = 1;
                    }
                }

                AddSplitLine(CurrentBaseLine, DataSource[i]).Description = dp + "-" + sort;
                ++sort;
            }

            foreach (var group in SplitLines.GroupBy(v => v.Description![..^2]).ToArray())
            {
                var lines = group.ToArray();
                if (lines.Length == 1)
                {
                    lines[0].Description = group.Key;
                }
            }
        }

        private void InitSplitLine_TwoD(object? tag = null)
        {
            CurrentBaseLine = GetDefaultBaseLine();
            BaseLines.Add(CurrentBaseLine);
            var perVaule = yMax.Y / 100;
            //double m = dataSource.Take(dataSource.Length / 2).Min(v => v.Y);
            //for (int i = 0; i < dataSource.Length; i++)
            //    dataSource[i].Y -= m;
            var end = DataSource.Length / 3 * 2;
            while (end < DataSource.Length - 1 && DataSource[end].Y > 0)
                end++;
            var maxDots = new List<int>();
            var minDots = new List<int>();
            GetPoints(DataSource, 0, end, out var _minDots, out var _maxDots);

            var config = TwoDConfig.Instance;
            for (var i = 0; i < _minDots.Length; i++)
            {
                var max = _maxDots[i];
                var min = _minDots[i];

                if (DataSource[max].X <= config.PeakRange.Start)
                    continue;
                if (DataSource[max].X >= config.PeakRange.End)
                    break;
                if (DataSource[max].Y < config.YMin * perVaule)
                    continue;
                if (DataSource[max].Y - DataSource[min].Y < config.MinHeight * perVaule)
                    continue;
                

                maxDots.Add(max);
                minDots.Add(min);

                //if (maxDots.Count > 1 && DataSource[max].X - DataSource[maxDots[^2]].X < 0.3) //若两个顶点峰太近,移除后一个
                //    maxDots.Remove(max);
            }

            var dp6Index = maxDots.IndexOf(highestIndex);
            Debug.Assert(dp6Index != -1);
            if (4.Equals(tag))
            {
                if (DataSource[maxDots[dp6Index]].Y - DataSource[minDots[dp6Index - 1]].Y > 10)
                {
                    dp6Index -= 2;
                }
                else
                {
                    dp6Index -= 1;
                }
            }
            int peakCount;

            //dp6
            var startMin = dp6Index - 1;
            var endMin = dp6Index;

            var dp6Start = minDots[endMin];
            var dp6End = minDots[startMin];

            minDots.Sort();
            maxDots.Sort();
            //dp4
            startMin = endMin;
            endMin = startMin + 1;
            if (DataSource[maxDots[endMin]].Y < DataSource[minDots[endMin]].Y * 3.5)
            {
                ++endMin;
            }
            if (DataSource[minDots[endMin + 1]].X - DataSource[minDots[endMin]].X < 0.5)
                ++endMin;
            int dp4Start = 0;
            if (DataSource[minDots[endMin]].X - DataSource[minDots[startMin]].X > 0.9)
            {
                var startT = minDots[startMin];
                var endT = minDots[endMin];
                var t = GetT(DataSource, startT, endT);
                GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                for (var i = 0; i < _minDots.Length; ++i)
                {
                    if ((t[_maxDots[i]].Y < 0 && t[_minDots[i - 1]].Y < t[_maxDots[i]].Y * 4) || t[_minDots[i]].Y > 0)
                    {
                        var add = startT + _maxDots[i];
                        if (minDots.Contains(add))
                            continue;
                        minDots.Add(add);
                        maxDots.Add(add);
                        if(dp4Start == 0)
                            dp4Start = add;
                    }
                }
            } //dp4单顶点峰，且dp3无顶点峰
            else if (DataSource[minDots[endMin + 1]].X - DataSource[minDots[endMin]].X < 1)
            {
                dp4Start = minDots[endMin];
                peakCount = endMin - startMin;
                if (peakCount != 2)
                {
                    var startT = minDots[startMin];
                    var endT = minDots[endMin];
                    var t = GetT(DataSource, startT, endT);
                    GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                    for (var i = 0; i < _minDots.Length; ++i)
                    {
                        if ((t[_maxDots[i]].Y < 0 && t[_minDots[i - 1]].Y < t[_maxDots[i]].Y * 4)|| t[_minDots[i]].Y > 0)
                        {
                            var add = startT + _maxDots[i];
                            if (minDots.Contains(add))
                                continue;
                            minDots.Add(add);
                            maxDots.Add(add);
                            ++endMin;
                            if (add > dp4Start)
                            {
                                dp4Start = add;
                            }
                        }
                    }
                }
            }
            else
            {
                if (DataSource[maxDots[endMin]].Y - DataSource[minDots[endMin]].Y * 5 < 0)
                    ++endMin;
                dp4Start = minDots[endMin];
                var startT = maxDots[endMin];
                var endT = minDots[endMin];

                var t = GetT(DataSource, startT, endT);
                GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                for (var i = 0; i < _minDots.Length; ++i)
                {
                    if ((t[_maxDots[i]].Y < 0 && t[_minDots[i - 1]].Y < t[_maxDots[i]].Y * 4) || t[_minDots[i]].Y > 0)
                    {
                        var add = startT + _maxDots[i];
                        if (minDots.Contains(add) || maxDots.Contains(add))
                            continue;
                        minDots.Add(add);
                        maxDots.Add(add);
                        ++endMin;
                        if (endMin - startMin == 4)
                        {
                            --endMin;
                            dp4Start = add;
                            break;
                        }
                    }
                }
            }
            minDots.Sort();
            maxDots.Sort();



            //dp3
            startMin = endMin;
            endMin = startMin + 1;
            if (DataSource[minDots[endMin]].X - DataSource[minDots[startMin]].X < 0.5)
            {
                ++endMin;
            }

            var dp3Start = minDots[endMin];
            peakCount = endMin - startMin;
            if (peakCount != 2)
            {
                var startT = minDots[startMin];
                var endT = minDots[endMin];
                var t = GetT(DataSource, startT, endT);
                GetPoints(t, 0, t.Length, out _minDots, out _maxDots);
                for (var i = 0; i < _maxDots.Length; ++i)
                {
                    if (t[_maxDots[i]].Y < 0 || t[_minDots[i]].Y > 0)
                    {
                        var add = startT + _maxDots[i];
                        if (minDots.Contains(add))
                            continue;
                        minDots.Add(add);
                        maxDots.Add(add);
                        ++endMin;
                        if (add > dp3Start)
                        {
                            dp3Start = add;
                        }

                        break;
                    }
                }
            }

            minDots.Sort();
            maxDots.Sort();

            //dp2
            startMin = endMin;
            endMin = minDots.Count - 1;
            var dp2Start = minDots[endMin];
            peakCount = endMin - startMin;
            if (peakCount > 2)
            {
                minDots.RemoveAt(minDots.Count - 1);
                maxDots.RemoveAt(maxDots.Count - 1);
                --endMin;
                dp2Start = minDots[endMin];
            }

            minDots.Sort();
            maxDots.Sort();
            var dp = 2;
            var sort = 1;
            foreach (var i in minDots.Distinct().Reverse())
            {
                if (i <= dp6End)
                {
                    dp += 2;
                    sort = 1;
                }
                else if (i <= dp6Start)
                {
                    if (dp != 6)
                    {
                        dp = 6;
                        sort = 1;
                    }
                }
                else if (i <= dp4Start)
                {
                    if (dp != 4)
                    {
                        dp = 4;
                        sort = 1;
                    }
                }
                else if (i <= dp3Start)
                {
                    if (dp != 3)
                    {
                        dp = 3;
                        sort = 1;
                    }
                }

                AddSplitLine(CurrentBaseLine, DataSource[i]).Description = dp + "-" + sort;
                ++sort;
            }

            foreach (var group in SplitLines.GroupBy(v => v.Description![..^2]).ToArray())
            {
                var lines = group.ToArray();
                if (lines.Length == 1)
                {
                    lines[0].Description = group.Key;
                }
                else
                {
                    if (exportType == ExportType.TwoDimension)
                    {
                        for (int i = 0; i < lines.Length - 1; ++i)
                            RemoveSplitLine(lines[i]);
                        lines[lines.Length - 1].Description = group.Key;
                    }
                }
            }
        }

        internal bool CheckCrossBaseLine(BaseLine baseLine, out Coordinates point)
        {
            if(IsVstreetEndPoint(baseLine.Start, 0) && IsVstreetEndPoint(baseLine.End, 0))
            {
                CoordinateLine line = baseLine.Line;
                CheckCrossedBaseline(ref line);
                if(line.Start != baseLine.Start)
                {
                    point = line.Start;
                    return true;
                }
            }
            point = default;
            return false;
        }
    }
}

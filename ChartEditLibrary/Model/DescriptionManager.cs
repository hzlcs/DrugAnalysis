﻿using LanguageExt;
using LanguageExt.Pipes;
using MathNet.Numerics;
using System.Diagnostics;

namespace ChartEditLibrary.Model
{
    public static class DescriptionManager
    {
        public const string Glu = "assignment";
        public const string DP = "DP";
        public const string COM = "component";

        public static IComparer<string> GluComparer => GluDescription.Comparer;
        public static IComparer<string> DPComparer => DPDescription.Comparer;
        public static IComparer<string> ComComparer => ComDescription.Comparer;


        public static class GluDescription
        {
            public static readonly string[] StdDescriptions = [S1, S2, S3, S4, S5, S6, S7, S8];
            public static readonly string[] Descriptions = [
                "",
            S1_1,
            S1,
            S2_1,
            S2_2,
            S2,
            S3,
            S4,
            S5_1,
            S5,
            S6,
            S7_1,
            S7,
            S8_1,
            S8_2,
            S8,
            f,
            g,
            h,
            i,
            j,
            k,
            ];
            private static readonly string[] ShortDescriptions;
            public static IComparer<string> Comparer { get; } = new GluComparer();

            static GluDescription()
            {
                ShortDescriptions = Descriptions.Select(v =>
                {
                    int index = v.IndexOf('(');
                    if (index == -1)
                        return v;
                    var end = v.IndexOf(')') + 1;
                    return v[..index] + v[end..];
                }).ToArray();
            }
            #region Descriptions
            public const string S1_1 = "ΔGlyser(ΔUA-Gal-Gal-Xyl-O-Ser)";
            public const string S1 = "S1(ΔIVA)";
            public const string S2_1 = "ΔGlyser ox2(ΔUA-Gal-Gal-O-Ser ox)";
            public const string S2_2 = "ΔGlyser ox1(ΔUA-Gal-Gal-Xyl-O-Ser ox)";
            public const string S2 = "S2(ΔIVS)";
            public const string S3 = "S3(ΔIIA)";
            public const string S4 = "S4(ΔIIIA)";
            public const string S5 = "S5(ΔIIS)";
            public const string S5_1 = "ΔIIS-gal(ΔGalA-GlcNS，6S)";
            public const string S6 = "S6(ΔIIIS)";
            public const string S7_1 = "[1;0;0;1;0;2]";
            public const string S7 = "S7(ΔIA)";
            public const string S8_1 = "[1;1;1;0;0;2];[1;1;1;1;1;3]";
            public const string S8_2 = "[1;1;1;0;0;2]";
            public const string S8 = "S8(ΔIS)";
            public const string f = "[1;1;2;0;0;4]";
            public const string g = "ΔIIA-IIS-glu(ΔUA-GlcNAc6S-GlcA-GlcNS，3S，6S)";
            public const string h = "ΔIS-IdoA2S(ΔUA2S-GlcNS6S-IdoA2S);[1;1;1;1;0;4]";
            public const string i = "1，6-AnhydroΔIS-IS(ΔUA，2S-GlcNS6S-UA2S-1，6-AnhydroGlcNS)";
            public const string j = "[1;1;2;0;1;4]";
            public const string k = "[1;1;2;0;0;6]";
            #endregion

            private class GluComparer : IComparer<string>
            {
                public int Compare(string? x, string? y)
                {
                    if (string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y))
                        return string.Compare(x, y);
                    int xIndex = Array.IndexOf(ShortDescriptions, x);
                    if (xIndex == -1)
                        xIndex = Array.IndexOf(Descriptions, x);

                    int yIndex = Array.IndexOf(ShortDescriptions, y);
                    if (yIndex == -1)
                        yIndex = Array.IndexOf(Descriptions, y);

                    return xIndex.CompareTo(yIndex);
                }
            }

            public static string[] GetShortGluDescription(string[] description)
            {
                var result = new string[description.Length];
                for (int i = 0; i < description.Length; ++i)
                {
                    int index = Array.IndexOf(Descriptions, description[i]);
                    if (index == -1)
                        result[i] = description[i];
                    else
                        result[i] = ShortDescriptions[index];
                }
                return result;
            }

            public static string[] GetLongGluDescription(string[] description)
            {
                var result = new string[description.Length];
                for (int i = 0; i < description.Length; ++i)
                {
                    int index = Array.IndexOf(ShortDescriptions, description[i]);
                    if (index == -1)
                        result[i] = description[i];
                    else
                        result[i] = Descriptions[index];
                }
                return result;
            }

            public static string GetShortGluDescription(string description)
            {
                int index = Array.IndexOf(Descriptions, description);
                if (index == -1)
                    return description;
                return ShortDescriptions[index];
            }

        }

        public static class DPDescription
        {
            public static IComparer<string> Comparer { get; } = new DPComparer();

            private class DPComparer : IComparer<string>
            {
                int IComparer<string>.Compare(string? l, string? r)
                {
                    if (string.IsNullOrEmpty(l) || string.IsNullOrEmpty(r))
                        return string.Compare(l, r);
                    if (l.EndsWith("SUM"))
                        l = l[..^4];
                    if (r.EndsWith("SUM"))
                        r = r[..^4];
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
            }
        }

        public static class ComDescription
        {
            public static IComparer<string> Comparer { get; } = new ComComparer();

            public static string GetDescriptionStart(string fileName)
            {
                int degree = 0;
                int index = fileName.LastIndexOf('-');
                if (index > 0)
                {
                    string temp = fileName[(index + 1)..].ToLower();
                    degree = int.Parse(temp.Substring(2));
                }
                return GetDescriptionStart(degree);
            }

            public static int GetSortStart(string fileName)
            {
                int degree = 0;
                int index = fileName.LastIndexOf('-');
                if (index > 0)
                {
                    string temp = fileName[(index + 1)..].ToLower();
                    degree = int.Parse(temp.Substring(2));
                }
                return GetSortStart(degree);
            }

            private static int GetSortStart(int degree)
            {
                return SortStart[degree];
            }

            private static readonly Dictionary<int, string> DescriptionStart = new()
            {
                { 4, "a" },
                { 6, "b" },
                { 8, "c" },
                { 10, "d" },
            };

            private static readonly Dictionary<int, int> SortStart = new()
            {
                { 4, 7 },
                { 6, 5 },
                { 8, 6 },
                { 10, 1 },
            };

            public static string GetDescriptionStart(int degree)
            {
                return DescriptionStart[degree];
            }

            private class ComComparer : IComparer<string>
            {
                public int Compare(string? x, string? y)
                {
                    if (x is null || y is null)
                    {
                        return string.Compare(x, y);
                    }
                    if (x[0] == y[0])
                    {
                        return int.Parse(x[1..]) - int.Parse(y[1..]);
                    }
                    return x[0] - y[0];
                }
            }
        }



        public static void Sort(string[] descriptions, string description)
        {
            if (description == Glu)
                Array.Sort(descriptions, GluComparer);
            else if (description == DP)
                Array.Sort(descriptions, DPComparer);
            else if (description == COM)
                Array.Sort(descriptions, ComComparer);
            else
                Array.Sort(descriptions);
        }


        public static (string[] descriptions, double?[] areas) ChangeToGroup(DescriptionString[] descriptions, double?[] areas)
        {
            Debug.Assert(descriptions.Length == areas.Length);
            List<string> ds = [];
            List<double?> newAreas = [];
            for (int i = 0; i < descriptions.Length; ++i)
            {
                var desc = descriptions[i];
                int end = i + 1;
                while (end < descriptions.Length && desc.Degree == descriptions[end].Degree)
                {
                    ++end;
                }
                if (end == i + 1)
                {
                    ds.Add(desc.Degree);
                    newAreas.Add(areas[i]);
                }
                else
                {
                    double? sum = 0;
                    for (int j = i; j < end; ++j)
                    {
                        sum += areas[j];
                        ds.Add(descriptions[j].Description);
                        newAreas.Add(areas[j]);
                    }
                    ds.Add(desc.Degree + "-SUM");
                    newAreas.Add(sum);
                    i = end - 1;
                }
            }
            return (ds.ToArray(), newAreas.ToArray());
        }

        public static (string[] descriptions, double?[][] areas) ChangeToGroup(DescriptionString[] descriptions, double?[][] areas)
        {
            Debug.Assert(descriptions.Length == areas.Length);
            List<string> ds = [];
            List<double?[]> newAreas = [];
            for (int i = 0; i < descriptions.Length; ++i)
            {
                var desc = descriptions[i];
                int end = i + 1;
                while (end < descriptions.Length && desc.Degree == descriptions[end].Degree)
                {
                    ++end;
                }
                if (end == i + 1)
                {
                    ds.Add(desc.Degree);
                    newAreas.Add(areas[i]);
                }
                else
                {

                    double?[] sum = Enumerable.Repeat((double?)0, areas[i].Length).ToArray();
                    for (int j = i; j < end; ++j)
                    {
                        for (int k = 0; k < sum.Length; ++k)
                        {
                            sum[k] += areas[j][k].GetValueOrDefault();
                        }
                        ds.Add(descriptions[j].Description);
                        newAreas.Add(areas[j]);
                    }
                    ds.Add(desc.Degree + "-SUM");
                    newAreas.Add(sum);
                    i = end - 1;
                }
            }
            return (ds.ToArray(), newAreas.ToArray());
        }
    }
}
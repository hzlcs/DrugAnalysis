using ChartEditLibrary.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Entitys
{
    public class TwoDConfig
    {
        public class Range(int start, int end)
        {
            public int Start { get; set; } = start;
            public int End { get; set; } = end;

            internal void Deconstruct(out float start, out float end)
            {
                start = Start;
                end = End;
            }

            public override string ToString()
            {
                return "Range";
            }
        }

        public static TwoDConfig Instance { get; private set; }


        [Category("归属")]
        [Description("划峰的范围")]
        [Editor("ChartEditWPF.Behaviors.RangePropertyEditor, ChartEditWPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "")]
        public Range PeakRange { get; set; } = new(13, 20);

        [Category("归属")]
        [Description("峰最高点和最低点的最小差值")]
        public double MinHeight { get; set; } = 0;

        [Category("归属")]
        [Description("最小峰高")]
        public double YMin { get; set; } = 0.01;

        [Category("分析图"), DisplayName("色阶数量")]
        public uint ColorCount { get; set; } = 20;
        [Category("分析图"), DisplayName("缺口大小(%)")]
        public uint Gap { get; set; } = 5;
        [Category("分析图"), DisplayName("样品字号")]
        public uint SampleFontSize { get; set; } = 16;
        [Category("分析图"), DisplayName("归属字号")]
        public uint DescFontSize { get; set; } = 16;

        const string edit = "ChartEditWPF.Behaviors.RangePropertyEditor, ChartEditWPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        [Category("读取数据范围"), DisplayName(" D1")]
        [Editor(edit, "")]
        public Range D1 { get; set; } = new(10, 21);

        [Category("读取数据范围"), DisplayName(" DP4")]
        [Editor(edit, "")]
        public Range DP4 { get; set; } = new(35, 85);

        [Category("读取数据范围"), DisplayName(" DP6")]
        [Editor(edit, "")]
        public Range DP6 { get; set; } = new(35, 85);

        [Category("读取数据范围"), DisplayName(" DP8")]
        [Editor(edit, "")]
        public Range DP8 { get; set; } = new(35, 85);

        [Category("读取数据范围"), DisplayName("DP10")]
        [Editor(edit, "")]
        public Range DP10 { get; set; } = new(35, 85);

        public Range GetRange(string fileName)
        {
            var fileInfo = new TwoDFileInfo(fileName);
            return fileInfo.Extension switch
            {
                4 => DP4,
                6 => DP6,
                8 => DP8,
                10 => DP10,
                _ => new Range(35, 85),
            };
        }

        static TwoDConfig()
        {
            LoadConfig();
        }

        [MemberNotNull(nameof(Instance))]
        private static void LoadConfig()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ChartEdit\\twoD.config");
            TwoDConfig? config = null;
            if (File.Exists(path))
            {
                try
                {
                    config = JsonConvert.DeserializeObject<TwoDConfig>(File.ReadAllText(path));
                }
                catch
                {

                }
            }
            Instance = config ?? new TwoDConfig();
        }

        public static void SaveConfig()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ChartEdit\\twoD.config");
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            }
            File.WriteAllText(path, JsonConvert.SerializeObject(Instance));
        }
    }
}

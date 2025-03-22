using ChartEditLibrary.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Entitys
{
    public partial class TwoDConfig
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
        }

        public static TwoDConfig Instance { get; private set; }

        [Category("归属")]
        [Description("划峰的范围")]
        [Editor("ChartEditWPF.Behaviors.RangePropertyEditor, ChartEditWPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "")]
        public Range PeakRange { get; set; } = new(13, 20);

        [Category("归属")]
        [Description("峰最高点和最低点的最小差值")]
        public float MinHeight { get; set; } = 0f;

        [Category("归属")]
        [Description("最小峰高")]
        public float YMin { get; set; } = 0.01f;

        [Category("Range"), DisplayName(" D1")]
        [Editor("ChartEditWPF.Behaviors.RangePropertyEditor, ChartEditWPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "")]
        public Range D1 { get; set; } = new(10, 21);

        [Category("Range"), DisplayName(" DP4")]
        [Editor("ChartEditWPF.Behaviors.RangePropertyEditor, ChartEditWPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "")]
        public Range DP4 { get; set; } = new(35, 85);

        [Category("Range"), DisplayName(" DP6")]
        [Editor("ChartEditWPF.Behaviors.RangePropertyEditor, ChartEditWPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "")]
        public Range DP6 { get; set; } = new(35, 85);

        [Category("Range"), DisplayName(" DP8")]
        [Editor("ChartEditWPF.Behaviors.RangePropertyEditor, ChartEditWPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "")]
        public Range DP8 { get; set; } = new(35, 85);

        [Category("Range"), DisplayName("DP10")]
        [Editor("ChartEditWPF.Behaviors.RangePropertyEditor, ChartEditWPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "")]
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

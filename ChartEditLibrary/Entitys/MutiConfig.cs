using CommunityToolkit.Mvvm.ComponentModel;
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
    public partial class MutiConfig
    {

        public static MutiConfig Instance { get; private set; }
        [Description("图表最大显示数量")]
        [Category("1.图表最大显示数量")]
        public int MaxShowCount { get; set; } = 3;

        [Description("定位最近的谷点")]
        [Category("2.定位谷点")]
        public bool NearestVstreet { get; set; } = true;

        [Description("谷点定位间隔")]
        [Category("2.定位谷点")]
        public int VstreetInterval { get; set; } = 10;

        [Description("最小峰高")]
        [Category("3.最小峰高")]
        public float MinHeight { get; set; } = 5f;

        

        static MutiConfig()
        {
            LoadConfig();
        }

        [MemberNotNull(nameof(Instance))]
        private static void LoadConfig()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ChartEdit\\global.config");
            MutiConfig? config = null;
            if(File.Exists(path))
            {
                try
                {
                    config = JsonConvert.DeserializeObject<MutiConfig>(File.ReadAllText(path));
                }
                catch
                {

                }
            }
            Instance = config ?? new MutiConfig();
        }

        public static void SaveConfig()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ChartEdit\\global.config");
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            }
            File.WriteAllText(path, JsonConvert.SerializeObject(Instance));
        }
    }
}

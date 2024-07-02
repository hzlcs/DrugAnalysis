using ChartEditLibrary.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Entitys
{
    public class Config(string exportType)
    {
        private static readonly Dictionary<string, Config> Configs = [];
        public static Config GetConfig(ExportType key)
        {
            if (!Configs.TryGetValue(key.ToString(), out var config))
            {
                config = new Config(key.ToString());
                Configs[key.ToString()] = config;
            }
            return config;
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <exception cref="Exception">配置文件加载失败</exception>
        public static void LoadConfig()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ChartEdit\\config.json");
            if (File.Exists(path))
            {
                var temp = JsonConvert.DeserializeObject<KeyValuePair<string, Config>[]>(File.ReadAllText(path));
                if (temp != null)
                    foreach (var item in temp)
                    {
                        Configs[item.Key] = item.Value;
                    }
            }
        }

        public static void SaveConfig()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ChartEdit\\config.json");
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            }
            File.WriteAllText(path, JsonConvert.SerializeObject(Configs.ToArray()));
        }

        [Description("样品类型")]
        public string ExportType { get; } = exportType;

        [Description("第一个峰的最小高度")]
        public float FirstYMin { get; set; } = 5;
        [Description("峰最小RT")]
        public float XMin { get; set; } = 10;
        [Description("峰最大RT")]
        public float XMax { get; set; } = 50;
        [Description("峰最小高度")]
        public float YMin { get; set; } = 1;
        [Description("峰顶点与两边峰谷的最小高度差")]
        public float MinHeight { get; set; } = 0.0f;


    }
}

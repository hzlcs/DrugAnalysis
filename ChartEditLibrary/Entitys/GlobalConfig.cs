using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Entitys
{
    public partial class GlobalConfig : ObservableObject
    {
        public static GlobalConfig Instance { get; private set; } 

        [ObservableProperty]
        private int maxShowCount = 3;


        static GlobalConfig()
        {
            LoadConfig();
        }

        [MemberNotNull(nameof(Instance))]
        private static void LoadConfig()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ChartEdit\\global.config");
            GlobalConfig? config = null;
            if(File.Exists(path))
            {
                try
                {
                    config = JsonConvert.DeserializeObject<GlobalConfig>(File.ReadAllText(path));
                }
                catch
                {

                }
            }
            Instance = config ?? new GlobalConfig();
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

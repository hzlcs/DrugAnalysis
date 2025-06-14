using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ChartEditLibrary.Entitys
{
    public class CommonConfig
    {
        public static CommonConfig Instance { get; private set; }

        [Category("其它"), DisplayName("信号图")]
        public bool Signal { get; set; } = true;

        [Category("配色"), DisplayName("配色表")]
        [Editor("ChartEditWPF.Behaviors.ColorPropertyEditor, ChartEditWPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "")]
        public List<ColorEditItem> ColorEditItems { get; set; } = [];

        [JsonIgnore]
        [Browsable(false)]
        public IEnumerable<string> ColorNames => ColorEditItems.Select(v => v.Name);

        [Category("PCA"), DisplayName("标签字号")]
        public int PCALableFontSize { get; set; } = 16;

        [Category("PCA"), DisplayName("标点大小")]
        public double PCAMarkerSize { get; set; } = 10;


        static CommonConfig()
        {
            LoadConfig();
            if (Instance.ColorEditItems.Count == 0)
            {
                Instance.ColorEditItems =
                [
                    new ColorEditItem(Color.Red),
                    new ColorEditItem(Color.Blue),
                    new ColorEditItem(Color.Green),
                ];
            }
        }

        [MemberNotNull(nameof(Instance))]
        private static void LoadConfig()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ChartEdit\\common.config");
            CommonConfig? config = null;
            if (File.Exists(path))
            {
                try
                {
                    config = JsonConvert.DeserializeObject<CommonConfig>(File.ReadAllText(path));
                }
                catch { }
            }
            Instance = config ?? new CommonConfig();
        }

        public static void SaveConfig()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ChartEdit\\common.config");
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            }
            File.WriteAllText(path, JsonConvert.SerializeObject(Instance));
        }
    }


    public partial class ColorEditItem : ObservableValidator
    {
        public ColorEditItem()
        {

        }
        public ColorEditItem(Color color)
        {
            Value = color;
            name = color.Name;
        }

        [ObservableProperty]
        [CustomValidation(typeof(ColorEditItem), nameof(ValidateName))]
        private string name = "";

        [ObservableProperty]
        [JsonIgnore]
        [NotifyPropertyChangedFor(nameof(Value))]
        [NotifyPropertyChangedFor(nameof(ColorName))]
        private byte a = 255;

        [ObservableProperty]
        [JsonIgnore]
        [NotifyPropertyChangedFor(nameof(Value))]
        [NotifyPropertyChangedFor(nameof(ColorName))]
        private byte r;

        [ObservableProperty]
        [JsonIgnore]
        [NotifyPropertyChangedFor(nameof(Value))]
        [NotifyPropertyChangedFor(nameof(ColorName))]
        private byte g;

        [ObservableProperty]
        [JsonIgnore]
        [NotifyPropertyChangedFor(nameof(Value))]
        [NotifyPropertyChangedFor(nameof(ColorName))]
        private byte b;


        public Color Value
        {
            get => Color.FromArgb(A, R, G, B);
            set
            {
                (A, R, G, B) = (value.A, value.R, value.G, value.B);
            }
        }


        [CustomValidation(typeof(ColorEditItem), nameof(ValidateColorName))]
        public string ColorName
        {
            get => ColorTranslator.ToHtml(Value);
            set
            {
                try
                {
                    var color = ColorTranslator.FromHtml(value);
                    (A, R, G, B) = (color.A, color.R, color.G, color.B);
                }
                catch
                {
                }
            }
        }

        partial void OnNameChanging(string value)
        {
            ValidateProperty(value, nameof(Name));
        }

        public static ValidationResult? ValidateColorName(string name, ValidationContext _)
        {
            try
            {
                ColorTranslator.FromHtml(name);
                return ValidationResult.Success;
            }
            catch
            {
                return new ValidationResult("颜色格式错误", [nameof(ColorName)]);
            }
        }
        public static ValidationResult? ValidateName(string name, ValidationContext _)
        {
            if (CommonConfig.Instance?.ColorEditItems?.Any(v => v.Name == name) == true)
                return new ValidationResult("名称重复", [nameof(Name)]);
            return ValidationResult.Success;
        }
    }
}

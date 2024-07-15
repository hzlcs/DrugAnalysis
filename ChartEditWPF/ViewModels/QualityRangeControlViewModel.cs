using ChartEditLibrary.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.ViewModels
{
    internal partial class QualityRangeControlViewModel : ObservableObject
    {
        IFileDialog _fileDialog = App.ServiceProvider.GetRequiredService<IFileDialog>();

        public string[] Columns { get; }

        public Row[] Rows { get; }

        public QualityRangeControlViewModel(SampleArea[] sampleAreas)
        {
            Columns = sampleAreas.Select(s => s.FileName).ToArray();
            Rows = new Row[sampleAreas[0].Area.Length];
            for (int i = 0; i < Rows.Length; i++)
            {
                float?[] values = sampleAreas.Select(s => s.Area[i]).ToArray();
                float avg = default;
                double sd = default;
                if (!values.All(v => !v.HasValue))
                {
                    avg = values.Select(v => v.GetValueOrDefault()).Average();
                    sd = CalculateStdDev(values);
                }
                Rows[i] = new Row(values, avg, sd, sd / avg, "AVG+SD");
            }
        }

        [RelayCommand]
        void Import()
        {
            if (!_fileDialog.ShowDialog(null, out var fileName))
            {
                return;
            }
        }

        public void Import((float Avg, float SD)?[] values)
        {
            for (int i = 0; i < values.Length; ++i)
            {
                Row row = Rows[i];
                row.Range = "-";
                if (!values[i].HasValue)
                    continue;
                if (row.Average == default)
                    continue;
                int value = (int)((row.Average - values[i]!.Value.Avg) / values[i]!.Value.SD);
                row.Range = "AVG" + (value > 0 ? "+" : "-") + value + "SD";
            }
        }

        private static double CalculateStdDev(float?[] values)
        {
            double ret = 0;
            var temp = values.Where(v => v.HasValue).Select(v => v!.Value).ToArray();
            if (temp.Length > 0)
            {
                //  计算平均数   
                double avg = temp.Average();
                //  计算各数值与平均数的差值的平方，然后求和 
                double sum = temp.Sum(d => Math.Pow(d - avg, 2));
                //  除以数量，然后开方
                ret = Math.Sqrt(sum / temp.Length);
            }
            return ret;
        }
    }

    public record Row(float?[] Areas, float Average, double SD, double RSD, string Range) : INotifyPropertyChanged
    {
        private string range = Range;
        public string Range
        {
            get => range;
            set
            {
                if (range == value)
                {
                    return;
                }
                range = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Range)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public record SampleArea(string FileName, float?[] Area);
}

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.ViewModels
{
    internal partial class QualityRangeControlViewModel : ObservableObject
    {
        public string[] Columns { get; }

        public Row[] Rows { get; }

        public QualityRangeControlViewModel(SampleArea[] sampleAreas)
        {
            Columns = sampleAreas.Select(s => s.FileName).ToArray();
            Rows = new Row[sampleAreas[0].Area.Length];
            for (int i = 0; i < Rows.Length; i++)
            {
                Rows[i] = new Row(sampleAreas.Select(s => s.Area[i]).ToArray(), sampleAreas.Select(s => s.Area[i].GetValueOrDefault()).Average());
            }
        }
    }

    public record Row(float?[] Areas, float Average);

    public record SampleArea(string FileName, float?[] Area);
}

using ChartEditLibrary.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.ViewModels
{
    internal partial class QualityRangeViewModel : ObservableObject
    {
        readonly IFileDialog _fileDialog = App.ServiceProvider.GetRequiredService<IFileDialog>();

        public ObservableCollection<QualityRangeControlViewModel> QualityRanges { get; } = [];

        [RelayCommand]
        void AddQualityRange()
        {
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            foreach (var fileName in fileNames)
            {
                if (!File.Exists(fileName))
                    continue;
                using StreamReader sr = new (File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                string[][] text = sr.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(v => v.Split(',')).ToArray();
                var title = text[0];
                SampleArea[] sampleAreas = new SampleArea[(title.Length - 1) / 6];
                for (int i = 0; i < sampleAreas.Length; ++i)
                {
                    sampleAreas[i] = new SampleArea(title[1 + i * 7], text.Skip(2).Select(v => {
                        string value = v[1 + i * 7 + 5];
                        if (string.IsNullOrEmpty(value))
                            return null;
                        return new float?(float.Parse(value));
                        }).ToArray());
                }
                QualityRanges.Add(new QualityRangeControlViewModel(sampleAreas));
            }
        }

        [RelayCommand]
        void Import()
        {

        }

        [RelayCommand]
        void ViewChart()
        {

        }

        [RelayCommand]
        void ExportChart()
        {

        }
    }


}

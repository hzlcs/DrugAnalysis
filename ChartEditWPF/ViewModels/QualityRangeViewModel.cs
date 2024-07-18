using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
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
    internal partial class QualityRangeViewModel(IFileDialog _fileDialog, IMessageBox _messageBox) : ObservableObject
    {
        public ObservableCollection<QualityRangeControlViewModel> QualityRanges { get; } = [];

        [RelayCommand]
        async Task AddQualityRange()
        {
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            string[]? dp = QualityRanges.FirstOrDefault()?.DP;
            List<string[]> newDp = [];
            foreach (var fileName in fileNames)
            {
                if (!File.Exists(fileName))
                    continue;
                var sample = await SampleManager.GetSampleAreasAsync(fileName);
                if (dp is not null)
                    newDp.Add(sample[0].DP);
                QualityRanges.Add(new QualityRangeControlViewModel(sample));
            }
            if (dp is not null)
            {
                dp = SampleManager.MergeDP(newDp.Prepend(dp));
                foreach (var sample in QualityRanges)
                {
                    sample.ApplyDP(dp);
                }
            }
        }

        [RelayCommand]
        async Task Import()
        {
            if (!_fileDialog.ShowDialog(null, out var fileName))
            {
                return;
            }
            try
            {
                AreaDatabase database = await SampleManager.GetDatabaseAsync(fileName[0]);
                foreach (var qualityRange in QualityRanges)
                {
                    qualityRange.Import(database);
                }
            }
            catch (Exception ex)
            {
                _messageBox.Show(ex.Message);
            }
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

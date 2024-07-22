using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditWPF.Windows;
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
    public partial class QualityRangeViewModel(IFileDialog _fileDialog, IMessageBox _messageBox) : ObservableObject
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
            App.ServiceProvider.GetRequiredService<QualityRangeChartWindow>().Show(QualityRanges);
        }

        [RelayCommand]
        void ExportResult()
        {
            if (QualityRanges.Count == 0)
                return;
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            StringBuilder sb = new StringBuilder();
            string[] dps = QualityRanges[0].DP;
            sb.AppendLine($"DP," + string.Join(",,", QualityRanges.Select(v => string.Join(",", v.Columns) + ",AVG,SD,RSD%,质量范围")));
            for (int i = 0; i < dps.Length; ++i)
            {
                sb.Append("DP" + dps[i] + ",");
                foreach (var sample in QualityRanges)
                {
                    RangeRow row = sample.Rows[i];
                    object?[] data = [row.Areas.ElementAtOrDefault(0), row.Areas.ElementAtOrDefault(1), row.Areas.ElementAtOrDefault(2), row.Average, row.StdDev, row.RSD, row.Range];
                    sb.Append(string.Join(",", data));
                    sb.Append(",,");
                }
                sb.Remove(sb.Length - 2, 2);
                if (i != dps.Length - 1)
                    sb.AppendLine();
            }
            File.WriteAllText(fileNames[0], sb.ToString(), Encoding.UTF8);
            File.WriteAllBytes(fileNames[0][..^3] + "png", App.ServiceProvider.GetRequiredService<QualityRangeChartWindow>().GetImage(QualityRanges));
        }
    }


}

using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditWPF.Services;
using ChartEditWPF.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.ViewModels
{
    public partial class QualityRangeViewModel(IFileDialog _fileDialog, IMessageBox _messageBox, ISelectDialog _selectDialog, ILogger<QualityRangeViewModel> logger) : ObservableObject
    {
        [ObservableProperty]
        private string[]? dp = [];

        public ObservableCollection<QualityRangeControlViewModel> QualityRanges { get; } = [];



        [RelayCommand]
        async Task AddQualityRange()
        {
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            using var _ = _messageBox.ShowLoading("正在导入样品...");
            string[]? dp = QualityRanges.FirstOrDefault()?.DP;
            List<string[]> newDp = [];
            foreach (var fileName in fileNames)
            {
                try
                {
                    if (!File.Exists(fileName))
                        continue;
                    var sample = await SampleManager.GetSampleAreasAsync(fileName);
                    if (dp is not null)
                        newDp.Add(sample[0].DP);
                    QualityRanges.Add(new QualityRangeControlViewModel(sample));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "AddQualityRange:{fileName}", fileName);
                    _messageBox.Popup(Path.GetFileNameWithoutExtension(fileName) + "导入失败", NotificationType.Error);
                }
            }
            if (dp is not null)
            {
                try
                {
                    dp = SampleManager.MergeDP(newDp.Prepend(dp));
                    foreach (var sample in QualityRanges)
                    {
                        sample.ApplyDP(dp);
                    }
                    Dp = dp;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "MergeDP");
                    _messageBox.Popup("合并DP失败", NotificationType.Error);
                }
            }
            Dp = QualityRanges[0].DP;
            _messageBox.Popup("样品导入成功", NotificationType.Success);
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
                using var _ = _messageBox.ShowLoading("正在导入数据...");
                var database = await SampleManager.GetDatabaseAsync(fileName[0]);
                foreach (var qualityRange in QualityRanges)
                {
                    qualityRange.Import(database);
                }
                _messageBox.Popup("数据库导入成功", NotificationType.Success);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Import");
                _messageBox.Popup("数据库导入失败", NotificationType.Error);
            }
        }

        [RelayCommand]
        void ViewChart()
        {
            App.ServiceProvider.GetRequiredService<QualityRangeChartWindow>().Show(QualityRanges);
        }

        [RelayCommand]
        void Remove()
        {
            object[]? objs = _selectDialog.ShowListDialog("选择移除数据", "样品", QualityRanges.Select(v => v.SampleName).ToArray());
            if (objs is null)
                return;
            foreach (var obj in objs)
                QualityRanges.Remove(QualityRanges.First(v => v.SampleName == (string)obj));
            Dp = QualityRanges.FirstOrDefault()?.DP;
        }

        [RelayCommand]
        void ExportResult()
        {
            if (QualityRanges.Count == 0)
                return;
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            using var _ = _messageBox.ShowLoading("正在导出结果...");
            try
            {
                var sb = new StringBuilder();
                string[] dps = QualityRanges[0].DP;
                sb.AppendLine($"DP," + string.Join(",,", QualityRanges.Select(v => string.Join(",", v.Columns) + ",AVG,SD,RSD%,质量范围")));
                for (var i = 0; i < dps.Length; ++i)
                {
                    sb.Append("DP" + dps[i] + ",");
                    foreach (var sample in QualityRanges)
                    {
                        var row = sample.Rows[i];
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
                _messageBox.Popup("导出成功", NotificationType.Success);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ExportResult");
                _messageBox.Popup("导出失败", NotificationType.Error);
            }
        }
    }


}

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
using static ChartEditWPF.ViewModels.SamplePageViewModel;

namespace ChartEditWPF.ViewModels
{
    public partial class QualityRangeViewModel(IFileDialog _fileDialog, IMessageBox _messageBox, ISelectDialog _selectDialog, ILogger<QualityRangeViewModel> logger) : ObservableObject
    {
        [ObservableProperty]
        private string[]? descriptions = [];

        [ObservableProperty]
        private string description = "-";

        public ObservableCollection<QualityRangeControlViewModel> QualityRanges { get; } = [];

        private void AddData(List<QualityRangeControlViewModel> data)
        {
            if (data.Count == 0)
                return;
            string[]? descriptions = QualityRanges.FirstOrDefault()?.Descriptions;
            List<string[]> newDescriptions = [];
            if (descriptions is not null)
                newDescriptions.Add(descriptions);
            foreach (var sample in data)
            {
                newDescriptions.Add(sample.Descriptions);
                QualityRanges.Add(sample);
            }
            descriptions = SampleManager.MergeDescription(newDescriptions);
            foreach (var sample in QualityRanges)
            {
                sample.ApplyDescription(descriptions);
            }
            Descriptions = descriptions;
            Description = QualityRanges[0].Description;
            //if (database is not null)
            //{
            //    DoWork();
            //}
        }

        [RelayCommand]
        private async Task AddQualityRange()
        {
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            using var _ = _messageBox.ShowLoading("正在导入样品...");
            List<QualityRangeControlViewModel> datas = [];

            try
            {
                foreach (var fileName in fileNames)
                {
                    try
                    {
                        if (!File.Exists(fileName))
                            continue;
                        var sample = await SampleManager.GetSampleAreasAsync(fileName);
                        if (QualityRanges.Count > 0 && sample.Description != QualityRanges[0].Description)
                        {
                            _messageBox.Popup(fileName + "\n样品类型不一致", NotificationType.Warning);
                            continue;
                        }
                        datas.Add(new QualityRangeControlViewModel(sample));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "AddQualityRange:{fileName}", fileName);
                        _messageBox.Popup(Path.GetFileNameWithoutExtension(fileName) + "导入失败", NotificationType.Error);
                    }
                }
                AddData(datas);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AddSample");
                _messageBox.Popup("导入样品失败", NotificationType.Error);
            }

            _messageBox.Popup("样品导入成功", NotificationType.Success);
        }

        [RelayCommand]
        private async Task AddDatabase()
        {
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            using var _ = _messageBox.ShowLoading("正在添加数据...");
            try
            {
                var datas = new List<QualityRangeControlViewModel>();
                foreach (var fileName in fileNames)
                {
                    if (!File.Exists(fileName))
                        continue;
                    var database = await SampleManager.GetDatabaseAsync(fileName);
                    datas.AddRange(GetSample(database));
                }
                AddData(datas);
                _messageBox.Popup("添加数据成功", NotificationType.Success);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AddDatabase");
                _messageBox.Popup("添加数据失败", NotificationType.Error);
            }

        }

        private static List<QualityRangeControlViewModel> GetSample(AreaDatabase database)
        {
            Dictionary<string, List<int>> sameSamples = [];
            List<int> @default = [];
            for (var i = 0; i < database.SampleNames.Length; ++i)
            {
                var index = database.SampleNames[i].LastIndexOf('-');
                if (index == -1)
                {
                    @default.Add(i);
                    continue;
                }
                var sampleName = database.SampleNames[i][..index];
                if (!sameSamples.TryGetValue(sampleName, out var list))
                {
                    list = [];
                    sameSamples.Add(sampleName, list);
                }
                list.Add(i);
            }
            List<QualityRangeControlViewModel> datas = [];
            foreach (var pair in sameSamples)
            {
                var list = pair.Value;
                var samples = list.Select(v => new SampleArea(pair.Key, [.. database.Descriptions], database.Rows.Select(x => x.Areas[v]).ToArray())).ToArray();
                datas.Add(new QualityRangeControlViewModel(database.Description, samples));
            }
            if (@default.Count > 0)
            {
                if (@default.Count == database.SampleNames.Length)
                    datas.Add(new QualityRangeControlViewModel(database));
                else
                {
                    AreaDatabase @new = new(database.ClassName, @default.Select(v => database.SampleNames[v]).ToArray(), database.Description, [.. database.Descriptions],
                        database.Rows.Select(v => new AreaDatabase.AreaRow(v.Description, @default.Select(i => v.Areas[i]).ToArray())).ToArray());
                    datas.Add(new QualityRangeControlViewModel(@new));
                }
            }
            return datas;
        }

        [RelayCommand]
        private async Task Import()
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
        private void ViewChart()
        {
            App.ServiceProvider.GetRequiredService<QualityRangeChartWindow>().Show(QualityRanges);
        }

        [RelayCommand]
        private void Remove()
        {
            object[]? objs = _selectDialog.ShowListDialog("选择移除数据", "样品", QualityRanges.Select(v => v.SampleName).ToArray());
            if (objs is null)
                return;
            foreach (var obj in objs)
                QualityRanges.Remove(QualityRanges.First(v => v.SampleName == (string)obj));
            Descriptions = QualityRanges.FirstOrDefault()?.Descriptions;
        }

        [RelayCommand]
        private void ExportResult()
        {
            if (QualityRanges.Count == 0)
                return;
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            using var _ = _messageBox.ShowLoading("正在导出结果...");
            try
            {
                var sb = new StringBuilder();
                string[] descriptions = QualityRanges[0].Descriptions;
                string description = QualityRanges[0].Description;
                var saveDatas = QualityRanges.Select(v => v.GetSaveData()).ToArray();
                sb.AppendLine($"{description}," + string.Join(",,", saveDatas.Select(v => string.Join(",", v.Column))));
                for (var i = 0; i < descriptions.Length; ++i)
                {
                    sb.Append($"{description}{descriptions[i]},");
                    foreach (var data in saveDatas)
                    {
                        sb.Append(string.Join(",", data.Rows[i]));
                        sb.Append(",,");
                    }
                    sb.Remove(sb.Length - 2, 2);
                    if (i != descriptions.Length - 1)
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

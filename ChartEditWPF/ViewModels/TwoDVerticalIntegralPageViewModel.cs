using ChartEditLibrary.Entitys;
using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using ChartEditWPF.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Newtonsoft.Json;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ChartEditWPF.ViewModels
{
    public partial class TwoDVerticalIntegralPageViewModel : ObservableObject
    {
        public ObservableCollection<TwoDControlViewModel> Samples { get; } = [];
        public ActiveButtonViewModel ManualIntegral { get; }
        public ActiveButtonViewModel AutoVstreetPoint { get; }

        private readonly IInputForm inputForm;
        private readonly IFileDialog fileDialog;
        private readonly IMessageBox messageBox;
        private readonly ILogger<TwoDVerticalIntegralPageViewModel> logger;

        public TwoDVerticalIntegralPageViewModel(IInputForm inputForm, IFileDialog _fileDialog, IMessageBox _messageBox, ILogger<TwoDVerticalIntegralPageViewModel> logger)
        {
            this.inputForm = inputForm;
            fileDialog = _fileDialog;
            messageBox = _messageBox;
            this.logger = logger;
            Application.Current.Exit += Current_Exit;
            ManualIntegral = new ActiveButtonViewModel(ManualIntegralChanged);
            AutoVstreetPoint = new ActiveButtonViewModel(AutoVstreetPointChanged);
            LoadCache();
        }

        private void LoadCache()
        {
            if (!File.Exists(CacheContent.TwoDCacheFile))
                return;
            try
            {
                var temp = JsonConvert.DeserializeObject<CacheContent[][]>(File.ReadAllText(CacheContent.TwoDCacheFile));
                if (temp is null || temp.Length == 0)
                    return;
                foreach (var cache in temp)
                {
                    var cacheContents = cache.Where(v => !string.IsNullOrEmpty(v.FilePath)).Select(DraggableChartVm.Create).ToArray();
                    var main = cacheContents[0];
                    main.ApplyCuttingLine(cache[0].CuttingLines!.Select(v => new CoordinateLine(v.Start.X, v.Start.Y, v.End.X, v.End.Y)).ToArray());
                    Samples.Add(new TwoDControlViewModel(main, cacheContents.Skip(1).ToArray()));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "缓存文件加载失败");
                messageBox.Show("缓存文件加载失败");
            }
        }

        private void Current_Exit(object? sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(CacheContent.TwoDCacheFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(CacheContent.TwoDCacheFile)!);
                var contents = Samples.Select(v =>
                {
                    CacheContent[] caches = new CacheContent[v.Details.Length + 1];
                    int index = 0;
                    foreach (var vm in v.Details.Select(v => v.DraggableChartVM).Prepend(v.Main.DraggableChartVM))
                    {
                        var cache = new CacheContent()
                        {
                            FilePath = vm.FilePath,
                            FileName = vm.FileName,
                            ExportType = vm.exportType.ToString(),
                            Description = vm.Description,
                            X = vm.DataSource.Select(x => x.X).ToArray(),
                            Y = vm.DataSource.Select(x => x.Y).ToArray(),
                            SaveContent = vm.GetSaveContent(),
                            CuttingLines = vm.CuttingLines?.Select(v => new CacheContent.Line(v)).ToArray()
                        };
                        caches[index++] = cache;
                    }
                    return caches;
                });
                File.WriteAllText(CacheContent.TwoDCacheFile, JsonConvert.SerializeObject(contents));

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "缓存文件保存失败");
            }
            finally
            {
                Samples.Clear();
            }
        }

        [RelayCommand]
        private async Task AddSample()
        {
            if (!fileDialog.ShowDialog(null, out string[]? fileNames))
                return;
            using var load = messageBox.ShowLoading();
            var samples = await GetFromFiles(fileNames);
            foreach (var data in samples)
            {
                var main = data[0].vm;
                main.InitSplitLine();
                foreach (var d in data.Skip(1))
                {
                    d.vm.InitSplitLine();
                }
                Samples.Add(new TwoDControlViewModel(main, data.Skip(1).Select(v => v.vm).ToArray()));
            }
            ManualIntegralChanged(ManualIntegral.IsActive);
            AutoVstreetPointChanged(AutoVstreetPoint.IsActive);
            messageBox.Popup("导入成功", NotificationType.Success);
        }

        private async Task<SampleFileData[][]> GetFromFiles(string[] fileNames, bool @new = false)
        {
            var fileInfos = GetFileInfo(fileNames);
            List<SampleFileData[]> res = [];
            foreach (var files in fileInfos)
            {
                try
                {
                    var mainFile = files[0];
                    var cutFile = files[1];
                    var vms = await Task.WhenAll(
                        files.Skip(2).Select(v => DraggableChartVm.CreateAsync(v.FilePath, ExportType.TwoDimensionDP, DescriptionManager.COM, @new))
                        .Prepend(DraggableChartVm.CreateAsync(mainFile.FilePath, ExportType.TwoDimension, DescriptionManager.DP, false))
                        );
                    var main = vms[0];
                    main.InitSplitLine();
                    await main.ApplyCuttingLine(cutFile.FilePath);
                    List<SampleFileData> details = [new SampleFileData(null, main)];
                    int index = 0;
                    foreach (var vm in vms.Skip(1))
                    {
                        details.Add(new SampleFileData(files.First(v => v.FileName == vm.FileName).Extension, vm));
                        ++index;
                    }
                    res.Add(details.ToArray());
                }
                catch (Exception ex)
                {
                    messageBox.Popup(ex.Message, NotificationType.Error);
                }
            }
            return res.ToArray();
        }

        private TwoDFileInfo[][] GetFileInfo(string[] fileNames, bool getFutFile = true)
        {
            var fileInfos = fileNames.Select(v => new TwoDFileInfo(v)).GroupBy(v => v.SampleName).ToDictionary(v => v.Key, v => v.Order().ToList());
            List<TwoDFileInfo[]> res = [];
            foreach (var kv in fileInfos)
            {
                var files = kv.Value;
                var mainFile = files[0];
                if (mainFile.Extension.HasValue)
                {
                    var mainFileName = Directory.GetFiles(mainFile.DirectionName, mainFile.SampleName + ".csv");
                    if (mainFileName.Length == 0)
                    {
                        messageBox.Popup($"样品：'{kv.Key}'文件名不符合规范", NotificationType.Warning);
                        continue;
                    }
                    files.Insert(0, new TwoDFileInfo(mainFileName[0]));
                    mainFile = files[0];
                }
                if (Samples.Any(v => v.Main.DraggableChartVM.FileName == mainFile.FilePath))
                {
                    messageBox.Popup($"样品：'{kv.Key}'已存在", NotificationType.Warning);
                    continue;
                }
                string[] others = Directory.GetFiles(mainFile.DirectionName, mainFile.SampleName + "*.csv");
                if (others.Length != files.Count)
                {
                    files = others.Select(v => new TwoDFileInfo(v)).Order().ToList();
                }
                if (getFutFile)
                {
                    var cutFile = files[1];
                    if (!cutFile.Extension.HasValue || cutFile.Extension.Value != 0)
                    {
                        messageBox.Popup($"样品：'{kv.Key}'未找到cut marker", NotificationType.Warning);
                        continue;
                    }
                }
                else
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        var file = files[i];
                        if (file.Extension.HasValue && file.Extension.Value == 0)
                        {
                            files.RemoveAt(i);
                            break;
                        }
                    }
                }

                res.Add(files.ToArray());
            }
            return res.ToArray();
        }

        [RelayCommand]
        private async Task TemplateImport()
        {
            if (Samples.Count == 0)
                return;
            var sampleNames = Samples.Select(v => v.Main.DraggableChartVM.FileName).ToArray();
            string model = inputForm.ShowCombboxDialog("请选择模板", sampleNames).ToString()!;
            var templateSample = Samples[Array.IndexOf(sampleNames, model)];
            if (!fileDialog.ShowDialog(null, out var fileNames) || fileNames is null || fileNames.Length == 0)
                return;
            using var _ = messageBox.ShowLoading("正在导入数据...");
            var samples = await GetFromFiles(fileNames, true);

            foreach (var template in templateSample.Details.Select(v => v.DraggableChartVM))
            {
                var fileInfo = new TwoDFileInfo(template.FilePath);
                if (template.BaseLines.Count == 0)
                    continue;
                template.UpdateEndpointLine();
                foreach (var sample in samples)
                {
                    try
                    {
                        var target = sample.FirstOrDefault(v => v.extension == fileInfo.Extension);
                        if (target is null)
                            continue;
                        var vm = target.vm;
                        vm.ApplyTemplateTwoD(template);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "{file}导入失败", sample[0].vm.FilePath);
                        messageBox.Popup(Path.GetFileNameWithoutExtension(sample[0].vm.FileName) + "导入失败", NotificationType.Error);
                    }
                }
            }

            foreach (var sample in samples)
            {
                Samples.Add(new TwoDControlViewModel(sample[0].vm, sample.Skip(1).Select(v => v.vm).ToArray()));
            }

            messageBox.Popup("导入完成", NotificationType.Success);
        }

        [RelayCommand]
        private void Clear()
        {
            Samples.Clear();
            messageBox.Popup("清空成功", NotificationType.Success);
        }

        [RelayCommand]
        private void Remove()
        {
            var select = inputForm.ShowListDialog("选择移除数据", "样品", Samples.Select(v => v.Main.DraggableChartVM.FileName).ToArray());
            if (select is null)
                return;
            foreach (var i in select)
            {
                var sample = Samples.FirstOrDefault(v => v.Main.DraggableChartVM.FileName == i);
                if (sample is not null)
                    Samples.Remove(sample);
            }
            messageBox.Popup("移除成功", NotificationType.Success);
        }

        [RelayCommand]
        private void Resize()
        {
            foreach (var sample in Samples)
            {
                sample.Main.ChartControl.AutoFit();
                foreach (var detail in sample.Details)
                {
                    detail.ChartControl.AutoFit();
                }
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            foreach (var i in Samples)
            {
                try
                {
                    foreach (var x in i.Details.Select(v => v.DraggableChartVM).Prepend(i.Main.DraggableChartVM))
                    {
                        var res = await x.SaveToFile();
                        res.IfFail(v => messageBox.Popup(v.Message, NotificationType.Error));
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "保存失败");
                }
            }
            messageBox.Popup("保存成功", NotificationType.Success);
        }

        [RelayCommand]
        private async Task Export()
        {
            if (Samples.Count == 0)
                return;
            string[]? objs = inputForm.ShowListDialog("选择导出数据", "样品", Samples.Select(v => v.SampleName).ToArray());
            if (objs is null || objs.Length == 0)
                return;
            if (!fileDialog.ShowDirectoryDialog(out var folderName))
                return;
            using var _ = messageBox.ShowLoading("正在导出数据...");
            try
            {
                foreach (var sample in objs.Select(v => Samples.First(x => x.SampleName == v)))
                {
                    foreach (var detail in sample.Details.Prepend(sample.Main).Select(v => v.DraggableChartVM))
                    {
                        if (detail.SplitLines.All(v => string.IsNullOrWhiteSpace(v.Description)))
                        {
                            int index = 0;
                            string start = DescriptionManager.ComDescription.GetDescriptionStart(detail.FileName);
                            foreach (var line in detail.SplitLines)
                            {
                                line.Description = start + (++index);
                            }
                        }
                        string fileName = Path.Combine(folderName, detail.FileName) + ".csv";
                        var data = detail.GetSaveRow();
                        data[1] = new(detail.Description, data[1].line);
                        await File.WriteAllLinesAsync(fileName, data.Select(v => v.description + "," + v.line));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Export");
                messageBox.Popup("导出成功", NotificationType.Success);
            }
        }

        [RelayCommand]
        private void ResetArea()
        {
            foreach (var i in Samples)
            {
                i.Main.DraggableChartVM.ResetArea();
                foreach (var d in i.Details)
                    d.DraggableChartVM.ResetArea();
            }
        }

        [RelayCommand]
        private async Task ChartAnalysis()
        {
            if (!fileDialog.ShowDialog(null, out string[]? fileNames))
                return;
            //string[] fileNames = [@"C:\Users\hzlcs\Desktop\DragA\CS748A.csv", 
            //    @"C:\Users\hzlcs\Desktop\DragA\FS316A.csv",@"C:\Users\hzlcs\Desktop\DragA\CS748A1.csv"
            //];
            try
            {
                var fileInfos = GetFileInfo(fileNames, false);
                if (fileInfos.Length == 0)
                    return;
                var datas = await Task.WhenAll(fileInfos.SelectMany(v => v.Select(HotChartManager.Create)));
                var res = HotChartManager.MergeSampleData(datas.Where(v => v is not null).OfType<HotChartManager.SampleData>().ToArray());
                new HotChartWindow(res).Show();
            }
            catch (Exception ex)
            {
                messageBox.Popup(ex.Message, NotificationType.Error);
                logger.LogError(ex, nameof(ChartAnalysis));
            }
        }

        private void ManualIntegralChanged(bool actived)
        {
            foreach (var i in Samples)
            {
                foreach (var d in i.Details)
                    d.ChartControl.ChangeActived(actived);
            }
        }

        private void AutoVstreetPointChanged(bool actived)
        {
            foreach (var i in Samples)
            {
                foreach (var d in i.Details)
                    d.ChartControl.ChangeAutoVstreet(actived);
            }
        }

        record SampleFileData(int? extension, DraggableChartVm vm);
    }
}

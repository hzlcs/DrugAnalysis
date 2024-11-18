using ChartEditLibrary.Entitys;
using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using ChartEditWPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static ChartEditLibrary.ViewModel.DraggableChartVm;

namespace ChartEditWPF.ViewModels
{
    internal partial class MutiVerticalIntegralViewModel : ObservableObject
    {
        private readonly ISelectDialog _selectDialog;
        private readonly IFileDialog _fileDialog;
        private readonly IMessageBox _messageBox;
        private readonly ILogger logger;

        public ObservableCollection<ShowControlViewModel> DataSources { get; set; } = [];

        [ObservableProperty]
        private string hideButtonText = "隐藏数据";

        public MutiVerticalIntegralViewModel(ISelectDialog selectDialog, IFileDialog fileDialog, IMessageBox messageBox, ILogger<VerticalIntegralViewModel> logger)
        {
            _selectDialog = selectDialog;
            _fileDialog = fileDialog;
            _messageBox = messageBox;
            this.logger = logger;
            Application.Current.Exit += Current_Exit;
            if (!File.Exists(CacheContent.MutiCacheFile))
                return;
            try
            {
                var temp = JsonConvert.DeserializeObject<CacheContent[]>(File.ReadAllText(CacheContent.MutiCacheFile));
                if (temp is null || temp.Length == 0)
                    return;
                var cacheContents = temp.Where(v => !string.IsNullOrEmpty(v.FilePath)).Select(Create).ToArray();
                foreach (var vm in cacheContents)
                {
                    var chartControl = App.ServiceProvider.GetRequiredService<MutiBaselineChartControl>();
                    chartControl.ChartData = vm;
                    var svm = new ShowControlViewModel(chartControl, chartControl.ChartData);
                    DataSources.Add(svm);
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
                if (!Directory.Exists(Path.GetDirectoryName(CacheContent.MutiCacheFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(CacheContent.MutiCacheFile)!);
                var contents = DataSources.Select(v =>
                {
                    var vm = v.DraggableChartVM;
                    var cache = new CacheContent()
                    {
                        FilePath = vm.FilePath,
                        FileName = vm.FileName,
                        Description = vm.Description,
                        ExportType = null,
                        X = vm.DataSource.Select(x => x.X).ToArray(),
                        Y = vm.DataSource.Select(x => x.Y).ToArray(),
                        SaveContent = vm.GetSaveContent()
                    };
                    return cache;
                });
                File.WriteAllText(CacheContent.MutiCacheFile, JsonConvert.SerializeObject(contents));

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "缓存文件保存失败");
            }
            finally
            {
                DataSources.Clear();
            }
        }

        [RelayCommand]
        private async Task Import()
        {
            //var type = (ExportType)_selectDialog.ShowCombboxDialog("选择导入类型", exportTypes);
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            using var _ = _messageBox.ShowLoading("正在导入数据...");
            foreach (var file in fileNames)
            {
                try
                {
                    var vm = await DraggableChartVm.CreateAsync(file, default, "assignment");
                    var chartControl = App.ServiceProvider.GetRequiredService<MutiBaselineChartControl>();
                    chartControl.ChartData = vm;
                    var svm = new ShowControlViewModel(chartControl, chartControl.ChartData);
                    DataSources.Add(svm);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{file}导入失败", file);
                    _messageBox.Popup(Path.GetFileNameWithoutExtension(file) + "导入失败", NotificationType.Error);
                }
            }
            _messageBox.Popup("导入完成", NotificationType.Success);
        }
        [RelayCommand]
        private void Export()
        {
            if (DataSources.Count == 0)
                return;
            object[]? objs = _selectDialog.ShowListDialog("选择导出数据", "样品", DataSources.Select(v => v.DraggableChartVM.FileName).ToArray());
            if (objs is null || objs.Length == 0)
                return;
            if (!_fileDialog.ShowDirectoryDialog(out var folderName))
                return;
            using var _ = _messageBox.ShowLoading("正在导出数据...");
            try
            {
                var data = objs.Select(v=> DataSources.First(x => x.DraggableChartVM.FileName == (string)v)).ToArray();
                foreach(var vm in data)
                {
                    if (vm.DraggableChartVM.SplitLines.Any(v => string.IsNullOrWhiteSpace(v.Description)))
                    {
                        _messageBox.Show($"请设置样品'{vm.DraggableChartVM.FileName}'所有峰的{vm.DraggableChartVM.Description}值");
                        return;
                    }
                }
                var contents = new Dictionary<string, List<SaveRow[]>>();
                foreach (var vm in data)
                {
                    vm.DraggableChartVM.SaveToFile().ContinueWith(v => v.Result.IfFail(e => _messageBox.Show(e.Message)));
                    var bytes = vm.ChartControl.GetImage();

                    var fileName = vm.DraggableChartVM.FileName;
                    File.WriteAllBytes(System.IO.Path.Combine(folderName, fileName + ".png"), bytes);

                    var fileKey = fileName[..fileName.LastIndexOf('-')];
                    if (!contents.ContainsKey(fileKey))
                        contents[fileKey] = [];
                    var saveRow = vm.DraggableChartVM.GetSaveRow();
                    contents[fileKey].Add(saveRow);
                }
                string description = data[0].DraggableChartVM.Description;
                foreach (var content in contents)
                {
                    var path = System.IO.Path.Combine(folderName, content.Key + ".csv");
                    File.Delete(path);
                    var sb = new StringBuilder();
                    sb.AppendLine("," + string.Join(",,", content.Value.Select(v => v[0].line)));
                    sb.AppendLine(description + "," + string.Join(",,", content.Value.Select(v => v[1].line)));
                    string[] descriptions = SampleManager.MergeDescription(content.Value.Select(v => v.Skip(2).Select(x => x.description).ToArray()));
                    var count = content.Value[0][0].line.AsSpan().Count(",");
                    var emptyLine = new string(Enumerable.Repeat(',', count).ToArray());
                    foreach (var descriptionValue in descriptions)
                    {
                        sb.Append($"{descriptionValue},");
                        sb.Append(string.Join(",,", content.Value.Select(row =>
                        {
                            var r = row.FirstOrDefault(v => v.description == descriptionValue);
                            return r.description is null ? emptyLine : r.line;
                        })));
                        sb.AppendLine();
                    }

                    File.WriteAllText(path, sb.ToString(0, sb.Length - Environment.NewLine.Length));
                }
                _messageBox.Popup("导出成功", NotificationType.Success);
            }
            catch(IOException ie)
            {
                _messageBox.Popup("导出失败：" + ie.Message, NotificationType.Error);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "导出失败");
                _messageBox.Popup("导出失败", NotificationType.Error);
            }
        }

        [RelayCommand]
        private void Remove()
        {
            if (DataSources.Count == 0)
                return;
            object[]? objs = _selectDialog.ShowListDialog("选择移除数据", "样品", DataSources.Select(v => v.DraggableChartVM.FileName).ToArray());
            if (objs is null)
                return;
            foreach (var obj in objs)
                DataSources.Remove(DataSources.First(v => v.DraggableChartVM.FileName == (string)obj));
        }

        [RelayCommand]
        private void Resize()
        {
            foreach (var control in DataSources)
            {
                control.ChartControl.AutoFit();
            }
        }

        [RelayCommand]
        private void HideData()
        {
            if (DataSources.Count == 0)
                return;
            var hided = DataSources[0].ShowData;
            foreach (var d in DataSources)
            {
                d.ShowData = !hided;
            }
            HideButtonText = hided ? "显示数据" : "隐藏数据";
        }

        [RelayCommand]
        private async Task SaveResult()
        {
            foreach (var i in DataSources)
            {
                try
                {
                    var res = await i.DraggableChartVM.SaveToFile();
                    res.IfFail(v => _messageBox.Show(v.Message));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "保存失败");
                }
            }
        }

        private bool actived = false;

        [ObservableProperty]
        Brush changeActive = Brushes.White;

        [RelayCommand]
        private void ChangeActived()
        {
            actived = !actived;
            ChangeActive = actived ? Brushes.LightGreen : Brushes.White;
            foreach (var i in DataSources)
            {
                i.ChartControl.ChangeActived(actived);
            }
        }

       
    }
}

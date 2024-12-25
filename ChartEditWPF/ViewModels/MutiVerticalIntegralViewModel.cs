using ChartEditLibrary;
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
using System.Collections.Specialized;
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

        private double panelHeight = 1080;

        [ObservableProperty]
        private double controlHeight = 320;

        [ObservableProperty]
        private string linkButtonText = "链接坐标";

        public ObservableCollection<ShowControlViewModel> DataSources { get; set; } = [];
        private IEnumerable<DraggableChartVm> DataVm => DataSources.Select(v => v.DraggableChartVM);

        [ObservableProperty]
        private string hideButtonText = "隐藏数据";

        [ObservableProperty]
        private string showDescText = "显示归属";

        public MutiVerticalIntegralViewModel(ISelectDialog selectDialog, IFileDialog fileDialog, IMessageBox messageBox, ILogger<VerticalIntegralViewModel> logger)
        {
            _selectDialog = selectDialog;
            _fileDialog = fileDialog;
            _messageBox = messageBox;
            this.logger = logger;
            DataSources.CollectionChanged += DataSources_CollectionChanged;
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
                UpdateHeight();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "缓存文件加载失败");
                messageBox.Show("缓存文件加载失败");
            }
        }


        private void DataSources_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ShowControlViewModel item in e.NewItems!)
                {
                    item.ChartControl.ChartAreaChanged += ChartControl_ChartAreaChanged;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (ShowControlViewModel item in e.OldItems!)
                {
                    item.ChartControl.ChartAreaChanged -= ChartControl_ChartAreaChanged;
                }
            }
        }
        private void ChartControl_ChartAreaChanged(IChartControl obj)
        {
            if (LinkButtonText == "链接坐标")
                return;
            foreach (var i in DataSources.Select(v => v.ChartControl))
            {
                if (i.Equals(obj))
                    continue;
                i.UpdateChartArea(obj);
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
                        ExportType = vm.exportType.ToString(),
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

        private void UpdateHeight()
        {
            if (DataSources.Count > 0)
            {
                int showCount = Math.Min(DataSources.Count, GlobalConfig.Instance.MaxShowCount);
                ControlHeight = (int)(panelHeight / showCount);
            }
        }

        [RelayCommand]
        private async Task ImportStd()
        {
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            if (fileNames.Length == 0)
            {
                return;
            }
            string file = fileNames[0];
            var vm = await DraggableChartVm.CreateAsync(file, ExportType.Standard, "assignment");
            vm.InitSplitLine(null);
            var chartControl = App.ServiceProvider.GetRequiredService<MutiBaselineChartControl>();
            chartControl.ChartData = vm;
            var svm = new ShowControlViewModel(chartControl, chartControl.ChartData);
            if (DataSources.FirstOrDefault(v => v.DraggableChartVM.exportType == ExportType.Standard) != null)
                DataSources.RemoveAt(0);
            DataSources.Insert(0, svm);
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
                    ShowMark(svm);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{file}导入失败", file);
                    _messageBox.Popup(Path.GetFileNameWithoutExtension(file) + "导入失败", NotificationType.Error);
                }
            }
            UpdateHeight();
            _messageBox.Popup("导入完成", NotificationType.Success);
        }

        [RelayCommand]
        private async Task TemplateImport()
        {
            if(DataSources.Count == 0)
                return;
            var std = GetStd();
            if (DataSources.Count == 1 && std is not null)
            {
                return;
            }
            var samples = DataSources.Select(v => v.DraggableChartVM.FileName);
            if(std is not null)
                samples = samples.Skip(1);
            string model = _selectDialog.ShowCombboxDialog("请选择模板", samples.ToArray()).ToString()!;
            var template = DataVm.First(v => v.FileName == model);
            if (!_fileDialog.ShowDialog(null, out var fileNames) || fileNames is null || fileNames.Length == 0)
                return;
            using var _ = _messageBox.ShowLoading("正在导入数据...");
            foreach (var file in fileNames)
            {
                try
                {
                    var vm = await DraggableChartVm.CreateAsync(file, default, "assignment");
                    vm.ApplyTemplate(template);
                    var chartControl = App.ServiceProvider.GetRequiredService<MutiBaselineChartControl>();
                    chartControl.ChartData = vm;
                    var svm = new ShowControlViewModel(chartControl, chartControl.ChartData);
                    DataSources.Add(svm);
                    ShowMark(svm);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{file}导入失败", file);
                    _messageBox.Popup(Path.GetFileNameWithoutExtension(file) + "导入失败", NotificationType.Error);
                }
            }
            UpdateHeight();
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
                var data = objs.Select(v => DataSources.First(x => x.DraggableChartVM.FileName == (string)v)).ToArray();
                foreach (var vm in data)
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
            catch (IOException ie)
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
            {
                var data = DataSources.FirstOrDefault(v => v.DraggableChartVM.FileName == (string)obj);
                if (data is null)
                    continue;
                DataSources.Remove(data);
            }
            UpdateHeight();
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
        private void LinkAxe()
        {
            LinkButtonText = LinkButtonText == "链接坐标" ? "取消链接" : "链接坐标";
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
            DraggableChartVm? std = null;
            if(DataSources.Count > 0 && DataSources[0].DraggableChartVM.exportType == ExportType.Standard)
                std = DataSources[0].DraggableChartVM;
            foreach (var i in DataSources.Select(v=>v.DraggableChartVM))
            {
                if (i.exportType == ExportType.Standard)
                    continue;
                try
                {
                    foreach (var x in i.SplitLines)
                        x.Description = "";
                    if(std is not null && i.SplitLines.All(v => string.IsNullOrWhiteSpace(v.Description)))
                        i.ApplyStandard(std);
                    var res = await i.SaveToFile();
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

        private bool showDesc = false;
        [RelayCommand]
        private void ShowDesc()
        {
            showDesc = !showDesc;
            ShowDescText = !showDesc ? "显示归属" : "隐藏归属";
            if (showDesc)
            {
                foreach (var i in DataSources)
                {
                    ShowMark(i);
                }

            }
            else
            {
                foreach (var i in DataSources)
                {
                    foreach (var line in i.DraggableChartVM.SplitLines)
                        line.HideMark(true);
                    i.ChartControl.PlotControl.Refresh();
                }
            }
        }

        private static void ShowMark(ShowControlViewModel control)
        {
            foreach (var line in control.DraggableChartVM.SplitLines)
                line.ShowMark(new ScottPlot.Coordinates(line.RT, control.DraggableChartVM.DataSource[line.RTIndex].Y + control.DraggableChartVM.Sensitivity.Y * 2), true);
            control.ChartControl.PlotControl?.Refresh();
        }

        [RelayCommand]
        private void PanelLoaded(double height)
        {
            if (panelHeight == 1080)
                panelHeight = height - 10;
            UpdateHeight();
        }

        private DraggableChartVm? GetStd()
        {
            if(DataSources.Count == 0)
                return null;
            if (DataSources[0].DraggableChartVM.exportType != ExportType.Standard)
                return null;
            return DataSources[0].DraggableChartVM;
        }
    }
}

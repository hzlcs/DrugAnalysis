using ChartEditLibrary.Entitys;
using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using ChartEditWPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using static ChartEditLibrary.ViewModel.DraggableChartVM;

namespace ChartEditWPF.ViewModels
{
    public partial class VerticalIntegralViewModel : ObservableObject
    {
        readonly ISelectDialog _selectDialog;
        readonly IFileDialog _fileDialog;
        readonly IMessageBox _messageBox;
        readonly ILogger logger;

        readonly ExportType[] exportTypes = Enum.GetValues<ExportType>();

        public ObservableCollection<ShowControlViewModel> DataSources { get; set; } = [];

        [ObservableProperty]
        private string hideButtonText = "隐藏数据";

        public VerticalIntegralViewModel(ISelectDialog selectDialog, IFileDialog fileDialog, IMessageBox messageBox, ILogger<VerticalIntegralViewModel> logger)
        {
            _selectDialog = selectDialog;
            _fileDialog = fileDialog;
            _messageBox = messageBox;
            this.logger = logger;
            App.Current.Exit += Current_Exit;
            if (!File.Exists(CacheContent.cacheFile))
                return;
            CacheContent[] cacheContents = null!;
            try
            {
                var temp = JsonConvert.DeserializeObject<CacheContent[]>(File.ReadAllText(CacheContent.cacheFile));
                if (temp is null || temp.Length == 0)
                    return;
                cacheContents = temp;
                var t = cacheContents.Where(v => !string.IsNullOrEmpty(v.FilePath)).Select(Create).ToArray();
                foreach (var vm in t)
                {
                    IChartControl chartControl = App.ServiceProvider.GetRequiredService<IChartControl>();
                    chartControl.ChartData = vm;
                    ShowControlViewModel svm = new ShowControlViewModel(chartControl, chartControl.ChartData);
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
                if (!Directory.Exists(Path.GetDirectoryName(CacheContent.cacheFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(CacheContent.cacheFile)!);
                var contenets = DataSources.Select(v =>
                {
                    var vm = v.DraggableChartVM;
                    CacheContent cache = new CacheContent()
                    {
                        FilePath = vm.FilePath,
                        FileName = vm.FileName,
                        X = vm.DataSource.Select(v => v.X).ToArray(),
                        Y = vm.DataSource.Select(v => v.Y).ToArray(),
                        SaveContent = vm.GetSaveContent()
                    };
                    return cache;
                });
                File.WriteAllText(CacheContent.cacheFile, JsonConvert.SerializeObject(contenets));
                
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "缓存文件保存失败");
            }
            finally
            {
                DataSources.Clear();
            }
        }

        int i = 0;
        [RelayCommand]
        async Task Import()
        {
            ExportType type = (ExportType)_selectDialog.ShowCombboxDialog("选择导入类型", exportTypes);
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            using var _ = _messageBox.ShowLoading("正在导入数据...");
            foreach (var file in fileNames)
            {
                try
                {
                    var vm = await DraggableChartVM.CreateAsync(file, type);
                    vm.InitSplitLine(null);
                    IChartControl chartControl = App.ServiceProvider.GetRequiredService<IChartControl>();
                    chartControl.ChartData = vm;
                    ShowControlViewModel svm = new ShowControlViewModel(chartControl, chartControl.ChartData);
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
        void Export()
        {
            if (DataSources.Count == 0)
                return;
            object[]? objs = _selectDialog.ShowListDialog("选择导出数据", "样品", DataSources.Select(v => v.DraggableChartVM.FileName).ToArray());
            if (objs is null || objs.Length == 0)
                return;
            if (!_fileDialog.ShowDirectoryDialog(out string? folderName))
                return;
            using var _ = _messageBox.ShowLoading("正在导出数据...");
            try
            {


                Dictionary<string, List<SaveRow[]>> contents = new Dictionary<string, List<SaveRow[]>>();
                foreach (var obj in objs)
                {
                    string fileName = (string)obj;
                    var vm = DataSources.First(v => v.DraggableChartVM.FileName == fileName);
                    vm.DraggableChartVM.SaveToFile().ContinueWith(v => v.Result.IfFail(e => _messageBox.Show(e.Message)));
                    var bytes = vm.ChartControl.GetImage();
                    File.WriteAllBytes(System.IO.Path.Combine(folderName, fileName + ".png"), bytes);

                    string fileKey = fileName[..fileName.LastIndexOf('-')];
                    if (!contents.ContainsKey(fileKey))
                        contents[fileKey] = new List<SaveRow[]>();
                    var saveRow = vm.DraggableChartVM.GetSaveRow();
                    contents[fileKey].Add(saveRow);
                }
                foreach (var content in contents)
                {

                    string path = System.IO.Path.Combine(folderName, content.Key + ".csv");
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("," + string.Join(",,", content.Value.Select(v => v[0].line)));
                    sb.AppendLine("DP," + string.Join(",,", content.Value.Select(v => v[1].line)));
                    string[] dps = SampleManager.MergeDP(content.Value.Select(v => v.Skip(2).Select(x => x.dp).ToArray()));
                    int count = content.Value[0][0].line.AsSpan().Count(",");
                    string emptyLine = new string(Enumerable.Repeat(',', count).ToArray());
                    foreach (var dp in dps)
                    {
                        sb.Append($"DP{dp},");
                        sb.Append(string.Join(",,", content.Value.Select(row =>
                        {
                            var r = row.FirstOrDefault(v => v.dp == dp);
                            if (r.dp is null)
                                return emptyLine;
                            else
                                return r.line;

                        })));
                        sb.AppendLine();
                    }

                    File.WriteAllText(path, sb.ToString(0, sb.Length - Environment.NewLine.Length));
                }
                _messageBox.Popup("导出成功", NotificationType.Success);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "导出失败");
                _messageBox.Popup("导出失败", NotificationType.Error);
            }
        }

        [RelayCommand]
        void Remove()
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
        void Resize()
        {
            foreach (var control in DataSources)
            {
                control.ChartControl.AutoFit();
            }
        }

        [RelayCommand]
        void HideData()
        {
            if (DataSources.Count == 0)
                return;
            bool hided = DataSources[0].ShowData;
            foreach (var d in DataSources)
            {
                d.ShowData = !hided;
            }
            if (hided)
            {
                HideButtonText = "显示数据";
            }
            else
            {
                HideButtonText = "隐藏数据";
            }
        }

        [RelayCommand]
        async Task SaveResult()
        {
            foreach (var i in DataSources)
            {
                try
                {
                    var res = await i.DraggableChartVM.SaveToFile();
                    res.IfFail(v => _messageBox.Show(v.Message));
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, "保存失败");
                }
            }
        }
    }
}

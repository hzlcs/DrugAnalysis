using ChartEditLibrary.Entitys;
using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using ChartEditWPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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
        readonly ISelectDialog selectDialog = App.ServiceProvider.GetRequiredService<ISelectDialog>();
        readonly IFileDialog fileDialog = App.ServiceProvider.GetRequiredService<IFileDialog>();
        readonly IMessageBox messageBox = App.ServiceProvider.GetRequiredService<IMessageBox>();

        readonly ExportType[] exportTypes = Enum.GetValues<ExportType>();

        public ObservableCollection<ShowControlViewModel> DataSources { get; set; } = [];

        [ObservableProperty]
        private string hideButtonText = "隐藏数据";

        public VerticalIntegralViewModel()
        {
            App.Current.Deactivated += Current_Exit;
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
            catch
            {
                messageBox.Show("缓存文件加载失败");
            }

        }

        private void Current_Exit(object? sender, EventArgs e)
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

        [RelayCommand]
        void Import()
        {
            ExportType type = (ExportType)selectDialog.ShowCombboxDialog("选择导入类型", exportTypes);
            if (!fileDialog.ShowDialog(null, out var fileNames))
                return;
            foreach (var file in fileNames)
            {
                var vm = DraggableChartVM.CreateAsync(file, type).Result;
                vm.InitSplitLine(null);
                IChartControl chartControl = App.ServiceProvider.GetRequiredService<IChartControl>();
                chartControl.ChartData = vm;
                ShowControlViewModel svm = new ShowControlViewModel(chartControl, chartControl.ChartData);
                DataSources.Add(svm);
            }

        }
        [RelayCommand]
        void Export()
        {
            object[]? objs = selectDialog.ShowListDialog("选择导出数据", "样品", DataSources.Select(v => v.DraggableChartVM.FileName).ToArray());
            if (objs is null || objs.Length == 0)
                return;
            if (!fileDialog.ShowDirectoryDialog(out string? folderName))
                return;
            Dictionary<string, List<SaveRow[]>> contents = new Dictionary<string, List<SaveRow[]>>();
            foreach (var obj in objs)
            {
                string fileName = (string)obj;
                var vm = DataSources.First(v => v.DraggableChartVM.FileName == fileName);
                _ = vm.DraggableChartVM.SaveToFile();
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
        }

        [RelayCommand]
        void Remove()
        {
            object[]? objs = selectDialog.ShowListDialog("选择导出数据", "样品", DataSources.Select(v => v.DraggableChartVM.FileName).ToArray());
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
                await i.DraggableChartVM.SaveToFile();
        }
    }
}

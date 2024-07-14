using ChartEditLibrary.Entitys;
using ChartEditLibrary.Interfaces;
using ChartEditLibrary.ViewModel;
using ChartEditWPF.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChartEditLibrary.ViewModel.DraggableChartVM;

namespace ChartEditWPF.ViewModels
{
    public partial class VerticalIntegralViewModel
    {
        readonly List<ShowControlViewModel> vms = [];
        readonly ISelectDialog selectDialog = App.ServiceProvider.GetRequiredService<ISelectDialog>();
        readonly IFileDialog fileDialog = App.ServiceProvider.GetRequiredService<IFileDialog>();

        readonly ExportType[] exportTypes = Enum.GetValues<ExportType>();

        public ObservableCollection<ShowControlViewModel> DataSources { get; set; } = [];

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
                vms.Add(svm);
            }

        }
        [RelayCommand]
        void Export()
        {
            object[]? objs = selectDialog.ShowListDialog("选择导出数据", "样品", vms.Select(v => v.DraggableChartVM.FileName).ToArray());
            if (objs is null)
                return;
            if (!fileDialog.ShowDirectoryDialog(out string? folderName))
                return;
            Dictionary<string, List<SaveRow[]>> contents = new Dictionary<string, List<SaveRow[]>>();
            foreach (var obj in objs)
            {
                string fileName = (string)obj;
                var vm = vms.First(v => v.DraggableChartVM.FileName == fileName);
                string fileKey = fileName[..fileName.LastIndexOf('-')];
                if (!contents.ContainsKey(fileKey))
                    contents[fileKey] = new List<SaveRow[]>();
                contents[fileKey].Add(vm.DraggableChartVM.GetSaveRowContent());

            }
            foreach (var content in contents)
            {

                string path = System.IO.Path.Combine(folderName, content.Key + ".csv");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("," + string.Join(",,", content.Value.Select(v => v[0].line)));
                sb.AppendLine("DP," + string.Join(",,", content.Value.Select(v => v[1].line)));
                string[] dps = content.Value.SelectMany(v => v).Select(v => v.dp).Distinct().Skip(1).ToArray();
                Array.Sort(dps, DPCompare);
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
                sb.Remove(sb.Length - 1, 1);
                System.IO.File.WriteAllText(path, sb.ToString());
            }
        }

        static int DPCompare(string l, string r)
        {
            int[] ls = l.Split('-').Select(int.Parse).ToArray();
            int[] rs = r.Split('-').Select(int.Parse).ToArray();
            if (ls[0] != rs[0])
                return rs[0] - ls[0];
            if (ls.Length == rs.Length && ls.Length == 2)
            {
                return rs[1] - ls[1];
            }
            if (ls.Length == 1)
                return 1;
            return -1;
        }

    }
}

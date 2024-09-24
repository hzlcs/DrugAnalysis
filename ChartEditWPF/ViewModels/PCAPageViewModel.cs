using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditWPF.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChartEditLibrary.Model.PCAManager;
using static ChartEditWPF.ViewModels.TCheckPageViewModel;

namespace ChartEditWPF.ViewModels
{
    internal partial class PCAPageViewModel(IFileDialog _fileDialog, IMessageBox _messageBox, ILogger<PCAPageViewModel> logger) : ObservableObject
    {
        public ObservableCollection<TCheckControlViewModel> Samples { get; } = [];

        [RelayCommand]
        async Task AddSample()
        {
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            using var _ = _messageBox.ShowLoading("正在导入样品...");
            try
            {
                string[]? dp = Samples.FirstOrDefault()?.DP;
                List<string[]> newDp = [];
                if (dp is not null)
                    newDp.Add(dp);
                foreach (var fileName in fileNames)
                {
                    if (!File.Exists(fileName))
                        continue;
                    var sample = await SampleManager.GetSampleAreasAsync(fileName);
                    newDp.Add(sample[0].DP);
                    Samples.Add(new TCheckControlViewModel(sample));
                }
                dp = SampleManager.MergeDP(newDp);
                foreach (var sample in Samples)
                {
                    sample.ApplyDP(dp);
                }
                _messageBox.Popup("导入样品成功", NotificationType.Success);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "AddSample");
                _messageBox.Popup("导入样品失败", NotificationType.Error);
            }

        }

        [RelayCommand]
        void ClearSamples()
        {
            Samples.Clear();
        }

        [RelayCommand]
        async Task ShowPCA()
        {
            if (Samples.Count == 0)
                return;

            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            var dis = _messageBox.ShowLoading("正在导入数据...");
            try
            {
                List<AreaDatabase> databases = [];
                AreaDatabase.AreaRow[] rows = new AreaDatabase.AreaRow[Samples[0].Rows.Count];
                string[] dp = Samples[0].DP;
                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i] = new AreaDatabase.AreaRow(dp[i], Samples.Select(v => v.Rows[i].Average).ToArray());
                }
                AreaDatabase database = new("样品", Samples.Select(v => v.SampleName).ToArray(), dp, rows);
                databases.Add(database);
                foreach (var fileName in fileNames)
                {
                    databases.Add(await SampleManager.GetDatabaseAsync(fileName));
                }
                var pcas = PCAManager.GetPCA([.. databases], out var singularValues, out var eigenVectors);
                _messageBox.Popup("PCA计算完成", NotificationType.Success);
                dis.Dispose();
                new PCAWindow(pcas, singularValues, eigenVectors).ShowDialog();
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "ShowPCA");
                _messageBox.Popup("PCA计算失败", NotificationType.Error);
            }
            
        }
    }
}

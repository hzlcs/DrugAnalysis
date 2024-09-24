using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.ViewModels
{
    internal partial class TCheckPageViewModel(IFileDialog _fileDialog, IMessageBox _messageBox, ILogger<TCheckPageViewModel> logger) : ObservableObject
    {
        private AreaDatabase? database;

        public ObservableCollection<TCheckControlViewModel> Samples { get; } = [];

        public ObservableCollection<PValue> PValues { get; } = [];

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
                if (PValues.Count == 0)
                {
                    for (int i = 0; i < dp.Length; ++i)
                    {
                        PValues.Add(new PValue(dp[i]));
                    }
                }
                for (int i = 0; i < dp.Length; ++i)
                {
                    if (PValues[i].DP != dp[i])
                    {
                        PValues.Insert(i, new PValue(dp[i]));
                    }
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
        async Task Import()
        {
            if (!_fileDialog.ShowDialog(null, out var fileName))
            {
                return;
            }
            try
            {
                using var _ = _messageBox.ShowLoading("正在导入数据库...");
                database = await SampleManager.GetDatabaseAsync(fileName[0]);
                if (Samples.Count == 0)
                    return;
                for (int i = 0; i < PValues.Count; ++i)
                {
                    string dp = PValues[i].DP;
                    if (!database.TryGetRow(dp, out var row))
                    {
                        PValues[i].Value = null;
                        continue;
                    }
                    float[] values = Samples.Select(v => v.Rows[i].Average.GetValueOrDefault()).ToArray();
                    PValues[i].Value = SampleManager.TCheck(values, row.Areas.Where(v => v.HasValue).Select(v => v!.Value).ToArray());
                }
                _messageBox.Popup("导入数据库成功", NotificationType.Success);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Import");
                _messageBox.Popup("导入数据库失败", NotificationType.Error);
            }
        }

        [RelayCommand]
        void ClearSamples()
        {
            Samples.Clear();
            PValues.Clear();
        }

        public class PValue(string dp) : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            public string DP { get; } = dp;

            private double? value;
            public double? Value
            {
                get => value;
                set
                {
                    if (this.value == value)
                        return;
                    this.value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }
    }
}

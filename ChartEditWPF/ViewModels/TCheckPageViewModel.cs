using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    internal partial class TCheckPageViewModel(IFileDialog _fileDialog, IMessageBox _messageBox) : ObservableObject
    {
        private AreaDatabase? database;

        public ObservableCollection<TCheckControlViewModel> Samples { get; } = [];

        public ObservableCollection<PValue> PValues { get; } = [];

        [RelayCommand]
        async Task AddSample()
        {
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
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
                    float[] values = Samples.Select(v => v.Rows[i].Average).ToArray();
                    PValues[i].Value = SampleManager.TCheck(values, row.Areas.Where(v => v.HasValue).Select(v => v!.Value).ToArray());
                }

            }
            catch (Exception ex)
            {
                _messageBox.Show(ex.Message);
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

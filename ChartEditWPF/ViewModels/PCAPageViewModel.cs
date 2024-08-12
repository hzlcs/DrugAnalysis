using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditWPF.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    internal partial class PCAPageViewModel(IFileDialog _fileDialog) : ObservableObject
    {
        public ObservableCollection<TCheckControlViewModel> Samples { get; } = [];

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
            var pcas = PCAManager.GetPCA([.. databases]);
            new PCAWindow(pcas).ShowDialog();
        }
    }
}

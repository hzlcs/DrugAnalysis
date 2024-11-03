using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditWPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChartEditWPF.ViewModels.TCheckPageViewModel;

namespace ChartEditWPF.ViewModels
{
    internal partial class SamplePageViewModel : ObservableObject
    {
        protected readonly IFileDialog _fileDialog;
        protected readonly IMessageBox _messageBox;
        protected readonly ISelectDialog _selectDialog;
        protected readonly ILogger<SamplePageViewModel> logger;
        protected AreaDatabase? database;

        public ObservableCollection<TCheckControlViewModel> Samples { get; } = [];
        public ObservableCollection<PValue> PValues { get; } = [];

        protected SamplePageViewModel(IFileDialog _fileDialog, IMessageBox _messageBox, ISelectDialog _selectDialog, ILogger<SamplePageViewModel> logger)
        {
            this._fileDialog = _fileDialog;
            this._messageBox = _messageBox;
            this._selectDialog = _selectDialog;
            this.logger = logger;
        }


        private void AddData(List<TCheckControlViewModel> data)
        {
            if (data.Count == 0)
                return;
            string[]? dp = Samples.FirstOrDefault()?.DP;
            List<string[]> newDp = [];
            if (dp is not null)
                newDp.Add(dp);
            foreach (var sample in data)
            {
                newDp.Add(sample.DP);
                Samples.Add(sample);
            }
            dp = SampleManager.MergeDP(newDp);
            foreach (var sample in Samples)
            {
                sample.ApplyDP(dp);
            }
            if (PValues.Count == 0)
            {
                for (var i = 0; i < dp.Length; ++i)
                {
                    PValues.Add(new PValue(dp[i]));
                }
            }
            for (var i = 0; i < dp.Length; ++i)
            {
                if (PValues[i].DP != dp[i])
                {
                    PValues.Insert(i, new PValue(dp[i]));
                }
            }
            if(database is not null)
            {
                DoWork();
            }
        }

        [RelayCommand]
        async Task AddSample()
        {
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            using var _ = _messageBox.ShowLoading("正在导入样品...");
            try
            {
                List<TCheckControlViewModel> datas = new List<TCheckControlViewModel>();
                foreach (var fileName in fileNames)
                {
                    if (!File.Exists(fileName))
                        continue;
                    var sample = await SampleManager.GetSampleAreasAsync(fileName);
                    datas.Add(new TCheckControlViewModel(sample));
                }
                AddData(datas);
                _messageBox.Popup("导入样品成功", NotificationType.Success);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AddSample");
                _messageBox.Popup("导入样品失败", NotificationType.Error);
            }
        }

        [RelayCommand]
        async Task AddDatabase()
        {
            if (!_fileDialog.ShowDialog(null, out var fileNames))
            {
                return;
            }
            using var _ = _messageBox.ShowLoading("正在添加数据...");
            try
            {
                List<TCheckControlViewModel> datas = new List<TCheckControlViewModel>();
                foreach (var fileName in fileNames)
                {
                    if (!File.Exists(fileName))
                        continue;
                    var database = await SampleManager.GetDatabaseAsync(fileName);
                    datas.AddRange(GetSample(database));
                }
                AddData(datas);
                _messageBox.Popup("添加数据成功", NotificationType.Success);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AddDatabase");
                _messageBox.Popup("添加数据失败", NotificationType.Error);
            }
        }

        [RelayCommand]
        void RemoveSamples()
        {
            if (Samples.Count == 0)
                return;
            object[]? objs = _selectDialog.ShowListDialog("选择移除数据", "样品", Samples.Select(v => v.SampleName).ToArray());
            if (objs is null)
                return;
            foreach (var obj in objs)
                Samples.Remove(Samples.First(v => v.SampleName == (string)obj));
            if(Samples.Count == 0)
            {
                PValues.Clear();
            }
            _messageBox.Popup("移除样品成功", NotificationType.Success);
        }

        [RelayCommand]
        void ClearSamples()
        {
            Samples.Clear();
            PValues.Clear();
            _messageBox.Popup("清空样品成功", NotificationType.Success);
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
                DoWork();
                _messageBox.Popup("导入数据库成功", NotificationType.Success);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Import");
                _messageBox.Popup("导入数据库失败", NotificationType.Error);
            }
        }


        protected virtual void DoWork()
        {

        }

        private static IEnumerable<TCheckControlViewModel> GetSample(AreaDatabase database)
        {
            Dictionary<string, List<int>> sameSamples = [];
            List<int> @default = [];
            for (var i = 0; i < database.SampleNames.Length; ++i)
            {
                var index = database.SampleNames[i].LastIndexOf('-');
                if (index == -1)
                {
                    @default.Add(i);
                    continue;
                }
                var sampleName = database.SampleNames[i][..index];
                if (!sameSamples.TryGetValue(sampleName, out var list))
                {
                    list = new List<int>();
                    sameSamples.Add(sampleName, list);
                }
                list.Add(i);
            }
            List<TCheckControlViewModel> datas = [];
            foreach (var pair in sameSamples)
            {
                var list = pair.Value;
                SampleArea[] samples = list.Select(v => new SampleArea(pair.Key, database.DP, database.Rows.Select(x => x.Areas[v]).ToArray())).ToArray();
                datas.Add(new TCheckControlViewModel(samples));
            }
            if (@default.Count > 0)
            {
                if (@default.Count == database.SampleNames.Length)
                    datas.Add(new TCheckControlViewModel(database));
                else
                {
                    AreaDatabase @new = new(database.ClassName, @default.Select(v => database.SampleNames[v]).ToArray(),database.DP,
                        database.Rows.Select(v=>new AreaDatabase.AreaRow(v.DP, @default.Select(i => v.Areas[i]).ToArray())).ToArray());
                    datas.Add(new TCheckControlViewModel(@new));
                }
            }
            return datas;
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

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
using ChartEditLibrary;

namespace ChartEditWPF.ViewModels
{
    internal partial class SamplePageViewModel : ObservableObject
    {
        protected readonly IFileDialog _fileDialog;
        protected readonly IMessageBox _messageBox;
        protected readonly IInputForm _selectDialog;
        protected readonly ILogger<SamplePageViewModel> logger;
        protected AreaDatabase? database;
        protected virtual bool GroupDegree { get; } = false;

        [ObservableProperty]
        private string description = "-";

        public ObservableCollection<TCheckControlViewModel> Samples { get; } = [];
        public ObservableCollection<PValue> PValues { get; } = [];

        protected SamplePageViewModel(IFileDialog _fileDialog, IMessageBox _messageBox, IInputForm _selectDialog, ILogger<SamplePageViewModel> logger)
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
            string[]? descriptions = Samples.FirstOrDefault()?.Descriptions;
            List<string[]> newDp = [];
            if (descriptions is not null)
                newDp.Add(descriptions);
            foreach (var sample in data)
            {
                newDp.Add(sample.Descriptions);
                Samples.Add(sample);
            }
            descriptions = SampleManager.MergeDescription(newDp, data[0].Description);
            foreach (var sample in Samples)
            {
                sample.ApplyDescription(descriptions);
            }
            descriptions = DescriptionManager.GetShortGluDescription(descriptions);
            if (PValues.Count == 0)
            {
                foreach (var t in descriptions)
                {
                    PValues.Add(new PValue(t));
                }
            }
            for (var i = 0; i < descriptions.Length; ++i)
            {
                if (PValues[i].Description != descriptions[i])
                {
                    PValues.Insert(i, new PValue(descriptions[i]));
                }
            }
            if (database is not null)
            {
                DoWork();
            }
            Description = Samples[0].Description;
        }

        [RelayCommand]
        private async Task AddSample()
        {
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            using var _ = _messageBox.ShowLoading("正在导入样品...");
            try
            {
                var datas = new List<TCheckControlViewModel>();
                foreach (var fileName in fileNames)
                {
                    if (!File.Exists(fileName))
                        continue;
                    var sample = await SampleManager.GetSampleAreasAsync(fileName);
                    if(GroupDegree)
                        sample = SampleManager.ChangeToGroup(sample);
                    if (Samples.Count > 0 && sample.Description != Samples[0].Description)
                    {
                        _messageBox.Popup(fileName + "\n样品类型不一致", NotificationType.Warning);
                        continue;
                    }
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
        private async Task AddDatabase()
        {
            if (!_fileDialog.ShowDialog(null, out var fileNames))
            {
                return;
            }
            using var _ = _messageBox.ShowLoading("正在添加数据...");
            try
            {
                var datas = new List<TCheckControlViewModel>();
                foreach (var fileName in fileNames)
                {
                    if (!File.Exists(fileName))
                        continue;
                    var database = await SampleManager.GetDatabaseAsync(fileName);
                    if(GroupDegree)
                        database = SampleManager.ChangeToGroup(database);
                    if (Samples.Count > 0 && database.Description != Samples[0].Description)
                    {
                        _messageBox.Popup(fileName + "\n样品类型不一致", NotificationType.Warning);
                        continue;
                    }
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
        private void RemoveSamples()
        {
            if (Samples.Count == 0)
                return;
            object[]? objs = _selectDialog.ShowListDialog("选择移除数据", "样品", Samples.Select(v => v.SampleName).ToArray());
            if (objs is null)
                return;
            foreach (var obj in objs)
                Samples.Remove(Samples.First(v => v.SampleName == (string)obj));
            if (Samples.Count == 0)
            {
                PValues.Clear();
            }
            _messageBox.Popup("移除样品成功", NotificationType.Success);
        }

        [RelayCommand]
        private void ClearSamples()
        {
            Samples.Clear();
            PValues.Clear();
            _messageBox.Popup("清空样品成功", NotificationType.Success);
        }

        [RelayCommand]
        private async Task Import()
        {
            if (!_fileDialog.ShowDialog(null, out var fileName))
            {
                return;
            }
            try
            {
                using var _ = _messageBox.ShowLoading("正在导入数据库...");
                database = await SampleManager.GetDatabaseAsync(fileName[0]);
                if (GroupDegree)
                    database = SampleManager.ChangeToGroup(database);
                if (Samples.Count == 0)
                    return;
                if (database.Description != Samples[0].Description)
                {
                    _messageBox.Popup(fileName[0] + "\n样品类型不一致", NotificationType.Warning);
                    return;
                }
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

        private static List<TCheckControlViewModel> GetSample(AreaDatabase database)
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
                    list = [];
                    sameSamples.Add(sampleName, list);
                }
                list.Add(i);
            }
            List<TCheckControlViewModel> datas = [];
            foreach (var pair in sameSamples)
            {
                var list = pair.Value;
                var samples = list.Select(v => new SampleArea(pair.Key + "-" + (v + 1).ToString(), [.. database.Descriptions], database.Rows.Select(x => x.Areas[v]).ToArray())).ToArray();
                datas.Add(new TCheckControlViewModel(database.Description, samples));
            }
            if (@default.Count > 0)
            {
                if (@default.Count == database.SampleNames.Length)
                    datas.Add(new TCheckControlViewModel(database));
                else
                {
                    AreaDatabase @new = new(database.ClassName, @default.Select(v => database.SampleNames[v]).ToArray(), database.Description, [.. database.Descriptions],
                        database.Rows.Select(v => new AreaDatabase.AreaRow(v.Description, @default.Select(i => v.Areas[i]).ToArray())).ToArray());
                    datas.Add(new TCheckControlViewModel(@new));
                }
            }
            return datas;
        }

        public class PValue(string description) : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            public string Description { get; } = description;

            private string value = "N/A";
            public string? Value
            {
                get => value;
                set
                {
                    string t = value ?? "N/A";
                    if (this.value == t)
                        return;
                    this.value = t;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }
    }
}

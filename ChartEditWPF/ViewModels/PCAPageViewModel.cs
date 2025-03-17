using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditWPF.Services;
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
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.ViewModels
{
    internal partial class PCAPageViewModel(IFileDialog _fileDialog, IMessageBox _messageBox, IInputForm _selectDialog, ILogger<PCAPageViewModel> _logger) : SamplePageViewModel(_fileDialog, _messageBox, _selectDialog, _logger)
    {
        private PCAManager.Result? result;
        private AreaDatabase? cache;
        private PCAWindow? window;

        protected override void DoWork()
        {
            if (database is null)
                return;
            try
            {
                List<AreaDatabase> databases = [];
                AreaDatabase.AreaRow[] rows = new AreaDatabase.AreaRow[Samples[0].DataRows.Count];
                string[] descriptions = Samples[0].Descriptions;
                for (var i = 0; i < rows.Length; i++)
                {
                    rows[i] = new AreaDatabase.AreaRow(descriptions[i], Samples.SelectMany(v => v.GetValues(i)).ToArray());
                }
                AreaDatabase database = new("样品", Samples.SelectMany(v => v.GetSampleNames()).ToArray(), Description, descriptions, rows);
                databases.Add(database);
                databases.Add(this.database);
                result = PCAManager.GetPCA([.. databases]);
                window = null;
                _messageBox.Popup("PCA计算完成", NotificationType.Success);
                if (cache is null || !ReferenceEquals(cache, this.database))
                {
                    cache = this.database;
                    ShowPCA();
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PCADoWork");
                _messageBox.Popup("PCA计算失败", NotificationType.Error);
            }
        }

        [RelayCommand]
        private void ShowPCA()
        {
            if (result is null)
            {
                DoWork();
            }
            if (result is null)
                return;
            try
            {
                window = new PCAWindow(result);
                window.Show();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ShowPCA");
                _messageBox.Popup("PCA显示失败", NotificationType.Error);
            }
        }

        [RelayCommand]
        private void Export()
        {
            if(result is null)
            {
                DoWork();
            }
            if(result is null)
            {
                return;
            }
            try
            {
                window ??= new PCAWindow(result);
                if (!_fileDialog.ShowDialog(null, out var fileNames))
                {
                    return;
                }
                byte[] data = window.GetResult();
                string fileName = Path.GetFileNameWithoutExtension(fileNames[0]) + ".png";
                File.WriteAllBytes(fileName, data);
                _messageBox.Popup("导出成功", NotificationType.Success);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "ExportPCA");
                _messageBox.Popup("导出失败", NotificationType.Error);
            }
        }

    }
}

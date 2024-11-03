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
    internal partial class PCAPageViewModel(IFileDialog _fileDialog, IMessageBox _messageBox, ISelectDialog _selectDialog, ILogger<PCAPageViewModel> _logger) : SamplePageViewModel(_fileDialog, _messageBox, _selectDialog, _logger)
    {
        PCAManager.Result? result;
        AreaDatabase? cache;

        protected override void DoWork()
        {
            if (database is null)
                return;
            try
            {
                List<AreaDatabase> databases = [];
                AreaDatabase.AreaRow[] rows = new AreaDatabase.AreaRow[Samples[0].Rows.Count];
                string[] dp = Samples[0].DP;
                for (var i = 0; i < rows.Length; i++)
                {
                    rows[i] = new AreaDatabase.AreaRow(dp[i], Samples.SelectMany(v => v.GetValues(i)).ToArray());
                }
                AreaDatabase database = new("样品", Samples.SelectMany(v => v.GetSampleNames()).ToArray(), dp, rows);
                databases.Add(database);
                databases.Add(this.database);
                result = PCAManager.GetPCA([.. databases]);
                
                _messageBox.Popup("PCA计算完成", NotificationType.Success);
                if (cache is null || !ReferenceEquals(cache, this.database))
                {
                    cache = this.database;
                    ShowPCA();
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DoWork");
                _messageBox.Popup("PCA计算失败", NotificationType.Error);
            }
        }

        [RelayCommand]
        void ShowPCA()
        {
            if (result is null)
            {
                _messageBox.Popup("请先计算PCA", NotificationType.Error);
                return;
            }
            new PCAWindow(result).Show();
        }

    }
}

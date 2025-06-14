using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditWPF.Services;
using ChartEditWPF.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChartEditWPF.ViewModels
{
    internal partial class TCheckPageVM(IFileDialog _fileDialog, IMessageBox _messageBox, IInputForm _selectDialog, ILogger<TCheckPageVM> _logger) : SamplePageViewModel(_fileDialog, _messageBox, _selectDialog, _logger)
    {
        protected override bool GroupDegree => true;

        protected override void DoWork()
        {
            if (database is null)
                return;
            for (var i = 0; i < PValues.Count; ++i)
            {
                var description = PValues[i].Description;
                if (!database.TryGetRow(description, out var row))
                {
                    PValues[i].Value = null;
                    continue;
                }
                var values = Samples.SelectMany(v => v.GetValues(i)).Where(v => v.HasValue).Select(v => v.GetValueOrDefault()).ToArray();
                PValues[i].Value = SampleManager.TCheck(values, row.Areas.Where(v => v.HasValue).Select(v => v.GetValueOrDefault()).ToArray())?.ToString("F3");
            }
        }

        [RelayCommand]
        private void Export()
        {
            if (Samples.Count == 0)
                return;
            if (!_fileDialog.ShowDialog(null, out var fileNames))
                return;
            using var _ = _messageBox.ShowLoading("正在导出结果...");
            try
            {
                var sb = new StringBuilder();
                string[] descriptions = Samples[0].Descriptions;
                string description = Samples[0].Description;
                var saveDatas = Samples.Select(v => v.GetSaveData()).ToArray();
                string sampleInterval = ",";
                sb.AppendLine($"{description}," + string.Join(sampleInterval,
                    saveDatas.Select(v => string.Join(",", v.Column))) + ",p值");
                if (description == DescriptionManager.Glu)
                    description = "";
                for (var i = 0; i < descriptions.Length; ++i)
                {
                    sb.Append($"{description}{descriptions[i]},");
                    foreach (var data in saveDatas)
                    {
                        sb.Append(string.Join(",", data.Rows[i]));
                        sb.Append(sampleInterval);
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append($",{PValues[i].Value}");
                    if (i != descriptions.Length - 1)
                        sb.AppendLine();
                }
                File.WriteAllText(fileNames[0], sb.ToString(), Encoding.UTF8);
                _messageBox.Popup("导出成功", NotificationType.Success);
            }
            catch(IOException)
            {
                _messageBox.Popup("导出失败,文件被占用,请先关闭", NotificationType.Error);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ExportResult");
                _messageBox.Popup("导出失败", NotificationType.Error);
            }
        }

        [RelayCommand]
        private void CopyData()
        {
            if(Samples.Count == 0)
            {
                return;
            }
            string[] desc = PValues.Select(v => v.Description).ToArray();
            var longDesc = Description == DescriptionManager.Glu ? DescriptionManager.GluDescription.GetLongGluDescription(desc) : desc;
            int rowCount = longDesc.Length + 1;
            var datas = Samples.Select(v => v.GetCopyData().GetEnumerator()).ToArray();
            string description = Description == DescriptionManager.DP ? Description : "";
            StringBuilder sb = new();
            for (int i = 0; i < rowCount; ++i)
            {
                if (i == 0)
                    sb.Append(Description + "\tp值");
                else
                    sb.Append(description + longDesc[i - 1] + "\t" + PValues[i - 1].Value);
                foreach (var data in datas)
                {
                    if (!data.MoveNext())
                        continue;
                    sb.Append('\t');
                    sb.AppendJoin('\t', data.Current);
                }
                sb.AppendLine();
            }
            Clipboard.Clear();
            Clipboard.SetText(sb.ToString());
            Clipboard.Flush();
        }
    }
}

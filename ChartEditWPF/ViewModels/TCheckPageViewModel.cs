using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditWPF.Services;
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
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.ViewModels
{
    internal partial class TCheckPageViewModel(IFileDialog _fileDialog, IMessageBox _messageBox, IInputForm _selectDialog, ILogger<TCheckPageViewModel> _logger) : SamplePageViewModel(_fileDialog, _messageBox, _selectDialog, _logger)
    {

        protected override void DoWork()
        {
            if (database is null)
                return;
            for (var i = 0; i < PValues.Count; ++i)
            {
                var description = PValues[i].Description;
                if (!database.TryGetRow(description, out var row))
                {
                    PValues[i].Value = double.NaN;
                    continue;
                }
                var values = Samples.SelectMany(v => v.GetValues(i)).Where(v => v.HasValue).Select(v => v.GetValueOrDefault()).ToArray();
                PValues[i].Value = SampleManager.TCheck(values, row.Areas.Where(v => v.HasValue).Select(v => v.GetValueOrDefault()).ToArray());
            }
        }

    }
}

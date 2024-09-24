using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChartEditWPF.ViewModels
{
    public partial class QualityRangeControlViewModel : ObservableObject
    {
        IFileDialog _fileDialog = App.ServiceProvider.GetRequiredService<IFileDialog>();

        IMessageBox _messageBox = App.ServiceProvider.GetRequiredService<IMessageBox>();

        public string SampleName { get; }

        public string[] DP { get; private set; }

        public string[] Columns { get; }

        public ObservableCollection<RangeRow> Rows { get; } = [];

        public QualityRangeControlViewModel(SampleArea[] sampleAreas)
        {
            Columns = sampleAreas.Select(s => s.SampleName).ToArray();
            SampleName = Columns[0][..Columns[0].LastIndexOf('-')];
            DP = sampleAreas[0].DP;
            for (int i = 0; i < DP.Length; i++)
            {
                float?[] values = sampleAreas.Select(s => s.Area[i]).ToArray();
                Rows.Add(new RangeRow(DP[i], values));
            }
        }

        public void ApplyDP(string[] dp)
        {
            DP = dp;
            for (int i = 0; i < DP.Length; i++)
            {
                if (Rows[i].DP == dp[i])
                    continue;
                Rows.Insert(i, new RangeRow(dp[i], new float?[Rows[0].Areas.Length]));
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
                AreaDatabase database = await SampleManager.GetDatabaseAsync(fileName[0]);
                Import(database);

            }
            catch (Exception ex)
            {
                _messageBox.Show(ex.Message);
            }
        }

        public void Import(AreaDatabase database)
        {
            for (int i = 0; i < Rows.Count; ++i)
            {
                RangeRow row = Rows[i];
                string dp = row.DP;
                if (!database.TryGetRow(dp, out var datarow))
                {
                    row.Range = "-";
                    continue;
                }
                double _range = Math.Round((row.Average.GetValueOrDefault() - datarow.Average.GetValueOrDefault()) / datarow.StdDev, 2);
                int range = 0;
                if (_range > 0)
                    range = (int)Math.Ceiling(_range);
                else if (_range < 0)
                    range = (int)Math.Floor(_range);
                row.Range = "AVG" + (range >= 0 ? "+" : "") + range + "SD";
            }
        }

    }

    public class RangeRow(string dp, float?[] Areas) : AreaDatabase.AreaRow(dp, Areas), INotifyPropertyChanged
    {
        private string range = "-";
        public string Range
        {
            get => range;
            set
            {
                if (range == value)
                {
                    return;
                }
                range = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Range)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChartEditWPF.ViewModels
{
    public partial class QualityRangeControlViewModel : ObservableObject
    {
        private static readonly string[] SourceArray = ["AVG", "SD", "RSD%"];
        public static string[][] DesignDataColumns { get; } = [
            ["sample1","sample2","sample3", ..SourceArray],
            ["database1"],
            ["database2"]
            ];

        public static ObservableCollection<RangeRow> DesignDataRows { get; } = [
            new RangeRow("DP1") { Data = [ new RangeData(new AreaDatabase.AreaRow("DP1", [1, 2, 3])) , new RangeData(1), new RangeData(2)]},
            ];

        private readonly IFileDialog _fileDialog = App.ServiceProvider.GetRequiredService<IFileDialog>();

        private readonly IMessageBox _messageBox = App.ServiceProvider.GetRequiredService<IMessageBox>();

        public string SampleName { get; }

        public string Description { get; }

        public string[] Descriptions { get; private set; }

        public string[] Columns { get; }

        public Data[][] DataColumns { get; }

        public AreaDatabase? database;

        public ObservableCollection<RangeRow> DataRows { get; } = [];

        public QualityRangeControlViewModel(SampleResult sample) : this(sample.Description, sample.SampleAreas)
        {

        }

        public QualityRangeControlViewModel(string description, SampleArea[] sampleAreas)
        {
            Columns = sampleAreas.Select(s => s.SampleName).ToArray();
            DataColumns = [[.. Columns.Select(v => new Data(v, 125)), .. SourceArray.Select(v => new Data(v, 50))]];
            SampleName = Columns[0][..Columns[0].LastIndexOf('-')];
            Descriptions = sampleAreas[0].Descriptions;
            Description = description;
            for (var i = 0; i < Descriptions.Length; i++)
            {
                var values = sampleAreas.Select(s => s.Area[i]).ToArray();
                RangeRow row = new RangeRow(Descriptions[i]);
                row.Data.Add(new RangeData(new AreaDatabase.AreaRow(Descriptions[i], values)));
                DataRows.Add(row);
            }
        }

        public QualityRangeControlViewModel(AreaDatabase database)
        {
            this.database = database;
            Columns = database.SampleNames;
            DataColumns = Columns.Select(v => new Data[] { new(v, 125) }).ToArray();
            SampleName = database.ClassName;
            Descriptions = [.. database.Descriptions];
            Description = database.Description;
            for (var i = 0; i < Descriptions.Length; i++)
            {
                if (database.TryGetRow(Descriptions[i], out var row))
                {
                    var rangeRow = new RangeRow(Descriptions[i]);
                    foreach (var area in row.Areas)
                        rangeRow.Data.Add(new RangeData(area.GetValueOrDefault()));
                    DataRows.Add(rangeRow);
                }
            }
        }



        public void ApplyDescription(string[] descriptions)
        {
            Descriptions = descriptions;
            for (var i = 0; i < Descriptions.Length; i++)
            {
                string rowDescription = DataRows[i].Description;
                if (rowDescription == descriptions[i])
                    continue;
                else if (!rowDescription.Contains('-') && rowDescription + "-1" == descriptions[i])
                    DataRows[i].Description = descriptions[i];
                else
                    DataRows.Insert(i, DataRows[0].NewRow(descriptions[i]));
            }
        }

        [RelayCommand]
        private async Task Import(Data[]? data)
        {
            if (!_fileDialog.ShowDialog(null, out var fileName))
            {
                return;
            }
            try
            {
                var database = await SampleManager.GetDatabaseAsync(fileName[0]);
                Import(database, data);

            }
            catch (Exception ex)
            {
                _messageBox.Show(ex.Message);
            }
        }

        public void Import(AreaDatabase database, Data[]? sample = null)
        {
            foreach (var row in DataRows)
            {
                var description = row.Description;
                if (!database.TryGetRow(description, out var dataRow))
                {
                    if (sample is null)
                    {
                        foreach (var data in row.Data)
                            data.Range = "-";
                    }
                    else
                    {
                        row.Data.First(v => v.Datas == sample).Range = "-";
                    }
                    continue;
                }
                if (sample is null)
                {
                    foreach (var data in row.Data)
                    {
                        data.Range = dataRow.GetRange(data.Average.GetValueOrDefault());
                    }
                }
                else
                {
                    var sampleData = row.Data.First(v => v.Datas == sample);
                    sampleData.Range = dataRow.GetRange(sampleData.Average.GetValueOrDefault());
                }

            }
        }

        public SaveData GetSaveData()
        {

            if (database is null)
            {
                string[] column = [.. Columns, .. SourceArray, "质量范围"];
                string[][] rows = [.. DataRows.Select(v => v.Data[0].Datas.Select(d => d.Value).Append(v.Data[0].Range).ToArray())];
                return new SaveData(column, rows);
            }
            else
            {
                string[] column = [.. Columns.SelectMany(v => new string[] { v, "质量范围" })];
                string[][] rows = DataRows.Select(v => v.Data.SelectMany(x => new string[] { x.Datas[0].Value, x.Range }).ToArray()).ToArray();
                return new SaveData(column, rows);
            }
        }

        public record SaveData(string[] Column, string[][] Rows);
    }



    public class RangeRow(string description)
    {
        public string Description { get; set; } = description;
        public ObservableCollection<RangeData> Data { get; set; } = [];

        public RangeRow NewRow(string description)
        {
            return new RangeRow(description, Data.Select(d => new RangeData(d.Datas)));
        }

        public RangeRow(string description, IEnumerable<RangeData> data) : this(description)
        {
            foreach (var d in data)
                Data.Add(d);
        }
    }

    public class RangeData : INotifyPropertyChanged
    {
        public Data[] Datas { get; }
        public float? Average { get; }
#if DEBUG
        private string range = "AVG+1SD";
#else
        private string range = "-";
#endif
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
        public double StdDev { get; }

        public RangeData(AreaDatabase.AreaRow row)
        {
            Datas =
            [
                .. row.Areas.Select(v => new Data(v.GetValueOrDefault().ToString("F2"))),
                new Data(row.Average.GetValueOrDefault().ToString("F2"), 50),
                new Data(row.StdDev.ToString("F2"), 50),
                new Data(row.RSD.ToString("P2"), 50)
,
            ];
            Average = row.Average.GetValueOrDefault();
            StdDev = row.StdDev;
        }

        public RangeData(float? average)
        {
            Datas = [new Data(average.GetValueOrDefault().ToString("F2"), 125)];
            Average = average;
        }

        public RangeData(Data[] datas)
        {
            Datas = [.. datas.Select(v => new Data("", v.Width))];
            Average = null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

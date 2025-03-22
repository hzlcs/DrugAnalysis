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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
            new RangeRow("DP1") { Data = [ new RangeData(new AreaDatabase.AreaRow("DP1", [1, 2, 3]), [50,50,50]) , new RangeData(1, 50), new RangeData(2, 50)]},
            ];

        private readonly IFileDialog _fileDialog = App.ServiceProvider.GetRequiredService<IFileDialog>();

        private readonly IMessageBox _messageBox = App.ServiceProvider.GetRequiredService<IMessageBox>();

        public string SampleName { get; }

        public string Description { get; }

        public string[] Descriptions { get; private set; }

        public string[] Columns { get; }

        public LabelData[][] DataColumns { get; }

        public AreaDatabase? database;

        public ObservableCollection<RangeRow> DataRows { get; } = [];

        private readonly int[] dataWidth;

        public QualityRangeControlViewModel(SampleResult sample) : this(sample.Description, sample.SampleAreas)
        {

        }

        public QualityRangeControlViewModel(string description, SampleArea[] sampleAreas)
        {
            Columns = sampleAreas.Select(s => s.SampleName).ToArray();
            dataWidth = Columns.Select(v => 50 + (v.Length - 4) * 6).ToArray();
            DataColumns = [[.. Enumerable.Range(0, Columns.Length).Select(v => new LabelData(Columns[v], dataWidth[v])), .. SourceArray.Select(v => new LabelData(v, 50))]];
            SampleName = Columns[0][..Columns[0].LastIndexOf('-')];
            Descriptions = sampleAreas[0].Descriptions;
            Description = description;
            for (var i = 0; i < Descriptions.Length; i++)
            {
                var values = sampleAreas.Select(s => s.Area[i]).ToArray();
                RangeRow row = new RangeRow(Descriptions[i]);
                row.Data.Add(new RangeData(new AreaDatabase.AreaRow(Descriptions[i], values), dataWidth));
                DataRows.Add(row);
            }
        }

        public QualityRangeControlViewModel(AreaDatabase database)
        {
            this.database = database;
            Columns = database.SampleNames;
            dataWidth = Columns.Select(v => 50 + (v.Length - 4) * 6).ToArray();
            DataColumns = Enumerable.Range(0, Columns.Length).Select(v => new LabelData[] { new LabelData(Columns[v], dataWidth[v]) }).ToArray();
            SampleName = database.ClassName;
            Descriptions = [.. database.Descriptions];
            Description = database.Description;
            for (var i = 0; i < Descriptions.Length; i++)
            {
                if (database.TryGetRow(Descriptions[i], out var row))
                {
                    var rangeRow = new RangeRow(Descriptions[i]);
                    int index = 0;
                    foreach (var area in row.Areas)
                        rangeRow.Data.Add(new RangeData(area.GetValueOrDefault(), dataWidth[index++]));
                    DataRows.Add(rangeRow);
                }
            }
        }



        public void ApplyDescription(string[] descriptions)
        {
            int i;
            for (i = 0; i < descriptions.Length && i < DataRows.Count; i++)
            {
                string rowDescription = DataRows[i].Description;
                if (rowDescription == descriptions[i])
                    continue;
                else if (!rowDescription.Contains('-') && rowDescription + "-1" == descriptions[i])
                    DataRows[i].Description = descriptions[i];
                else
                {
                    int index = Array.IndexOf(Descriptions, descriptions[i]);
                    RangeRow temp;
                    if (index != -1)
                    {
                        temp = DataRows[index];
                        DataRows.RemoveAt(index);
                    }
                    else
                    {
                        temp = DataRows[0].NewRow(descriptions[i]);
                    }
                    DataRows.Insert(i, temp);
                }
            }
            if(i < descriptions.Length)
            {
                for (; i < descriptions.Length; i++)
                {
                    DataRows.Add(DataRows[0].NewRow(descriptions[i]));
                }
            }
            Descriptions = descriptions;
        }

        [RelayCommand]
        private async Task Import(LabelData[]? data)
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

        [RelayCommand]
        private void CopyData()
        {
            var data = GetCopyData();
            int index = 0;
            string GetFirst()
            {
                ++index;
                if (index == 1)
                    return Description;
                return Descriptions[index - 2];
            }
            string text = string.Join(Environment.NewLine, data.Select(v => GetFirst() + "\t" + string.Join("\t", v)));
            Clipboard.Clear();
            Clipboard.SetText(text);
            Clipboard.Flush();
        }

        public IEnumerable<IEnumerable<string>> GetCopyData()
        {
            if (database is null)
                yield return DataColumns.SelectMany(v => v.Select(x => x.Value)).Append("质量范围");
            else
                yield return DataColumns.SelectMany(v => v.Select(x => x.Value).Append("质量范围"));
            for (int i = 0; i < DataRows.Count; ++i)
            {
                yield return DataRows[i].Data.SelectMany(v => v.Datas.Select(x => x.Value).Append(v.Range));
            }
        }

        public void Import(AreaDatabase database, LabelData[]? sample = null)
        {
            foreach (var row in DataRows)
            {
                var description = row.Description;
                if (!database.TryGetRow(description, out var dataRow))
                {
                    if (sample is null)
                    {
                        foreach (var data in row.Data)
                            data.Range = "N/A";
                    }
                    else
                    {
                        row.Data.First(v => v.Datas == sample).Range = "N/A";
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
                string[] column = [.. Columns.Select(v => $"\"\"\"{v}\"\"\""), .. SourceArray, "质量范围"];
                string[][] rows = [.. DataRows.Select(v => v.Data[0].Datas.Select(d => d.Value).Append(v.Data[0].Range).ToArray())];
                return new SaveData(column, rows);
            }
            else
            {
                string[] column = [.. Columns.SelectMany(v => new string[] { $"\"\"\"{v}\"\"\"", "质量范围" })];
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
        public LabelData[] Datas { get; }
        public double? Average { get; }
#if DEBUG
        private string range = "AVG+1SD";
#else
        private string range = "N/A";
#endif
        public string Range
        {
            get => range;
            set
            {
                if (range == value)                
                    return;                
                range = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Range)));
            }
        }
        public double StdDev { get; }

        public RangeData(AreaDatabase.AreaRow row, int[] width)
        {
            Datas =
            [
                ..Enumerable.Range(0,row.Areas.Length).Select(v => new LabelData(row.Areas[v].GetValueOrDefault().ToString("F2"), width[v])),
                new LabelData(row.Average.GetValueOrDefault().ToString("F2"), 50),
                new LabelData(row.StdDev.ToString("F2"), 50),
                new LabelData(row.RSD.ToString("P2"), 50)
,
            ];
            Average = row.Average.GetValueOrDefault();
            StdDev = row.StdDev;
        }

        public RangeData(double? average, int width)
        {
            Datas = [new LabelData(average.GetValueOrDefault().ToString("F2"), width)];
            Average = average;
        }

        public RangeData(LabelData[] datas)
        {
            Datas = [.. datas.Select(v => new LabelData("0", v.Width))];
            Average = null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

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
using System.Windows;
using System.Xml.Serialization;

namespace ChartEditWPF.ViewModels
{
    internal partial class TCheckControlViewModel : ObservableObject
    {
        public string SampleName { get; }

        public string Description { get; }

        public string[] Descriptions { get; private set; }

        public string[] Columns { get; }

        public ObservableCollection<LabelData> ColumnDatas { get; }

        private SampleArea[]? Samples { get; }

        private AreaDatabase? database;

        public ObservableCollection<DataRow> DataRows { get; } = [];

        private readonly int[] dataWidth;

        public TCheckControlViewModel(SampleResult sample) : this(sample.Description, sample.SampleAreas)
        {

        }

        public TCheckControlViewModel(string description, SampleArea[] sampleAreas)
        {
            SampleName = sampleAreas[0].SampleName;
            SampleName = SampleName[..SampleName.LastIndexOf('-')];
            Samples = sampleAreas;
            Columns = sampleAreas.Select(s => s.SampleName).ToArray();
            dataWidth = Columns.Select(v => 50 + (v.Length - 4) * 6).ToArray();
            ColumnDatas = [.. Enumerable.Range(0, Columns.Length).Select(v => new LabelData(Columns[v], dataWidth[v])).Concat(SourceArray.Select(v => new LabelData(v, 50))).ToList()];
            Descriptions = sampleAreas[0].Descriptions;
            Description = description;
            for (var i = 0; i < Descriptions.Length; i++)
            {
                var values = sampleAreas.Select(s => s.Area[i]).ToArray();
                DataRows.Add(GetDataRow(new AreaDatabase.AreaRow(Descriptions[i], values), dataWidth));
            }
        }

        public TCheckControlViewModel(AreaDatabase database)
        {
            SampleName = database.ClassName;
            Columns = [.. database.SampleNames];
            dataWidth = Columns.Select(v => 50 + (v.Length - 4) * 6).ToArray();
            ColumnDatas = [.. Enumerable.Range(0, Columns.Length).Select(v => new LabelData(Columns[v], dataWidth[v])).Concat(SourceArray.Select(v => new LabelData(v, 50))).ToList()];
            Descriptions = [.. database.Descriptions];
            Description = database.Description;
            DataRows = new ObservableCollection<DataRow>(database.Rows.Select(v => GetDataRow(v, dataWidth)));
            this.database = database;
            Samples = null;
        }



        private static DataRow GetDataRow(AreaDatabase.AreaRow row, int[] dataWidth)
        {
            int index = 0;
            return new DataRow(row.Areas.Select(v => new LabelData(v.GetValueOrDefault().ToString("F2"), dataWidth[index++])).Concat(
            [
                new LabelData(row.Average.GetValueOrDefault().ToString("F2"), 50),
                new LabelData(row.StdDev.ToString("F2"), 50),
                new LabelData(row.RSD.ToString("P1"), 50)
            ]).ToList(), row);
        }

        public void ApplyDescription(string[] descriptions)
        {
            int i;
            for (i = 0; i < descriptions.Length && i < DataRows.Count; i++)
            {
                string rowDescription = DataRows[i].Row.Description;
                if (rowDescription == descriptions[i])
                    continue;
                else if (!rowDescription.Contains('-') && rowDescription + "-1" == descriptions[i])
                    DataRows[i].Row.Description = descriptions[i];
                else
                {
                    int index = Array.IndexOf(Descriptions, descriptions[i]);
                    DataRow temp;
                    if (index != -1)
                    {
                        temp = DataRows[index];
                        DataRows.RemoveAt(index);
                    }
                    else
                        temp = GetDataRow(new AreaDatabase.AreaRow(descriptions[i], new double?[DataRows[i].Row.Areas.Length]), dataWidth);
                    DataRows.Insert(i, temp);
                }

            }
            if (i < descriptions.Length)
            {
                for (; i < descriptions.Length; i++)
                {
                    DataRows.Add(GetDataRow(new AreaDatabase.AreaRow(descriptions[i], new double?[DataRows[0].Row.Areas.Length]), dataWidth));
                }
            }
            Descriptions = descriptions;
        }

        public double?[] GetValues(int index)
        {
            if (Samples is null)
            {
                return DataRows[index].Row.Areas.Select(v => v).ToArray();
            }
            else
            {
                return [DataRows[index].Row.Average];
            }
        }

        public SaveData GetSaveData()
        {
            if (Samples is null)
            {
                return new SaveData(Columns.Select(v => $"\"\"\"{v}\"\"\"").ToArray(), DataRows.Select(v => v.Row.Areas.Select(v => v.GetValueOrDefault().ToString("F2")).ToArray()).ToArray());
            }
            else
            {
                return new SaveData([$"\"\"\"{SampleName}\"\"\""], DataRows.Select(v => new string[] { v.Row.Average.GetValueOrDefault().ToString("F2") }).ToArray());
            }
        }

        public string[] GetSampleNames()
        {
            return Samples is null ? Columns : [SampleName];
        }

        public IEnumerable<IEnumerable<string>> GetCopyData()
        {
            yield return ColumnDatas.Select(v => v.Value);
            for (int i = 0; i < DataRows.Count; ++i)
            {
                yield return DataRows[i].DataList.Select(v => v.Value);
            }
        }



        public static ObservableCollection<DataRow> DesignRow { get; } =
        [
            GetDataRow(new AreaDatabase.AreaRow("dp2", [1.0f, 2.0f, 3.0f]), [50,50,50]),
            GetDataRow(new AreaDatabase.AreaRow("dp3", [2.0f, 3.0f, 4.0f]), [50,50,50])
        ];

        private static readonly string[] SourceArray = ["AVG", "SD", "RSD%"];

        public static ObservableCollection<LabelData> DesignColumn { get; } = new ObservableCollection<LabelData>(
            new string[] { "Sample1", "Sample2", "Sample3" }.Select(v => new LabelData(v, 125)).
            Concat(SourceArray.Select(v => new LabelData(v, 50)).ToList()));



        public record SaveData(string[] Column, string[][] Rows);

    }

    public record DataRow(List<LabelData> DataList, AreaDatabase.AreaRow Row);

}

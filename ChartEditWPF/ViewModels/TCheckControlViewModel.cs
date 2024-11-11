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

        public ObservableCollection<Data> ColumnDatas { get; }

        private SampleArea[]? Samples { get; }

        public ObservableCollection<AreaDatabase.AreaRow> Rows { get; } = [];

        public ObservableCollection<DataRow> DataRows { get; } = [];

        public TCheckControlViewModel(SampleResult sample) : this(sample.Description, sample.SampleAreas)
        {

        }

        public TCheckControlViewModel(string description, SampleArea[] sampleAreas)
        {
            SampleName = sampleAreas[0].SampleName;
            SampleName = SampleName[..SampleName.LastIndexOf('-')];
            Samples = sampleAreas;
            Columns = sampleAreas.Select(s => s.SampleName).ToArray();
            ColumnDatas = new ObservableCollection<Data>(Columns.Select(v => new Data(v, 125)).Concat(SourceArray.Select(v => new Data(v, 50))).ToList());
            Descriptions = sampleAreas[0].Descriptions;
            Description = description;
            for (var i = 0; i < Descriptions.Length; i++)
            {
                var values = sampleAreas.Select(s => s.Area[i]).ToArray();
                Rows.Add(new AreaDatabase.AreaRow(Descriptions[i], values));
                DataRows.Add(GetDataRow(Rows[i]));
            }
        }

        public TCheckControlViewModel(AreaDatabase database)
        {
            SampleName = database.ClassName;
            Columns = [.. database.SampleNames];
            ColumnDatas = new ObservableCollection<Data>(Columns.Select(v => new Data(v, 125)).Concat(SourceArray.Select(v => new Data(v, 50))).ToList());
            Descriptions = [.. database.Descriptions];
            Description = database.Description;
            Rows = new ObservableCollection<AreaDatabase.AreaRow>(database.Rows);
            DataRows = new ObservableCollection<DataRow>(database.Rows.Select(GetDataRow));
            Samples = null;
        }

        

        private static DataRow GetDataRow(AreaDatabase.AreaRow row)
        {
            return new DataRow(row.Areas.Select(v => new Data(v.GetValueOrDefault().ToString("F2"))).Concat(
            [
                new Data(row.Average.GetValueOrDefault().ToString("F2"), 50),
                new Data(row.StdDev.ToString("F2"), 50),
                new Data(row.RSD.ToString("P1"), 50)
            ]).ToList());
        }

        public void ApplyDescription(string[] description)
        {
            Descriptions = description;
            int i;
            for (i = 0; i < Descriptions.Length && i < Rows.Count; i++)
            {
                if (Rows[i].Description == description[i])
                    continue;
                Rows.Insert(i, new AreaDatabase.AreaRow(description[i], new float?[Rows[0].Areas.Length]));
                DataRows.Insert(i, GetDataRow(Rows[i]));
            }
            if (i < Descriptions.Length)
            {
                for (; i < Descriptions.Length; i++)
                {
                    Rows.Add(new AreaDatabase.AreaRow(description[i], new float?[Rows[0].Areas.Length]));
                    DataRows.Add(GetDataRow(Rows[i]));
                }
            }
        }

        public float?[] GetValues(int index)
        {
            if (Samples is null)
            {
                return Rows[index].Areas.Select(v => v).ToArray();
            }
            else
            {
                return [Rows[index].Average];
            }
        }
        public string[] GetSampleNames()
        {
            return Samples is null ? Columns : [SampleName];
        }

        public static ObservableCollection<DataRow> DesignRow { get; } =
        [
            GetDataRow(new AreaDatabase.AreaRow("dp2", [1.0f, 2.0f, 3.0f])),
            GetDataRow(new AreaDatabase.AreaRow("dp3", [2.0f, 3.0f, 4.0f]))
        ];

        private static readonly string[] SourceArray = ["AVG", "SD", "RSD%"];

        public static ObservableCollection<Data> DesignColumn { get; } = new ObservableCollection<Data>(
            new string[] { "Sample1", "Sample2", "Sample3" }.Select(v => new Data(v, 125)).
            Concat(SourceArray.Select(v => new Data(v, 50)).ToList()));

    }

    public record DataRow(List<Data> DataList);

    public record Data(string Value, int Width = 125);
}

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

        public string[] DP { get; private set; }

        public string[] Columns { get; }

        public ObservableCollection<Data> ColumnDatas { get; }

        private SampleArea[]? Samples { get; }

        public ObservableCollection<AreaDatabase.AreaRow> Rows { get; } = [];

        public ObservableCollection<DataRow> DataRows { get; } = [];

        public TCheckControlViewModel(SampleArea[] sampleAreas)
        {
            SampleName = sampleAreas[0].SampleName;
            SampleName = SampleName[..SampleName.LastIndexOf('-')];
            Samples = sampleAreas;
            Columns = sampleAreas.Select(s => s.SampleName).ToArray();
            ColumnDatas = new ObservableCollection<Data>(Columns.Select(v => new Data(v, 125)).Concat(sourceArray.Select(v => new Data(v, 50))).ToList());
            DP = sampleAreas[0].DP;
            for (var i = 0; i < DP.Length; i++)
            {
                var values = sampleAreas.Select(s => s.Area[i]).ToArray();
                Rows.Add(new RangeRow(DP[i], values));
                DataRows.Add(GetDataRow(Rows[i]));
            }
        }

        public TCheckControlViewModel(AreaDatabase database)
        {
            SampleName = database.ClassName;
            Columns = [.. database.SampleNames];
            ColumnDatas = new ObservableCollection<Data>(Columns.Select(v => new Data(v, 125)).Concat(sourceArray.Select(v => new Data(v, 50))).ToList());
            DP = database.DP;
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

        public void ApplyDP(string[] dp)
        {
            DP = dp;
            int i;
            for (i = 0; i < DP.Length && i < Rows.Count; i++)
            {
                if (Rows[i].DP == dp[i])
                    continue;
                Rows.Insert(i, new AreaDatabase.AreaRow(dp[i], new float?[Rows[0].Areas.Length]));
                DataRows.Insert(i, GetDataRow(Rows[i]));
            }
            if (i < DP.Length)
            {
                for(; i < DP.Length; i++)
                {
                    Rows.Add(new AreaDatabase.AreaRow(dp[i], new float?[Rows[0].Areas.Length]));
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
            if (Samples is null)
                return Columns;
            return [SampleName];
        }

        public static ObservableCollection<DataRow> DesignRow { get; } = new ObservableCollection<DataRow>()
        {
            GetDataRow(new AreaDatabase.AreaRow("dp2", [1.0f, 2.0f, 3.0f])),
            GetDataRow(new AreaDatabase.AreaRow("dp3", [2.0f, 3.0f, 4.0f])),
        };

        internal static readonly string[] sourceArray = ["AVG", "SD", "RSD%"];

        public static ObservableCollection<Data> DesignColumn { get; } = new ObservableCollection<Data>(
            new string[] { "Sample1", "Sample2", "Sample3" }.Select(v => new Data(v, 125)).
            Concat(sourceArray.Select(v => new Data(v, 50)).ToList()));

    }

    public record DataRow(List<Data> Datas);

    public record Data(string Value, int Width = 125);
}

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
    internal partial class TCheckControlViewModel : ObservableObject
    {
        public string[] DP { get; private set; }

        public string[] Columns { get; }

        public ObservableCollection<AreaDatabase.AreaRow> Rows { get; } = [];

        public TCheckControlViewModel(SampleArea[] sampleAreas)
        {
            Columns = sampleAreas.Select(s => s.SampleName).ToArray();
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
            for(int i = 0; i < DP.Length; i++)
            {
                if (Rows[i].DP == dp[i])
                    continue;
                Rows.Insert(i, new AreaDatabase.AreaRow(dp[i], new float?[Rows[0].Areas.Length]));
            }
        }

    }



}

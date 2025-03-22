using ChartEditLibrary.Interfaces;
using ChartEditLibrary.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.ViewModels
{
    public partial class TwoDControlViewModel : ObservableObject
    {
        public TwoDControlViewModel(DraggableChartVm main, DraggableChartVm[] details)
        {
            var chartControl = App.ServiceProvider.GetRequiredService<SingleBaselineChartControl>();
            chartControl.ChartData = main;
            Main = new ShowControlViewModel(chartControl, chartControl.ChartData);
            SampleName = main.FileName;
            Details = new ShowControlViewModel[details.Length];
            for (int i = 0; i < details.Length; i++)
            {
                var control = App.ServiceProvider.GetRequiredService<MutiBaselineChartControl>();
                control.ChartData = details[i];
                Details[i] = new ShowControlViewModel(control, control.ChartData);
            }
        }
        public string SampleName { get; }
        public ShowControlViewModel Main { get; }
        public ShowControlViewModel[] Details { get; }
    }
}

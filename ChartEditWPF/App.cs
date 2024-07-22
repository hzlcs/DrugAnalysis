using ChartEditLibrary.Interfaces;
using ChartEditWPF.Models;
using ChartEditWPF.Pages;
using ChartEditWPF.Services;
using ChartEditWPF.ViewModels;
using ChartEditWPF.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChartEditWPF
{
    public class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        [STAThread]
        public static void Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();
            host.Start();
            ServiceProvider = host.Services;
            App app = new();
            //app.InitializeComponent();
            app.MainWindow = host.Services.GetRequiredService<MainWindow>();
            app.MainWindow.DataContext = host.Services.GetRequiredService<MainViewModel>();
            app.MainWindow.Visibility = Visibility.Visible;
            app.Resources.MergedDictionaries.Add(LoadComponent(new Uri("MyResource.xaml", UriKind.Relative)) as System.Windows.ResourceDictionary);
            app.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();
                services.AddTransient<IChartControl, ChartControl>();
                services.AddSingleton<IMessageBox, WPFMessageBox>();
                services.AddSingleton<IInputForm, WPFInputForm>();
                services.AddSingleton<IFileDialog, WPFFileDialog>();
                services.AddSingleton<ISelectDialog, WPFSelectDialog>();

                services.AddSingleton<VerticalIntegralViewModel>();
                services.AddKeyedSingleton<IPage, VerticalIntegralPage>(Models.Pages.VerticalIntegral, (s, v) => new VerticalIntegralPage() { DataContext = s.GetRequiredService<VerticalIntegralViewModel>() });

                services.AddSingleton<TCheckPageViewModel>();
                services.AddKeyedSingleton<IPage, TCheckPage>(Models.Pages.TCheck, (s, v) => new TCheckPage() 
                { DataContext = s.GetRequiredService<TCheckPageViewModel>() });

                services.AddSingleton<QualityRangeViewModel>();
                services.AddKeyedSingleton<IPage, QualityRangePage>(Models.Pages.QualityRange, (s, v) => new QualityRangePage() 
                { DataContext = s.GetRequiredService<QualityRangeViewModel>() });

                services.AddSingleton<VerticalIntegralConfigViewModel>();
                services.AddKeyedSingleton<IPage, VerticalIntegralConfigPage>(Models.Pages.VerticalIntegralConfig, (s, v) => new VerticalIntegralConfigPage()
                { DataContext = s.GetRequiredService<VerticalIntegralConfigViewModel>() });

                services.AddTransient<QualityRangeChartWindow>();
            });
    }
}

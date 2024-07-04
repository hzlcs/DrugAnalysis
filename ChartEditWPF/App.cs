using ChartEditLibrary.Interfaces;
using ChartEditWPF.Services;
using ChartEditWPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChartEditWPF
{
    public class App : Application
    {
        [STAThread]
        public static void Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();
            host.Start();

            App app = new();
            //app.InitializeComponent();
            app.MainWindow = host.Services.GetRequiredService<MainWindow>();
            app.MainWindow.DataContext = host.Services.GetRequiredService<MainViewModel>();
            app.MainWindow.Visibility = Visibility.Visible;
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

            });
    }
}

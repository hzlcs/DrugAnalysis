using ChartEditLibrary.Interfaces;
using ChartEditWPF.Models;
using ChartEditWPF.Pages;
using ChartEditWPF.Services;
using ChartEditWPF.ViewModels;
using ChartEditWPF.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System.Windows.Threading;

namespace ChartEditWPF
{
    public class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        [STAThread]
        public static void Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();
            host.Start();
            ServiceProvider = host.Services;
            App app = new()
            {
                //app.InitializeComponent();
                MainWindow = host.Services.GetRequiredService<MainWindow>()
            };
            app.MainWindow.DataContext = host.Services.GetRequiredService<MainViewModel>();
            app.MainWindow.Visibility = Visibility.Visible;
            app.Resources.MergedDictionaries.Add(LoadComponent(new Uri("MyResource.xaml", UriKind.Relative)) as System.Windows.ResourceDictionary);
            app.DispatcherUnhandledException += UnhandledException;
            app.Run();
            host.StopAsync().Wait();
        }

        private static void UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var logger = ServiceProvider.GetRequiredService<ILogger<App>>();
            logger.LogError(e.Exception, "Unhandled exception");
            e.Handled = false;
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<MainWindow>();
                services.AddSingleton<IMessageWindow>(provider => provider.GetRequiredService<MainViewModel>());
                services.AddSingleton<MainViewModel>();
                services.AddTransient<SingleBaselineChartControl>();
                services.AddTransient<MutiBaselineChartControl>();
                services.AddSingleton<IMessageBox, WPFMessageBox>();
                services.AddSingleton<IInputForm, WPFInputForm>();
                services.AddSingleton<IFileDialog, WPFFileDialog>();
                services.AddSingleton<ISelectDialog, WPFSelectDialog>();

                services.AddSingleton<VerticalIntegralViewModel>();
                services.AddKeyedSingleton<IPage, VerticalIntegralPage>(Models.Pages.VerticalIntegral, (s, v) => new VerticalIntegralPage() { DataContext = s.GetRequiredService<VerticalIntegralViewModel>() });

                services.AddSingleton<MutiVerticalIntegralViewModel>();
                services.AddKeyedSingleton<IPage, MutiVerticalIntegralPage>(Models.Pages.MutiVerticalIntegral, (s, v) => new MutiVerticalIntegralPage() { DataContext = s.GetRequiredService<MutiVerticalIntegralViewModel>() });

                services.AddSingleton<TCheckPageViewModel>();
                services.AddKeyedSingleton<IPage, TCheckPage>(Models.Pages.TCheck, (s, v) => new TCheckPage()
                { DataContext = s.GetRequiredService<TCheckPageViewModel>() });

                services.AddSingleton<QualityRangeViewModel>();
                services.AddKeyedSingleton<IPage, QualityRangePage>(Models.Pages.QualityRange, (s, v) => new QualityRangePage()
                { DataContext = s.GetRequiredService<QualityRangeViewModel>() });

                services.AddSingleton<VerticalIntegralConfigViewModel>();
                services.AddKeyedSingleton<IPage, VerticalIntegralConfigPage>(Models.Pages.VerticalIntegralConfig, (s, v) => new VerticalIntegralConfigPage()
                { DataContext = s.GetRequiredService<VerticalIntegralConfigViewModel>() });

                services.AddSingleton<PCAPageViewModel>();
                services.AddKeyedSingleton<IPage, PCAPage>(Models.Pages.PCA, (s, v) => new PCAPage()
                { DataContext = s.GetRequiredService<PCAPageViewModel>() });

                services.AddTransient<QualityRangeChartWindow>();
                ConfigureLog();
            });

        private static void ConfigureLog()
        {
            const string logOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {SourceContext:l}{NewLine}{Message}{NewLine}{Exception}";
            Log.Logger = new LoggerConfiguration()
              .MinimumLevel.Override("Default", Debugger.IsAttached ? LogEventLevel.Debug : LogEventLevel.Information)
              .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
              .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Error)
#if DEBUG
              .MinimumLevel.Debug()
#endif
              .Enrich.FromLogContext()
              .WriteTo.File($"{AppContext.BaseDirectory}Logs/log.txt", rollingInterval: RollingInterval.Day, outputTemplate: logOutputTemplate)
              .CreateLogger();
            Log.Logger.Debug("Logger initialized");
        }
    }

}
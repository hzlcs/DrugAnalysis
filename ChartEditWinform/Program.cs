using ChartEditLibrary.Interfaces;
using ChartEditWinform.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace ChartEditWinform
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();
            
            host.Start();
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(host.Services.GetRequiredService<Form1>());
            host.StopAsync().Wait();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton<IMessageBox, WinformMessageBox>();
            services.AddSingleton<IInputForm, WinformInputForm>();
            services.AddSingleton<IFileDialog, WinformFileDialog>();
            services.AddTransient<IChartControl, ChartControl>();
            services.AddSingleton<Form1>();
        });
    }
}
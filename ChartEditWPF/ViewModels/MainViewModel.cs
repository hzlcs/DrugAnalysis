﻿using ChartEditLibrary.Interfaces;
using ChartEditLibrary.ViewModel;
using ChartEditWPF.Models;
using ChartEditWPF.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ChartEditWPF.ViewModels
{
    internal partial class MainViewModel(IServiceProvider serviceProvider) : ObservableObject, IMessageWindow
    {

        public static NotificationContent[] DesignNotificationContents { get; } =
        [
            new NotificationContent("通知", "这是一个通知", NotificationType.Information),
            new NotificationContent("警告", "这是一个警告", NotificationType.Warning),
            new NotificationContent("错误", "这是一个错误", NotificationType.Error),
        ];

        public ObservableCollection<NotificationContent> Notifications { get; } = [];

        [ObservableProperty]
        private bool popupVisible;

        [ObservableProperty]
        private Visibility loadingVisible = Visibility.Hidden;

        [ObservableProperty]
        private IPage? content;

        [RelayCommand]
        private void ButtonClick(object? tag)
        {
            if (tag is not null)
                Content = serviceProvider.GetRequiredKeyedService<IPage>(tag);
        }

        private readonly SemaphoreSlim semaphore = new(1, 1);
        public async void Popup(string message, NotificationType type)
        {
            await semaphore.WaitAsync();
            if (Notifications.Count >= 3)
                Notifications.RemoveAt(0);
            PopupVisible = true;
            var content = new NotificationContent(type.ToString(), message, type);
 
            Notifications.Add(content);
            semaphore.Release();
            await Task.Delay(3000);
            Notifications.Remove(content);
            if (Notifications.Count == 0)
                PopupVisible = false;


        }

        public IDisposable ShowLoading(string message = "正在加载中...")
        {
            return new LoadStruct(ChangeVisibility);
        }

        private void ChangeVisibility(Visibility visibility)
        {
            LoadingVisible = visibility;
        }

        private readonly struct LoadStruct : IDisposable
        {
            public LoadStruct(Action<Visibility> action)
            {
                ++showCount;
                action.Invoke(Visibility.Visible);
                this.action = action;
            }

            private static int showCount;
            private readonly Action<Visibility> action;

            public void Dispose()
            {
                if (--showCount == 0)
                    action.Invoke(Visibility.Hidden);
            }
        }
    }

}

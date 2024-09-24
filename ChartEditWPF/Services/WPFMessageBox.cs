using ChartEditLibrary.Interfaces;
using ChartEditWPF.ViewModels;
using ChartEditWPF.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ChartEditWPF.Services
{
    internal class WPFMessageBox(IMessageWindow messageWindow) : IMessageBox
    {

        //todo
        public bool ConfirmOperation(string message)
        {
            Debug.WriteLine(message);
            return MessageBox.Show(message, "", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        }

        public void Show(string message)
        {
            Debug.WriteLine(message);
            MessageBox.Show(message);
        }

        public void Popup(string message, ChartEditLibrary.Interfaces.NotificationType type)
        {
            messageWindow.Popup(message, type);
        }

        public IDisposable ShowLoading(string message = "正在加载中...")
        {
            return messageWindow.ShowLoading(message);
        }
    }
}

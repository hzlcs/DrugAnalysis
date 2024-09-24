using ChartEditLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.Services
{
    public interface IMessageWindow
    {
        void Popup(string message, NotificationType type);

        IDisposable ShowLoading(string message = "正在加载中...");
    }
}

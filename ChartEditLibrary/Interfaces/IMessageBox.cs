using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Interfaces
{
    public enum NotificationType
    {

        //
        // 摘要:
        //     Information style type
        Information,
        //
        // 摘要:
        //     Success style type
        Success,
        //
        // 摘要:
        //     Warning style type
        Warning,
        //
        // 摘要:
        //     Error style type
        Error

    }

    public interface IMessageBox
    {
        void Show(string message);

        void Popup(string message, NotificationType type);

        bool ConfirmOperation(string message);

        IDisposable ShowLoading(string message = "正在加载中...");
    }
}

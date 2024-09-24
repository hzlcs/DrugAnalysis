using ChartEditLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ChartEditWPF.ViewModels
{
    public class NotificationContent(string title, string message, NotificationType type)
    {
        public string Title { get; } = title;
        public string Message { get; } = message;
        public NotificationType Type { get; } = type;
        public Brush Background { get; } = type switch
        {
            NotificationType.Success => Brushes.Green,
            NotificationType.Error => Brushes.Red,
            NotificationType.Warning => Brushes.Orange,
            NotificationType.Information => Brushes.Blue,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}

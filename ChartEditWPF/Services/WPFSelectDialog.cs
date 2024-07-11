using ChartEditWPF.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.Services
{
    public interface ISelectDialog
    {
        object ShowDialog(string title, Array options);
    }

    public class WPFSelectDialog : ISelectDialog
    {
        object res = null!;
        public object ShowDialog(string title, Array options)
        {
            new SelectWindow(title, options, Callback).ShowDialog();
            
            return res;
        }

        void Callback(object obj)
        {
            res = obj;
        }
    }
}

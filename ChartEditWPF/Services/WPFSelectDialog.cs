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
        object ShowCombboxDialog(string title, Array options);
        object[]? ShowListDialog(string title, string itemName, Array options);
    }

    public class WPFSelectDialog : ISelectDialog
    {
        object res = null!;
        object[]? objects;
        public object ShowCombboxDialog(string title, Array options)
        {
            new SelectOneWindow(title, options, Callback).ShowDialog();
            return res;
        }

        public object[]? ShowListDialog(string title, string itemName, Array options)
        {
            objects = null;
            new SelectMutiWindow(title, itemName, options, Callback).ShowDialog();
            return objects;
        }

        void Callback(object[] objs)
        {
            objects = objs;
        }

        void Callback(object obj)
        {
            res = obj;
        }
    }
}

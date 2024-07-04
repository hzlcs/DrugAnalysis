using ChartEditLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.Services
{
    internal class WPFMessageBox : IMessageBox
    {
        public bool ConfirmOperation(string message)
        {
            Debug.WriteLine(message);
            return true;
        }

        public void Show(string message)
        {
            Debug.WriteLine(message);
        }
    }
}

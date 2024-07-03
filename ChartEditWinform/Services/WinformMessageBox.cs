using ChartEditLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWinform.Services
{
    internal class WinformMessageBox : IMessageBox
    {
        public bool ConfirmOperation(string message)
        {
            return MessageBox.Show(message, "", MessageBoxButtons.YesNo) == DialogResult.Yes;
        }

        public void Show(string message)
        {
            MessageBox.Show(message);
        }
    }
}

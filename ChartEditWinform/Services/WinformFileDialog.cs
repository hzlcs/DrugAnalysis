using ChartEditLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWinform.Services
{
    internal class WinformFileDialog : IFileDialog
    {
        private readonly OpenFileDialog openFileDialog = new OpenFileDialog();

        public string FileName { get => openFileDialog.FileName; set => openFileDialog.FileName = value; }

        public bool ShowDialog()
        {
            return openFileDialog.ShowDialog() == DialogResult.OK;
        }
    }
}

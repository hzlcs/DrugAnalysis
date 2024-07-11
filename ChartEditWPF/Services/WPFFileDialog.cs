using ChartEditLibrary.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.Services
{
    internal class WPFFileDialog : IFileDialog
    {
        public string[]? FileNames { get; set; }
        public string? FileName { get; set; }

        public bool ShowDialog()
        {
            OpenFileDialog fileDialog = new()
            {
                Multiselect = true
            };
            if (fileDialog.ShowDialog() == true)
            {
                FileNames = fileDialog.FileNames;
                return true;
            }
            return false;
        }
    }
}

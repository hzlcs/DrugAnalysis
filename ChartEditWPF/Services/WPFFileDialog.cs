using ChartEditLibrary.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.Services
{
    internal class WPFFileDialog : IFileDialog
    {

        public bool ShowDialog(string? fileName, [MaybeNullWhen(false)] out string[] fileNames)
        {
            OpenFileDialog fileDialog = new()
            {
                Multiselect = true,
                FileName = fileName,
                CheckFileExists = false,
                CheckPathExists = true,
                AddExtension = true,
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            };
            if (fileDialog.ShowDialog() == true)
            {
                fileNames = fileDialog.FileNames;
                return true;
            }
            fileNames = null;
            return false;
        }

        public bool ShowDirectoryDialog([MaybeNullWhen(false)] out string folderName)
        {
            OpenFolderDialog openFolder = new();
            if (openFolder.ShowDialog() == true)
            {
                folderName = openFolder.FolderName;
                return true;
            }
            folderName = null;
            return false;
        }
    }
}

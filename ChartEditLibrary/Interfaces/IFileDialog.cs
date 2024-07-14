using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Interfaces
{
    public interface IFileDialog
    {
        bool ShowDialog(string? fileName, [MaybeNullWhen(false)] out string[] fileNames);
        bool ShowDirectoryDialog([MaybeNullWhen(false)] out string folderName);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Interfaces
{
    public interface IFileDialog
    {
        string[]? FileNames { get; set; }
        string? FileName { get; set; }
        bool ShowDialog();
    }
}

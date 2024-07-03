using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Interfaces
{
    public interface IFileDialog
    {
        string FileName { get; set; }
        bool ShowDialog();
    }
}

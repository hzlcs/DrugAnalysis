using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Interfaces
{
    public interface IInputForm
    {
        bool TryGetInput(object? data, out string? value);
    }
}

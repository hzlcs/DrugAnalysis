using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Interfaces
{
    public interface IInputForm
    {
        bool TryGetInput(object? data, [MaybeNullWhen(false)] out string value);
    }
}

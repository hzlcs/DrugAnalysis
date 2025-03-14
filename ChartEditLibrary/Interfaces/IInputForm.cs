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

        T ShowCombboxDialog<T>(string title, T[] options, bool canEdit = false);

        T[]? ShowListDialog<T>(string title, string itemName, T[] options);
    }
}

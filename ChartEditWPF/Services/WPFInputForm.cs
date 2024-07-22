using ChartEditLibrary.Interfaces;
using ChartEditWPF.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWPF.Services
{
    internal class WPFInputForm : IInputForm
    {
        private string? result;

        public bool TryGetInput(object? data, [MaybeNullWhen(false)] out string value)
        {
            var res = new InputWindow(data?.ToString(), InputWindow_Callback).ShowDialog();
            value = result;
            return res.GetValueOrDefault();
        }

        private void InputWindow_Callback(string obj)
        {
            result = obj;
        }
    }
}

using ChartEditLibrary.Interfaces;
using ChartEditWinform.ChartCore.UserForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWinform.Services
{
    internal class WinformInputForm : IInputForm
    {

        public bool TryGetInput(object? data, out string? value)
        {
            var inputForm = new InputDPForm(data?.ToString());
            bool res = inputForm.ShowDialog() == DialogResult.OK;
            value = res ? inputForm.DPValue : null;
            return res;
        }
    }
}

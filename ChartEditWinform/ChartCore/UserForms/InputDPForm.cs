using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChartEditWinform.ChartCore.UserForms
{
    public partial class InputDPForm : Form
    {
        public string? DPValue { get; private set; }

        public InputDPForm()
        {
            InitializeComponent();
        }

        public InputDPForm(string? value) : this()
        {
            textBox1.Text = value?.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DPValue = textBox1.Text;
            if(DPValue.StartsWith("dp", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("请去除DP前缀");
                return;
            }
            if(string.IsNullOrWhiteSpace(DPValue))
            {
                DPValue = null;
            }
            else
            {
                if(!DPRegex().IsMatch(DPValue))
                {
                    MessageBox.Show("请输入正确的DP值");
                    return;
                }
            }

            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        [GeneratedRegex(@"(\d+)((-\d)?$)")]
        private static partial Regex DPRegex();
    }
}

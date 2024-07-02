using ChartEditWinform.Entitys;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChartEditWinform.Forms
{
    public partial class ConfigForm : Form
    {
        public ConfigForm()
        {
            InitializeComponent();
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(Enum.GetValues(typeof(ExportType)).Cast<object>().ToArray());
            comboBox1.SelectedIndex = 0;
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = Config.GetConfig((ExportType)comboBox1.SelectedItem!);
        }

        private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Config.SaveConfig();
        }
    }
}

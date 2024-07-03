using ChartEditLibrary.Entitys;
using ChartEditLibrary.Interfaces;
using ChartEditLibrary.ViewModel;
using ChartEditWinform.ChartCore;
using ChartEditWinform.Controls;
using ChartEditWinform.Entitys;
using ChartEditWinform.Forms;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ScottPlot;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Windows.Forms;

namespace ChartEditWinform
{
    public partial class Form1 : Form
    {
        private readonly List<DataItem> datas = [];
        private readonly BindingSource source;
        readonly OpenFileDialog dialog;
        private readonly IServiceProvider _serviceProvider;
        public Form1()
        {
            InitializeComponent();
            source = new BindingSource()
            {
                DataSource = datas,
            };
            dialog = new OpenFileDialog()
            {
                Filter = "csv文件(*.txt)|*.csv",
                Multiselect = true,
            };
        }

        public Form1(IServiceProvider serviceProvider) : this()
        {
            _serviceProvider = serviceProvider;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView1.InitDataGridView();
            dataGridView1.DataSource = source;
            controlInitHeight = (int)(MainPanel.Height / 3.0);
            Config.LoadConfig();
            LoadCache();
        }

        private void LoadCache()
        {
            if (!File.Exists(cacheFile))
                return;
            CacheContent[] cacheContents = null!;
            try
            {
                var temp = JsonConvert.DeserializeObject<CacheContent[]>(File.ReadAllText(cacheFile));
                if (temp is null)
                    return;
                cacheContents = temp;
            }
            catch
            {
                MessageBox.Show("缓存文件加载失败");
            }
            var vms = cacheContents!.Select(v => DraggableChartVM.Create(v)).ToArray();
            foreach (var vm in vms)
            {
                var data = new DataItem() { DraggableChartVM = vm!, FileName = vm!.FileName };
                datas.Insert(0, data);
                IChartControl chartControl = _serviceProvider.GetRequiredService<IChartControl>();
                chartControl.ChartData = vm;
                var control = new ShowControl(chartControl) { Dock = DockStyle.Top };
                data.Control = control;
                float height = Math.Max(control.Height, (float)MainPanel.Height / cacheContents.Length);
                control.Width = MainPanel.Width;
                control.Height = (int)height;
                MainPanel.Controls.Add(control);
            }
            source.ResetBindings(false);
        }

        private void ImportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Import(ExportType.Enoxaparin);
        }

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            {
                var data = datas[e.RowIndex];
                data.Selected = !data.Selected;
            }
        }

        private void DataGridView1_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                var data = datas[e.RowIndex];
                data.Selected = !data.Selected;
            }
        }

        private void HideDataButton_Click(object sender, EventArgs e)
        {
            bool selected = datas.Any(v => v.Selected);
            bool hide = HideDataButton.Text[0] == '隐';
            foreach (var i in datas)
                i.Control.ChangeEditView(hide);
            HideDataButton.Text = string.Format(hide ? "显示{0}数据表格" : "隐藏{0}数据表格", selected ? "" : "");
        }

        private void ChartAutoFitButton_Click(object sender, EventArgs e)
        {
            foreach (var i in datas)
                i.Control.AutoFit();
        }

        private void ExportOther_Click(object sender, EventArgs e)
        {
            Import(ExportType.Other);
        }
        int controlInitHeight = 0;
        private void Import(ExportType exportType, object? tag = null)
        {
            if (dialog.ShowDialog() != DialogResult.OK)
                return;
            string[] files = dialog.FileNames;
            var items = GetDataItems(files, exportType, tag);
            foreach (var data in items.Reverse())
            {
                string fileName = data.FileName;
                var existIndex = datas.FindIndex(d => d.FileName == fileName);
                ShowControl control = data.Control;
                MainPanel.Controls.Add(control);
                if (existIndex > 0)
                {
                    var exist = datas[existIndex];
                    datas.RemoveAt(existIndex);
                    datas.Insert(existIndex, data);
                    control.Location = exist.Control.Location;
                    control.Size = exist.Control.Size;
                    exist.Control.Dispose();
                    MainPanel.Controls.Remove(exist.Control);
                    MainPanel.Controls.SetChildIndex(control, existIndex);
                }
                else
                {
                    datas.Insert(0, data);
                    if (datas.Count > 1)
                    {
                        var last = datas[^1].Control;
                        control.Location = new Point(last.Location.X, last.Location.Y + last.Height + 5);

                    }
                }

            }
            foreach (Control control in MainPanel.Controls)
            {
                float height = Math.Max(controlInitHeight, (float)MainPanel.Height / MainPanel.Controls.Count);
                control.Width = MainPanel.Width;
                control.Height = (int)height;
            }

            source.ResetBindings(false);
        }

        private DataItem[] GetDataItems(string[] files, ExportType exportedType, object? tag)
        {
            var tasks = files.Select(async v =>
            {
                try
                {
                    var vm = await DraggableChartVM.CreateAsync(v, exportedType).ConfigureAwait(false);
                    vm.InitSplitLine(tag);
                    return vm;
                }
                catch (Exception ex)
                {
                    while (ex.InnerException is not null)
                        ex = ex.InnerException;
                    MessageBox.Show(ex.Message);
                    return null;
                }

            }).ToArray();
            Task.WaitAll(tasks);
            return tasks.Select(v => v.Result).Where(v => v is not null)
                .Select(v =>
                {
                    IChartControl  chartControl = _serviceProvider.GetRequiredService<IChartControl>();
                    chartControl.ChartData = v;
                    return new DataItem() { DraggableChartVM = v, Control = new ShowControl(chartControl) { Dock = DockStyle.Top }, FileName = v.FileName };
                }).ToArray();
        }

        private void ConfigToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            new ConfigForm().ShowDialog();
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
                return;
            if (!Directory.Exists(folderBrowserDialog1.SelectedPath))
            {
                Directory.CreateDirectory(folderBrowserDialog1.SelectedPath);
            }
            Task.WaitAll(datas.Select(d => d.Control.Export(folderBrowserDialog1.SelectedPath)).ToArray());
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            var selected = datas.Where(d => d.Selected).ToList();
            if (selected.Count == 0)
            {
                MainPanel.Controls.Clear();
                foreach (var d in datas)
                    d.Control.Dispose();
                datas.Clear();
            }
            else
            {
                foreach (var i in selected)
                {
                    MainPanel.Controls.Remove(i.Control);
                    i.Control.Dispose();
                    datas.Remove(i);
                }
            }
            source.ResetBindings(false);
        }

        readonly string cacheFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ChartEdit\\cache.json");

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            if (!Directory.Exists(Path.GetDirectoryName(cacheFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
            var contenets = datas.Select(v =>
            {
                var vm = v.DraggableChartVM;
                CacheContent cache = new CacheContent()
                {
                    FileName = vm.FileName,
                    X = vm.DataSource.Select(v => v.X).ToArray(),
                    Y = vm.DataSource.Select(v => v.Y).ToArray(),
                    SaveContent = vm.GetSaveContent()
                };
                return cache;
            }).Reverse();
            File.WriteAllText(cacheFile, JsonConvert.SerializeObject(contenets));
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Import(ExportType.Enoxaparin, 4);
        }
    }

    struct FileItem
    {
        public int index;
        public string fileName;

        public FileItem(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            int index = fileName.IndexOf('_');
            this.index = int.Parse(fileName[..index]);
            this.fileName = fileName[(index + 1)..];
        }
    }

}

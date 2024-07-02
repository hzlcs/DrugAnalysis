using ChartEditWinform.Controls;

namespace ChartEditWinform
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            menuStrip1 = new MenuStrip();
            文件ToolStripMenuItem = new ToolStripMenuItem();
            导入ToolStripMenuItem = new ToolStripMenuItem();
            ExportOther = new ToolStripMenuItem();
            配置ToolStripMenuItem = new ToolStripMenuItem();
            ConfigToolStripMenuItem1 = new ToolStripMenuItem();
            tableLayoutPanel1 = new TableLayoutPanel();
            MainPanel = new ScrollPanel();
            tableLayoutPanel2 = new TableLayoutPanel();
            dataGridView1 = new DataGridView();
            SelectColumn = new DataGridViewCheckBoxColumn();
            FileName = new DataGridViewTextBoxColumn();
            panel1 = new Panel();
            ClearButton = new Button();
            ExportButton = new Button();
            ChartAutoFitButton = new Button();
            HideDataButton = new Button();
            folderBrowserDialog1 = new FolderBrowserDialog();
            toolStripMenuItem1 = new ToolStripMenuItem();
            menuStrip1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { 文件ToolStripMenuItem, 配置ToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1898, 32);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // 文件ToolStripMenuItem
            // 
            文件ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 导入ToolStripMenuItem, toolStripMenuItem1, ExportOther });
            文件ToolStripMenuItem.Name = "文件ToolStripMenuItem";
            文件ToolStripMenuItem.Size = new Size(62, 28);
            文件ToolStripMenuItem.Text = "文件";
            // 
            // 导入ToolStripMenuItem
            // 
            导入ToolStripMenuItem.Name = "导入ToolStripMenuItem";
            导入ToolStripMenuItem.Size = new Size(285, 34);
            导入ToolStripMenuItem.Text = "导入依诺";
            导入ToolStripMenuItem.Click += ImportToolStripMenuItem_Click;
            // 
            // ExportOther
            // 
            ExportOther.Name = "ExportOther";
            ExportOther.Size = new Size(285, 34);
            ExportOther.Text = "导入其他";
            ExportOther.Click += ExportOther_Click;
            // 
            // 配置ToolStripMenuItem
            // 
            配置ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { ConfigToolStripMenuItem1 });
            配置ToolStripMenuItem.Name = "配置ToolStripMenuItem";
            配置ToolStripMenuItem.Size = new Size(62, 28);
            配置ToolStripMenuItem.Text = "配置";
            // 
            // ConfigToolStripMenuItem1
            // 
            ConfigToolStripMenuItem1.Name = "ConfigToolStripMenuItem1";
            ConfigToolStripMenuItem1.Size = new Size(146, 34);
            ConfigToolStripMenuItem1.Text = "配置";
            ConfigToolStripMenuItem1.Click += ConfigToolStripMenuItem1_Click;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 337F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(MainPanel, 1, 0);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 32);
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1898, 992);
            tableLayoutPanel1.TabIndex = 3;
            // 
            // MainPanel
            // 
            MainPanel.AutoScroll = true;
            MainPanel.Dock = DockStyle.Fill;
            MainPanel.Location = new Point(340, 3);
            MainPanel.Name = "MainPanel";
            MainPanel.Size = new Size(1555, 986);
            MainPanel.TabIndex = 1;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Controls.Add(dataGridView1, 0, 0);
            tableLayoutPanel2.Controls.Add(panel1, 0, 1);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(3, 3);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Size = new Size(331, 986);
            tableLayoutPanel2.TabIndex = 0;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.BackgroundColor = SystemColors.Control;
            dataGridView1.ColumnHeadersHeight = 34;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { SelectColumn, FileName });
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(3, 3);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.ScrollBars = ScrollBars.Vertical;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.Size = new Size(325, 487);
            dataGridView1.TabIndex = 0;
            dataGridView1.CellClick += DataGridView1_CellClick;
            dataGridView1.CellContentDoubleClick += DataGridView1_CellContentDoubleClick;
            // 
            // SelectColumn
            // 
            SelectColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            SelectColumn.DataPropertyName = "Selected";
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.NullValue = false;
            dataGridViewCellStyle3.SelectionBackColor = Color.White;
            dataGridViewCellStyle3.SelectionForeColor = Color.White;
            SelectColumn.DefaultCellStyle = dataGridViewCellStyle3;
            SelectColumn.FalseValue = "false";
            SelectColumn.HeaderText = "选择";
            SelectColumn.IndeterminateValue = "";
            SelectColumn.MinimumWidth = 8;
            SelectColumn.Name = "SelectColumn";
            SelectColumn.ReadOnly = true;
            SelectColumn.Resizable = DataGridViewTriState.False;
            SelectColumn.TrueValue = "true";
            SelectColumn.Width = 70;
            // 
            // FileName
            // 
            FileName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            FileName.DataPropertyName = "FileName";
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            FileName.DefaultCellStyle = dataGridViewCellStyle4;
            FileName.HeaderText = "样品";
            FileName.MinimumWidth = 8;
            FileName.Name = "FileName";
            FileName.ReadOnly = true;
            // 
            // panel1
            // 
            panel1.Controls.Add(ClearButton);
            panel1.Controls.Add(ExportButton);
            panel1.Controls.Add(ChartAutoFitButton);
            panel1.Controls.Add(HideDataButton);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 496);
            panel1.Name = "panel1";
            panel1.Size = new Size(325, 487);
            panel1.TabIndex = 1;
            // 
            // ClearButton
            // 
            ClearButton.Font = new Font("Microsoft YaHei UI", 12F);
            ClearButton.Location = new Point(6, 210);
            ClearButton.Name = "ClearButton";
            ClearButton.Size = new Size(316, 57);
            ClearButton.TabIndex = 3;
            ClearButton.Text = "移除样品";
            ClearButton.UseVisualStyleBackColor = true;
            ClearButton.Click += ClearButton_Click;
            // 
            // ExportButton
            // 
            ExportButton.Font = new Font("Microsoft YaHei UI", 12F);
            ExportButton.Location = new Point(6, 147);
            ExportButton.Name = "ExportButton";
            ExportButton.Size = new Size(316, 57);
            ExportButton.TabIndex = 2;
            ExportButton.Text = "批量导出图表和数据";
            ExportButton.UseVisualStyleBackColor = true;
            ExportButton.Click += ExportButton_Click;
            // 
            // ChartAutoFitButton
            // 
            ChartAutoFitButton.Font = new Font("Microsoft YaHei UI", 12F);
            ChartAutoFitButton.Location = new Point(6, 84);
            ChartAutoFitButton.Name = "ChartAutoFitButton";
            ChartAutoFitButton.Size = new Size(316, 57);
            ChartAutoFitButton.TabIndex = 1;
            ChartAutoFitButton.Text = "图表自适应";
            ChartAutoFitButton.UseVisualStyleBackColor = true;
            ChartAutoFitButton.Click += ChartAutoFitButton_Click;
            // 
            // HideDataButton
            // 
            HideDataButton.Font = new Font("Microsoft YaHei UI", 12F);
            HideDataButton.Location = new Point(6, 21);
            HideDataButton.Name = "HideDataButton";
            HideDataButton.Size = new Size(316, 57);
            HideDataButton.TabIndex = 0;
            HideDataButton.Text = "隐藏数据表格";
            HideDataButton.UseVisualStyleBackColor = true;
            HideDataButton.Click += HideDataButton_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(270, 34);
            toolStripMenuItem1.Text = "导入依诺-dp4";
            toolStripMenuItem1.Click += toolStripMenuItem1_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1898, 1024);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(menuStrip1);
            KeyPreview = true;
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Form1";
            WindowState = FormWindowState.Maximized;
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private MenuStrip menuStrip1;
        private ToolStripMenuItem 文件ToolStripMenuItem;
        private ToolStripMenuItem 导入ToolStripMenuItem;
        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private DataGridView dataGridView1;
        private Panel panel1;
        private DataGridViewCheckBoxColumn SelectColumn;
        private DataGridViewTextBoxColumn FileName;
        private Button HideDataButton;
        private ScrollPanel MainPanel;
        private Button ChartAutoFitButton;
        private ToolStripMenuItem ExportOther;
        private ToolStripMenuItem 配置ToolStripMenuItem;
        private ToolStripMenuItem ConfigToolStripMenuItem1;
        private Button ExportButton;
        private FolderBrowserDialog folderBrowserDialog1;
        private Button ClearButton;
        private ToolStripMenuItem toolStripMenuItem1;
    }
}

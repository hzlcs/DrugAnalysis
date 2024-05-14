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
            draggableChart1 = new ChartCore.DraggableChartControl();
            tableLayoutPanel1 = new TableLayoutPanel();
            chartEditControl1 = new ChartCore.ChartEditControl();
            menuStrip1 = new MenuStrip();
            文件ToolStripMenuItem = new ToolStripMenuItem();
            导入ToolStripMenuItem = new ToolStripMenuItem();
            tableLayoutPanel1.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // draggableChart1
            // 
            draggableChart1.BackColor = Color.White;
            draggableChart1.ChartData = null;
            draggableChart1.Dock = DockStyle.Fill;
            draggableChart1.Location = new Point(3, 3);
            draggableChart1.Name = "draggableChart1";
            draggableChart1.Size = new Size(876, 806);
            draggableChart1.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 696F));
            tableLayoutPanel1.Controls.Add(draggableChart1, 0, 0);
            tableLayoutPanel1.Controls.Add(chartEditControl1, 1, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 32);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1578, 812);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // chartEditControl1
            // 
            chartEditControl1.AutoSize = true;
            chartEditControl1.Dock = DockStyle.Fill;
            chartEditControl1.DragData = null;
            chartEditControl1.Location = new Point(885, 3);
            chartEditControl1.Name = "chartEditControl1";
            chartEditControl1.Size = new Size(690, 806);
            chartEditControl1.TabIndex = 1;
            chartEditControl1.Load += chartEditControl1_Load;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { 文件ToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1578, 32);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // 文件ToolStripMenuItem
            // 
            文件ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 导入ToolStripMenuItem });
            文件ToolStripMenuItem.Name = "文件ToolStripMenuItem";
            文件ToolStripMenuItem.Size = new Size(62, 28);
            文件ToolStripMenuItem.Text = "文件";
            // 
            // 导入ToolStripMenuItem
            // 
            导入ToolStripMenuItem.Name = "导入ToolStripMenuItem";
            导入ToolStripMenuItem.Size = new Size(146, 34);
            导入ToolStripMenuItem.Text = "导入";
            导入ToolStripMenuItem.Click += 导入ToolStripMenuItem_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1578, 844);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(menuStrip1);
            KeyPreview = true;
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ChartCore.DraggableChartControl draggableChart1;
        private TableLayoutPanel tableLayoutPanel1;
        private ChartCore.ChartEditControl chartEditControl1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem 文件ToolStripMenuItem;
        private ToolStripMenuItem 导入ToolStripMenuItem;
    }
}

namespace ChartEditWinform.ChartCore
{
    partial class ChartEditControl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            panel1 = new Panel();
            dataGridView1 = new DataGridView();
            indexC = new DataGridViewTextBoxColumn();
            RTC = new DataGridViewTextBoxColumn();
            areaC = new DataGridViewTextBoxColumn();
            startC = new DataGridViewTextBoxColumn();
            endC = new DataGridViewTextBoxColumn();
            radioC = new DataGridViewTextBoxColumn();
            dpC = new DataGridViewTextBoxColumn();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(panel1, 0, 1);
            tableLayoutPanel1.Controls.Add(dataGridView1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 93.24074F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 6.759259F));
            tableLayoutPanel1.Size = new Size(920, 1080);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 1010);
            panel1.Name = "panel1";
            panel1.Size = new Size(914, 67);
            panel1.TabIndex = 0;
            // 
            // dataGridView1
            // 
            dataGridView1.BackgroundColor = Color.White;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { indexC, RTC, areaC, startC, endC, radioC, dpC });
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(3, 3);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new Size(914, 1001);
            dataGridView1.TabIndex = 1;
            // 
            // indexC
            // 
            indexC.HeaderText = "峰";
            indexC.MinimumWidth = 8;
            indexC.Name = "indexC";
            indexC.ReadOnly = true;
            indexC.Width = 150;
            // 
            // RTC
            // 
            RTC.HeaderText = "RT";
            RTC.MinimumWidth = 8;
            RTC.Name = "RTC";
            RTC.ReadOnly = true;
            RTC.Width = 150;
            // 
            // areaC
            // 
            areaC.HeaderText = "面积";
            areaC.MinimumWidth = 8;
            areaC.Name = "areaC";
            areaC.ReadOnly = true;
            areaC.Width = 150;
            // 
            // startC
            // 
            startC.HeaderText = "开始";
            startC.MinimumWidth = 8;
            startC.Name = "startC";
            startC.ReadOnly = true;
            startC.Width = 150;
            // 
            // endC
            // 
            endC.HeaderText = "结束";
            endC.MinimumWidth = 8;
            endC.Name = "endC";
            endC.ReadOnly = true;
            endC.Width = 150;
            // 
            // radioC
            // 
            radioC.HeaderText = "峰面积总和%";
            radioC.MinimumWidth = 8;
            radioC.Name = "radioC";
            radioC.ReadOnly = true;
            radioC.Width = 150;
            // 
            // dpC
            // 
            dpC.HeaderText = "DP";
            dpC.MinimumWidth = 8;
            dpC.Name = "dpC";
            dpC.ReadOnly = true;
            dpC.Width = 150;
            // 
            // ChartEditControl
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tableLayoutPanel1);
            Name = "ChartEditControl";
            Size = new Size(920, 1080);
            Load += ChartEditControl_Load;
            tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private Panel panel1;
        private DataGridView dataGridView1;
        private DataGridViewTextBoxColumn indexC;
        private DataGridViewTextBoxColumn RTC;
        private DataGridViewTextBoxColumn areaC;
        private DataGridViewTextBoxColumn startC;
        private DataGridViewTextBoxColumn endC;
        private DataGridViewTextBoxColumn radioC;
        private DataGridViewTextBoxColumn dpC;
    }
}

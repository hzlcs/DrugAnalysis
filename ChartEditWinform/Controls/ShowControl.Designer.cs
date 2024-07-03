namespace ChartEditWinform.Controls
{
    partial class ShowControl
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
            chartEditControl1 = new ChartCore.ChartEditControl();
            draggableChartControl1 = new ChartCore.DraggableChartControl();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 650F));
            tableLayoutPanel1.Controls.Add(chartEditControl1, 1, 0);
            tableLayoutPanel1.Controls.Add(draggableChartControl1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1500, 441);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // chartEditControl1
            // 
            chartEditControl1.Dock = DockStyle.Fill;
            chartEditControl1.DragData = null;
            chartEditControl1.Location = new Point(853, 3);
            chartEditControl1.Name = "chartEditControl1";
            chartEditControl1.Size = new Size(644, 435);
            chartEditControl1.TabIndex = 0;
            // 
            // draggableChartControl1
            // 
            draggableChartControl1.Dock = DockStyle.Fill;
            draggableChartControl1.Location = new Point(3, 3);
            draggableChartControl1.Name = "draggableChartControl1";
            draggableChartControl1.Size = new Size(844, 435);
            draggableChartControl1.TabIndex = 1;
            // 
            // ShowControl
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tableLayoutPanel1);
            Name = "ShowControl";
            Size = new Size(1500, 441);
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private ChartCore.ChartEditControl chartEditControl1;
        private ChartCore.DraggableChartControl draggableChartControl1;
    }
}

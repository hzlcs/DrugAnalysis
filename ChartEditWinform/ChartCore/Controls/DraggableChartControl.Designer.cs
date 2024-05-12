namespace ChartEditWinform.ChartCore
{
    partial class DraggableChartControl
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
            chartPlot = new ScottPlot.WinForms.FormsPlot();
            SuspendLayout();
            // 
            // chartPlot
            // 
            chartPlot.DisplayScale = 1.5F;
            chartPlot.Dock = DockStyle.Fill;
            chartPlot.Location = new Point(0, 0);
            chartPlot.Name = "chartPlot";
            chartPlot.Size = new Size(1324, 855);
            chartPlot.TabIndex = 0;
            // 
            // DraggableChartControl
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(chartPlot);
            Name = "DraggableChartControl";
            Size = new Size(1324, 855);
            ResumeLayout(false);
        }

        #endregion

        private ScottPlot.WinForms.FormsPlot chartPlot;
    }
}

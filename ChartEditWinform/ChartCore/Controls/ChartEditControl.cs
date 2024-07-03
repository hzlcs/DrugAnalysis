using ChartEditLibrary.Interfaces;
using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
using Newtonsoft.Json.Linq;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;

namespace ChartEditWinform.ChartCore
{
    public partial class ChartEditControl : UserControl, IChartDataControl
    {
        EditLineBase? currentDraggedLine;
        private DraggableChartVM? dragData;

        public ChartEditControl()
        {
            InitializeComponent();
        }


        public void SplitLines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            dataGridView1.RowCount = Math.Max(1, dragData!.SplitLines.Count);
        }

        public void BaseLineDataChanged()
        {
            if (currentDraggedLine is not null && currentDraggedLine is BaseLine)
            {
                dataGridView1.Refresh();
            }
        }

        private void ChartEditControl_Load(object sender, EventArgs e)
        {
            dataGridView1.InitDataGridView();
            dataGridView1.VirtualMode = true;
            dataGridView1.CellValueNeeded += DataGridView1_CellValueNeeded;
            dataGridView1.MultiSelect = false;

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.Width = 80;
                column.Resizable = DataGridViewTriState.False;
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            indexC.Width = 50;
            radioC.Width = 135;
            RTC.DefaultCellStyle.Format = "0.000";
            startC.DefaultCellStyle.Format = "0.000";
            endC.DefaultCellStyle.Format = "0.000";
            areaC.DefaultCellStyle.Format = "0.00";
            radioC.DefaultCellStyle.Format = "0.00";

        }

        private void DataGridView1_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            e.Value = dragData?.SplitLines[e.RowIndex][e.ColumnIndex];
        }

        public void DraggedLineChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is null)
                return;

            DraggableChartVM vm = (DraggableChartVM)sender;
            if (e.PropertyName != nameof(vm.DraggedLine))
                return;
            dataGridView1.Refresh();
            dataGridView1.ClearSelection();
            if (currentDraggedLine != null)
            {
                currentDraggedLine.PropertyChanged -= CurrentSplitLine_PropertyChanged;
            }

            if (vm.DraggedLine is null)
            {
                return;
            }

            currentDraggedLine = vm.DraggedLine.Value.DraggedLine;
            CurrentSplitLine_PropertyChanged(currentDraggedLine, new PropertyChangedEventArgs(nameof(currentDraggedLine.Line)));
            currentDraggedLine.PropertyChanged += CurrentSplitLine_PropertyChanged;

        }

        private DateTime refreshTime = DateTime.Now;

        public void CurrentSplitLine_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not SplitLine line)
                return;
            if (e.PropertyName != nameof(EditLineBase.Line))
                return;
            UpdateData(line);
        }

        private void UpdateData(SplitLine line)
        {
            SplitLine? nextLine = dragData?.SplitLines.ElementAtOrDefault(line.Index);
            //dataGridView1.ClearSelection();
            dataGridView1.Rows[line.Index - 1].Selected = true;
            foreach (DataGridViewColumn i in dataGridView1.Columns)
            {
                dataGridView1.Rows[line.Index - 1].Cells[i.Index].Value = null;
                if (nextLine is not null)
                {
                    dataGridView1.Rows[line.Index].Cells[i.Index].Value = null;
                }
            }
            if (line.Equals(dragData?.SplitLines[^1]))
            {
                foreach (var i in dragData.SplitLines)
                {
                    dataGridView1.Rows[i.Index - 1].Cells[5].Value = null;
                }
            }
        }

        public void BindData(DraggableChartVM vm)
        {
            if (dragData is not null)
            {
                dragData.PropertyChanged -= DraggedLineChanged;
                dragData.OnBaseLineChanged -= BaseLineDataChanged;
                dragData.SplitLines.CollectionChanged -= SplitLines_CollectionChanged;
            }
            dragData = vm;
            dragData.PropertyChanged += DraggedLineChanged;
            dragData.OnBaseLineChanged += BaseLineDataChanged;
            dragData.SplitLines.CollectionChanged += SplitLines_CollectionChanged;
            dataGridView1.Rows.Clear();
            dataGridView1.RowCount = dragData.SplitLines.Count + 1;
        }
    }
}

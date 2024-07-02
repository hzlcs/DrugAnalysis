using ChartEditLibrary.Model;
using ChartEditLibrary.ViewModel;
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
    public partial class ChartEditControl : UserControl
    {
        EditLineBase? currentDraggedLine;
        private DraggableChartVM? dragData;

        public ChartEditControl()
        {
            InitializeComponent();
        }


        public DraggableChartVM? DragData
        {
            get => dragData;
            set
            {
                if (value is null)
                    return;
                if (dragData is not null)
                {
                    dragData.PropertyChanged -= DraggedLinePropertyChanged;
                    dragData.OnDataChanged -= DraggedLineDataChanged;
                    dragData.SplitLines.CollectionChanged -= SplitLines_CollectionChanged;
                }
                dragData = value;
                dragData.PropertyChanged += DraggedLinePropertyChanged;
                dragData.OnDataChanged += DraggedLineDataChanged;
                dragData.SplitLines.CollectionChanged += SplitLines_CollectionChanged;
                dataGridView1.Rows.Clear();
                dataGridView1.RowCount = dragData.SplitLines.Count + 1;
            }
        }

        private void SplitLines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            dataGridView1.RowCount = Math.Max(1, dragData!.SplitLines.Count);
        }

        private void DraggedLineDataChanged()
        {
            if (currentDraggedLine is not null && currentDraggedLine is BaseLine )
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
            e.Value = DragData?.SplitLines[e.RowIndex][e.ColumnIndex];
        }

        private void DraggedLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
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
                currentDraggedLine.PropertyChanged -= CurrentLine_PropertyChanged;
            }

            if (vm.DraggedLine is null)
            {
                return;
            }

            currentDraggedLine = vm.DraggedLine.Value.DraggedLine;
            CurrentLine_PropertyChanged(currentDraggedLine, new PropertyChangedEventArgs(nameof(currentDraggedLine.Line)));
            currentDraggedLine.PropertyChanged += CurrentLine_PropertyChanged;

        }

        private DateTime refreshTime = DateTime.Now;

        private void CurrentLine_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is null)
                return;
            if (e.PropertyName != nameof(EditLineBase.Line))
                return;
            if (sender is SplitLine line)
            {
                UpdateData(line);
            }

        }

        private void UpdateData(SplitLine line)
        {
            SplitLine? nextLine = DragData?.SplitLines.ElementAtOrDefault(line.Index);
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
            if (line.Equals(DragData?.SplitLines[^1]))
            {
                foreach(var i in DragData.SplitLines)
                {
                    dataGridView1.Rows[i.Index - 1].Cells[5].Value = null;
                }
            }
        }
    }
}

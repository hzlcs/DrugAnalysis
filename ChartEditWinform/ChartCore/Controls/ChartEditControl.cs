using ChartEditWinform.ChartCore.Entity;
using ChartEditWinform.ChartCore.Interface;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
                    dragData.PropertyChanged -= DraggedLinePropertyChanged;
                dragData = value;
                richTextBox1.Text = dragData.GetDescription(out var index);
                dragData.PropertyChanged += DraggedLinePropertyChanged;
            }
        }

        private void ChartEditControl_Load(object sender, EventArgs e)
        {



        }

        private void DraggedLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is null)
                return;

            DraggableChartVM vm = (DraggableChartVM)sender;
            if (e.PropertyName != nameof(vm.DraggedLine))
                return;
            if (currentDraggedLine != null)
            {
                currentDraggedLine.PropertyChanged -= CurrentLine_PropertyChanged;
            }

            if (vm.DraggedLine is null)
            {
                richTextBox1.Text = vm.GetDescription(out var index);
                return;
            }

            currentDraggedLine = vm.DraggedLine.Value.DraggedLine;
            CurrentLine_PropertyChanged(currentDraggedLine, new PropertyChangedEventArgs(nameof(currentDraggedLine.Line)));
            currentDraggedLine.PropertyChanged += CurrentLine_PropertyChanged;
        }


        private void CurrentLine_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(sender is null) 
                return;
            if (e.PropertyName != nameof(EditLineBase.Line))
                return;
            
            richTextBox1.Text = DragData!.GetDescription(out var index);
            if (index.HasValue)
            {
                int start = richTextBox1.Find(richTextBox1.Lines[index.Value]);
                richTextBox1.SelectionStart = start;
                richTextBox1.SelectionLength = richTextBox1.Lines[index.Value].Length;
                richTextBox1.SelectionColor = Color.Red;
                richTextBox1.HideSelection = true;
            }
        }
    }
}

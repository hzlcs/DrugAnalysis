using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChartEditWinform.Controls
{
    public partial class ScrollPanel : Panel
    {
        public ScrollPanel()
        {
            InitializeComponent();
            //AutoScroll = true;
            VerticalScroll.Enabled = true;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if(e.Location.X > Width - 50)
            {
                base.OnMouseWheel(e);
            }

            
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
        }

    }
}

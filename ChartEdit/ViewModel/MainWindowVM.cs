
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Drawing;

namespace ChartEdit.ViewModel
{
    internal partial class MainWindowVM : ObservableObject
    {


        public MainWindowVM()
        {

            InitSerires();

            //ReadCsv("D:\\药物分析\\寡糖数据.csv\\范本-1-131201103-1.csv");
        }

        private void InitSerires()
        {

        }

        private async Task<PointF[]> ReadCsv(string file)
        {
            var data = await File.ReadAllLinesAsync(file);

            PointF[] points =
             data.Skip(2).Select(v =>
            {
                var arr = v.Split(',');
                return new PointF(float.Parse(arr[1]), float.Parse(arr[2]));
            }).ToArray();

            return points;
        }

    }


}

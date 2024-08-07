﻿using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditWinform
{
    internal static class Session
    {
        

    }

    public static class Utility 
    {
        public static async Task<Coordinates[]> ReadCsv(string path, int skip = 0)
        {
            char[] separator = ['\n'];
            char[] spe = [',', '\t'];
            string[] data;
            using (StreamReader sr = new(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                sr.ReadLine();
                sr.ReadLine();
                data = (await sr.ReadToEndAsync().ConfigureAwait(false)).Split(separator, StringSplitOptions.RemoveEmptyEntries);
            }
            try
            {
                double[][] temp = data.Select(v => v.Split(spe).Skip(skip).Select(v1 => double.Parse(v1)).ToArray())
                .Where(v => v[0] >= 20 && v[0] <= 60).ToArray();
                return temp.Select(v => new Coordinates(v[0], v[1])).ToArray();
            }
            catch
            {
                throw new Exception(Path.GetFileNameWithoutExtension(path) + "数据格式错误");
            }
            
        }

        public static void InitDataGridView(this DataGridView dataGridView)
        {
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.AllowUserToOrderColumns = false;
            dataGridView.AllowUserToResizeColumns = false;
            dataGridView.AllowUserToResizeRows = false;
            dataGridView.AutoGenerateColumns = false;
            dataGridView.RowHeadersWidth = 20;
        }
    }

}

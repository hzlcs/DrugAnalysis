using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Model
{
    public class TwoDFileInfo : IComparer<TwoDFileInfo>, IComparable<TwoDFileInfo>
    {
        public string SampleName { get; }
        public string FileName { get; }
        public string FilePath { get; }
        public string DirectionName { get; }
        public string SampleNameWithDirection => $"{DirectionName}\\{SampleName}";
        public int? Extension { get; }

        public TwoDFileInfo(string fileName)
        {
            this.FilePath = fileName;
            DirectionName = Path.GetDirectoryName(fileName)!;
            FileName = Path.GetFileNameWithoutExtension(fileName);
            int index = FileName.LastIndexOf('-');
            if (index == -1)
            {
                Extension = null;
                SampleName = FileName;
            }
            else
            {
                SampleName = FileName[..index];
                string temp = FileName[(index + 1)..].ToLower();
                if (temp.Contains("cut"))
                    Extension = 0;
                else if (temp.Contains("dp"))
                {
                    Extension = int.Parse(temp.Substring(2));
                }
                else
                    Extension = null;
            }
        }

        public int Compare(TwoDFileInfo? x, TwoDFileInfo? y)
        {
            if(x is null || y is null)
                return 0;
            if (x.Extension is null)
                return -1;
            if (y.Extension is null)
                return 1;
            return x.Extension.Value.CompareTo(y.Extension.Value);
        }

        public int CompareTo(TwoDFileInfo? other)
        {
            return Compare(this, other);
        }
    }
}

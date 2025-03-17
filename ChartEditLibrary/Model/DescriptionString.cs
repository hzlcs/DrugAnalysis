using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Model
{
    public readonly struct DescriptionString : IComparable<DescriptionString>
    {


        public string Degree { get; }
        public int Sort { get; }
        public string Description { get; }

        public DescriptionString(string description)
        {
            string[] spl = description.Split('-');
            if (spl.Length == 1)
            {
                Degree = spl[0];
                Sort = 1;
                Description = description + "-1";
            }
            else
            {
                Degree = spl[0];
                _ = int.TryParse(spl[1], out int sort);
                Sort = sort;
                Description = description;
            }
        }

        public DescriptionString(string degree, int sort, string description)
        {
            Degree = degree;
            Sort = sort;
            Description = description;
        }

        public static bool operator ==(DescriptionString left, DescriptionString right)
        {
            return left.Description == right.Description;
        }
        public static bool operator !=(DescriptionString left, DescriptionString right)
        {
            return left.Description != right.Description;
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is DescriptionString other)
            {
                return Description == other.Description;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Description.GetHashCode();
        }

        public int CompareTo(DescriptionString other)
        {
            int compare = string.Compare(Degree, other.Degree);
            if (compare != 0)
                return compare;
            return Sort.CompareTo(other.Sort);
        }
    }
}

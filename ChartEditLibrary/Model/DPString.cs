using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Model
{
    internal readonly struct DescriptionString
    {
        public string Description { get; }

        public DescriptionString(string description)
        {
            if(description.Contains('-'))
                Description = description;
            else
                Description = description + "-1";
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
            if(obj is DescriptionString other)
            {
                return Description == other.Description;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Description.GetHashCode();
        }
    }
}

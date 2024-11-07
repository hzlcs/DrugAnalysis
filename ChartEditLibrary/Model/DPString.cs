using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartEditLibrary.Model
{
    internal readonly struct DPString
    {
        public string DP { get; }

        public DPString(string dp)
        {
            if(dp.Contains('-'))
                DP = dp;
            else
                DP = dp + "-1";
        }

        public static bool operator ==(DPString left, DPString right)
        {
            return left.DP == right.DP;
        }
        public static bool operator !=(DPString left, DPString right)
        {
            return left.DP != right.DP;
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if(obj is DPString other)
            {
                return DP == other.DP;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return DP.GetHashCode();
        }
    }
}

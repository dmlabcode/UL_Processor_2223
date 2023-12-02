using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UL_Processor_V2023
{
    public class UbiLocation
    {
        public DateTime time;
        public String type;
        public String tag;
        public String id="";
        public double x=0;
        public double y=0;

    }
    public class DateTimeComparer : IComparer<UbiLocation>
    {
        public int Compare(UbiLocation x, UbiLocation y)
        {
            int sameToSec = x.time.CompareTo(y.time);
            if (sameToSec == 0)
            {
                if (x.time.Millisecond == y.time.Millisecond) return 0;
                return x.time.Millisecond < y.time.Millisecond ? -1 : 1;
            }
            return sameToSec;
        }
    }
}

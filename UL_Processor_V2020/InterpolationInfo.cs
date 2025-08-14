using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UL_Processor_V2023;

namespace UL_Processor_V2020
{
    internal class InterpolationInfo
    {

        public void getInfo(Classroom cr, String grDir)
        {
            cr.processUbiInterpolation();
        }

    }
}

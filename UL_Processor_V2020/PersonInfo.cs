using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UL_Processor_V2023
{
    

    public class PersonInfo
    {
        public String mapId = "";
        public DateTime time;
        public double x = 0;
        public double y = 0;

    }
    public class PersonSuperInfo: PersonInfo
    {
        public double xl = 0;
        public double yl = 0;
        public double xr = 0;
        public double yr = 0;
        public double ori_chaoming = 0;
        public Boolean wasTalking = false;
        //public double vocUttCount = 0;
        //public double adultWordCount = 0;
        public LenaVars lenaVars = new LenaVars();
    }
}

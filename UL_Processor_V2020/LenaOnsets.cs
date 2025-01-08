using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UL_Processor_V2023
{
    public class LenaOnset
    {
        public String itsFile = "";
        public String lenaId = "";
        public String conversationid = "";

        public String id = "";
        public String type = "";
        public String subjectType = "";

        public DateTime startTime;
        public DateTime parentStartTime;
        public DateTime recStartTime;
        public DateTime endTime;

        public double startSec;
        public double endSec;

        public double durSecs = 0;
        public double segmentDurSecs = 0;
        public double count = 0;
        public double tc = 0;
        public double avgDb = 0;
        public double peakDb = 0;
        public Boolean inSocialContactAnyTalking = false;
        public Boolean outsideKidsSocialContactSubjectTalking = false;
        public Boolean inSocialContactSubjectTalking = false;
        public List<String> teachersInContact = new System.Collections.Generic.List<String>();


       // sw.WriteLine("File,Date,Subject,LenaID,SubjectType,conversationid,voctype,recstart,startsec,endsec,starttime,endtime,duration,seg_duration,wordcount,avg_db,avg_peak,turn_taking ");

 
    }
}

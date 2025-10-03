using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static IronPython.Modules.PythonDateTime;
using UL_Processor_V2023;
using System.Xml;
using System.Runtime.Remoting.Contexts;
using static IronPython.Modules._ast;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Collections;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Web.UI;

namespace UL_Processor_V2020
{
 
    internal class TimeFrameObjSegments
    {
        public String pairActivityFile = "";
        public String aliceFile = "";
        public String dir = "C:\\IBSS\\LB1718\\";
        public String mappingFile = "C:\\IBSS\\LB1718\\";
        public Dictionary<String, DateTime> startTimes = new Dictionary<string, DateTime>();
        public int startHour = 7;
        public int endHour = 14;
        public int endMinute = 0;
        public DateTime currentDay;
        public String szDate;
        public Dictionary<DateTime, DayMapping> dayMappings = new Dictionary<DateTime, DayMapping>();
        Dictionary<DateTime, Dictionary<String, List<TimeFrameObj>>> daysMergedSubjectSegments = new Dictionary<DateTime, Dictionary<string, List<TimeFrameObj>>>();
        public TimeFrameObjSegments(String paFile, String aFile, Boolean oldMappingFormat, String mapFile)
        {
            mappingFile = mapFile;
            pairActivityFile = paFile;
            aliceFile = aFile;

            String szDates = "02-16-2018";
            setMappings(szDates);
            foreach (DateTime date in dayMappings.Keys)
            {
                currentDay = date;
                szDate = Utilities.getDateDashStr(date);
                processAndReportLenaAndAliceFiles();
                mergeWithUbi();


                TextWriter sw = new StreamWriter(aliceFile.Replace(".", "_ALICECOMPARE_"+szDate + new Random().Next()) + ".csv");

                sw.WriteLine("DATE,SUBJECT,START_ONSET_SEC,END_ONSET_SEC,ALICE_START_ONSET_SEC,ALICE_END_ONSET_SEC,LENA_START_ONSET_SEC,LENA_END_ONSET_SEC,TYPE,HASALICE,HASLENA,ALICEUTTCOUNT,LENA_UTT,LENAUTTSEGMENT,SOCIALCONTACT");//,LENACRY");

                TextWriter sw2 = new StreamWriter(aliceFile.Replace(".", "_ALICECOMPARE_TOTALSE_" + szDate + new Random().Next()) + ".csv");

                sw2.WriteLine("SUBJECT,LENAUTTS,ALICEUTTS,LENACRIES");

                foreach (String szMapId in daysMergedSubjectSegments[currentDay].Keys)
                {
                    List<TimeFrameObj> mergedSegments = daysMergedSubjectSegments[currentDay][szMapId];
                    foreach (TimeFrameObj s in mergedSegments)
                    {
                        sw.WriteLine(szDate + "," + szMapId + "," + s.timeInSecs + "," + s.timeEndISecs + "," +
                            (s.aliceSegment != null ? s.aliceSegment.timeInSecs : 0) + "," +
                            (s.aliceSegment != null ? s.aliceSegment.timeEndISecs : 0) + "," +
                            (s.lenaSegment != null ? s.lenaSegment.timeInSecs : 0) + "," +
                            (s.lenaSegment != null ? s.lenaSegment.timeEndISecs : 0) + "," +
                            "" + "," + (s.aliceSegment != null ? "TRUE" : "FALSE") + "," +
                            (s.lenaSegment != null ? "TRUE" : "FALSE") + "," +
                            (s.aliceSegment != null ? s.aliceSegment.uttNumber : 0) + "," +
                            (s.lenaSegment != null ? s.lenaSegment.uttNumber : 0) + "," +
                            (s.lenaSegment != null ? s.lenaSegment.segmentNumber : 0) + "," +
                            s.ubiSocialContacts);
                    }
                }
                 
                foreach (String s in dayMappings[currentDay].subjectMappings.Keys)
                {
                    sw2.WriteLine(s + "," + dayMappings[currentDay].subjectMappings[s].lenaUtts + "," +
                        dayMappings[currentDay].subjectMappings[s].aliceUtts + "," +
                        dayMappings[currentDay].subjectMappings[s].lenaCries);
                }

                sw.Close();
                sw2.Close();

            }

                 

        }
        //public Dictionary<DateTime, Dictionary<String, PersonSuperInfo>> ubiTenths = new Dictionary<DateTime, Dictionary<string, PersonSuperInfo>>();

        public void mergeWithUbi()
        {
            string[] ubiLogFiles = Directory.GetFiles(dir + "//" + szDate + "//", "*.log");

            Dictionary<String, List<UbiLocation>> ubiLefts = new Dictionary<String, List<UbiLocation>>();
            Dictionary<String, List<UbiLocation>> ubiRights = new Dictionary<String, List<UbiLocation>>();

            foreach (string file in ubiLogFiles)
            {
                String fileName = Path.GetFileName(file);
                if (fileName.StartsWith("MiamiLocation") && fileName.EndsWith(".log"))
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        while (!sr.EndOfStream)
                        {
                            //Location,00:11:CE:00:00:00:02:14,2023-02-10 08:50:00.835,1.35892486572266,2.7289354801178,0.25,
                            String szLine = sr.ReadLine();
                            String[] lineCols = szLine.Split(',');

                            //Location,PR_LEAP_2122_T3L,2021-10-25 8:46:01.0,1.6005289068222,5.75430334854126,1
                            if (lineCols.Length > 5 && lineCols[5] != "")
                            {
                                UbiLocation ubiLoc = new UbiLocation();
                                ubiLoc.tag = lineCols[1];
                                DateTime lineTime = Convert.ToDateTime(lineCols[2]);
                                ubiLoc.time = lineTime;
                                ubiLoc.x = Convert.ToDouble(lineCols[3]);
                                ubiLoc.y = Convert.ToDouble(lineCols[4]);

                                foreach(String szMapId in dayMappings[currentDay].subjectMappings.Keys)
                                {
                                    if (dayMappings[currentDay].subjectMappings[szMapId].leftTag== ubiLoc.tag)
                                    {
                                        if(!ubiLefts.ContainsKey(szMapId))
                                            ubiLefts.Add(szMapId, new List<UbiLocation>());
                                        ubiLefts[szMapId].Add(ubiLoc);
                                    }
                                    else  if (dayMappings[currentDay].subjectMappings[szMapId].rightTag == ubiLoc.tag)
                                    {
                                        if (!ubiRights.ContainsKey(szMapId))
                                            ubiRights.Add(szMapId, new List<UbiLocation>());
                                        ubiRights[szMapId].Add(ubiLoc);
                                    }
                                }
                                 
                            }
                        }
                    }
                }
            }
           // Dictionary<String, Tuple<double, double, String>> interpolaionSecs = new Dictionary<String, Tuple<double, double, String>>();
            Dictionary<DateTime, Dictionary<String, PersonInfo>> ubiTenthsL = getTenths(ubiLefts);
            Dictionary<DateTime, Dictionary<String, PersonInfo>> ubiTenthsR = getTenths(ubiRights);
            Dictionary<DateTime, Dictionary<String, PersonSuperInfo>> ubiTenths = new Dictionary<DateTime, Dictionary<string, PersonSuperInfo>>();
            foreach (DateTime szTimeStamp in ubiTenthsL.Keys)
            {
                Boolean timeExistsInRights = ubiTenthsR.ContainsKey(szTimeStamp);
                if (timeExistsInRights )
                {
                    foreach (String person in ubiTenthsL[szTimeStamp].Keys)
                    {
                        PersonInfo personInfo = ubiTenthsL[szTimeStamp][person];

                        if (ubiTenthsR.ContainsKey(szTimeStamp) && ubiTenthsR[szTimeStamp].ContainsKey(person))
                        {
                            PersonInfo personInfoR = ubiTenthsR[szTimeStamp][person];
                            PersonSuperInfo spi = new PersonSuperInfo();
                            spi.xl = personInfo.x;
                            spi.yl = personInfo.y;
                            spi.xr = personInfoR.x;
                            spi.yr = personInfoR.y;
                            spi.ori_chaoming = 0;
                            spi.orientation_pi = 0;//degrees
                            spi.orientation_deg = 0;//degrees
                            spi.mapId = person;
                            if(!ubiTenths.ContainsKey(szTimeStamp))
                            {
                                ubiTenths.Add(szTimeStamp, new Dictionary<string, PersonSuperInfo>());
                            }

                            ubiTenths[szTimeStamp].Add(person, spi);
                        }
                    }
                }


            }
            ubiTenths = ubiTenths.OrderBy(x => x.Key).ThenBy(x => x.Key.Millisecond).ToDictionary(x => x.Key, x => x.Value);
            foreach (String szMapId in daysMergedSubjectSegments[currentDay].Keys)
            {
                foreach (TimeFrameObj tfo in daysMergedSubjectSegments[currentDay][szMapId])
                {
                    //Dictionary<DateTime, List<TimeFrameObj>> daysMergedSegments = new Dictionary<DateTime, List<TimeFrameObj>>();

                    DateTime startTime = startTimes[szMapId];

                    foreach(DateTime t in ubiTenths.Keys)
                    {
                        DateTime segmentStartTime = startTime.AddSeconds(tfo.timeInSecs);
                        int ms = segmentStartTime.Millisecond > 0 ? segmentStartTime.Millisecond / 100 * 100 : segmentStartTime.Millisecond;// + 100;
                        segmentStartTime = new DateTime(segmentStartTime.Year, segmentStartTime.Month, segmentStartTime.Day, segmentStartTime.Hour, segmentStartTime.Minute, segmentStartTime.Second, ms);
                        
                        DateTime segmentEndTime = startTime.AddSeconds(tfo.timeEndISecs);
                        ms = segmentEndTime.Millisecond > 0 ? segmentEndTime.Millisecond / 100 * 100 : segmentEndTime.Millisecond;// + 100;
                        segmentEndTime = new DateTime(segmentEndTime.Year, segmentEndTime.Month, segmentEndTime.Day, segmentEndTime.Hour, segmentEndTime.Minute, segmentEndTime.Second, ms);


                        //DateTime time = lenaOnset.startTime;


                        if (t> segmentEndTime)
                        {
                            break;
                        }
                        if(ubiTenths[t].ContainsKey(szMapId)  &&
                                    t >=segmentStartTime && 
                                    t<segmentEndTime)
                        {
                            Boolean isInSocialContact = false;
                            foreach(String szOther in ubiTenths[t].Keys)
                            {
                                if(szMapId!=szOther)
                                {
                                    double dist = Utilities.calcSquaredDist(ubiTenths[t][szMapId], ubiTenths[t][szOther]);
                                    Boolean withinGofR = (dist <= (1.5 * 1.5)) && (dist >= 0);
                                    Tuple<double, double> angles = Utilities.withinOrientationData(ubiTenths[t][szMapId], ubiTenths[t][szOther]);
                                    Boolean orientedCloseness = withinGofR && ((Math.Abs(angles.Item1) <= 45 && Math.Abs(angles.Item2) <= 45));
                                    if(orientedCloseness)
                                    {
                                        isInSocialContact = true;
                                        tfo.ubiSocialContacts += (!tfo.ubiSocialContacts.Contains("|"+ szOther+"|") ?(tfo.ubiSocialContacts!=""? (szOther+"|") :("|"+szOther+"|")):"");
                                       
                                        //break;
                                    }

                                }
                            }
                           // if (isInSocialContact)
                              //  break;

                        }

                    }



                }


            }




        }
        public Tuple<double, double> linearInterpolate(DateTime t, DateTime t1, double xa, double ya, DateTime t2, double xb, double yb)//copied from ClassroomDay
        {
            double x0 = t1.Minute * 60000 + t1.Second * 1000 + t1.Millisecond;
            double x1 = t2.Minute * 60000 + t2.Second * 1000 + t2.Millisecond;
            double x = t.Minute * 60000 + t.Second * 1000 + t.Millisecond;
            /**** got ms totLA***/

            double y0x = xa;
            double y1x = xb;
            double y0y = ya;
            double y1y = yb;

            double xlerp = (y0x * (x1 - x) + y1x * (x - x0)) / (x1 - x0);
            double ylerp = (y0y * (x1 - x) + y1y * (x - x0)) / (x1 - x0);
            return new Tuple<double, double>(xlerp, ylerp);
        }
        public double linearInterpolate(DateTime t, DateTime t1, double y0, DateTime t2, double y1)//copied from ClassroomDay
        {
            double x0 = t1.Minute * 60000 + t1.Second * 1000 + t1.Millisecond;
            double x1 = t2.Minute * 60000 + t2.Second * 1000 + t2.Millisecond;
            double x = t.Minute * 60000 + t.Second * 1000 + t.Millisecond;
            double lerp = (y0 * (x1 - x) + y1 * (x - x0)) / (x1 - x0);
            return lerp;
        }
        public Dictionary<DateTime, Dictionary<String, PersonInfo>> getTenths(Dictionary<String, List<UbiLocation>> ubiLocations)//copied from ClassroomDay
        {
            Dictionary<DateTime, Dictionary<String, PersonInfo>> dayActivities = new Dictionary<DateTime, Dictionary<string, PersonInfo>>();
            foreach (String personId in ubiLocations.Keys)
            {
                List<UbiLocation> ubiLoc = ubiLocations[personId];
                DateTime first = ubiLoc[0].time;//first timestamp
                DateTime last = ubiLoc[ubiLoc.Count - 1].time;//last timestamp

                //targets will begin at closest 100 ms multiple of start
                int ms = first.Millisecond / 100 * 100 + 100;
                if (first.Millisecond % 100 == 0)
                {
                    ms -= 100;
                }
                DateTime target = new DateTime();//will be next .1 sec
                if (ms == 1000)
                {
                    if (first.Second == 59)
                    {
                        target = new DateTime(first.Year, first.Month, first.Day, first.Hour, first.Minute + 1, 0, 0);
                    }
                    else
                    {
                        target = new DateTime(first.Year, first.Month, first.Day, first.Hour, first.Minute, first.Second + 1, 0);
                    }
                }
                else
                {
                    target = new DateTime(first.Year, first.Month, first.Day, first.Hour, first.Minute, first.Second, ms);
                }
                //Boolean subjFoundClose = true;
                string line = "";
                int lastIndex = 0;
                while (target.CompareTo(last) <= 0)
                {
                    /******/
                    //find next time row based on ms
                    //PersonInfo pi = new PersonInfo();
                    UbiLocation pi = new UbiLocation();
                    pi.time = target;
                    int index = ubiLoc.BinarySearch(pi, new DateTimeComparer());
                    if (index < 0)
                    {
                        index = ~index;
                    }

                    if (index > 0)
                    {

                        //if ((raw[index - 1].dt.Hour == raw[index].dt.Hour) && (Math.Abs(raw[index - 1].dt.Minute - raw[index].dt.Minute) < 2))
                        //if ((Math.Abs(raw[index - 1].dt.Minute - raw[index].dt.Minute) < 2))
                        TimeSpan difference = ubiLoc[index].time.Subtract(ubiLoc[index - 1].time); // could also write `now - otherTime`
                        if (difference.TotalSeconds < 60)
                        {
                            //if (!subjFoundClose)
                            //    sw.WriteLine(line + "," + ubiLoc[index].time.ToLongTimeString() + "," + ubiLoc[index].time.Millisecond);
                            //subjFoundClose = true;

                            Tuple<double, double> targetpoint = linearInterpolate(target, ubiLoc[index - 1].time, ubiLoc[index - 1].x, ubiLoc[index - 1].y, ubiLoc[index].time, ubiLoc[index].x, ubiLoc[index].y);
                            double orientation1 = 0;// ubiLoc[index - 1].ori;
                            double orientation2 = 0;// ubiLoc[index].ori;
                            double targetorientation = linearInterpolate(target, ubiLoc[index - 1].time, orientation1, ubiLoc[index].time, orientation2);
                            PersonInfo pi2 = new PersonInfo();

                            //if (difference.TotalSeconds < 60)
                            {
                                //if (!subjFoundClose)
                                //    sw.WriteLine(line + "," + ubiLoc[index].time.ToLongTimeString() + "," + ubiLoc[index].time.Millisecond);

                                // subjFoundClose = true

                                if (lastIndex == index)
                                {
                                    pi2.interpolated = true;
                                }

                                pi2.x = targetpoint.Item1;
                                pi2.y = targetpoint.Item2;

                                pi2.time = target;

                                if (!dayActivities.ContainsKey(target))
                                    dayActivities.Add(target, new Dictionary<string, PersonInfo>());

                                if (!dayActivities[target].ContainsKey(personId))
                                    dayActivities[target].Add(personId, pi2);
                            }

                        }

                    }
                    lastIndex = index;
                    target = target.AddMilliseconds(100);
                }

            }
            return dayActivities;
        }

        public void processAndReportLenaAndAliceFiles()
        {
            foreach (String szMapId in dayMappings[currentDay].subjectMappings.Keys)
            {
                List<BasicSegment> lsegments = processSubjectLenaFile(szDate, szMapId);
                List<BasicSegment> asegments = processSubjectAliceFile(szDate, szMapId);
                List<TimeFrameObj> mergedSegments = mergeSegments(asegments, lsegments, szMapId);
                    
                if(!daysMergedSubjectSegments.ContainsKey(currentDay))
                {
                    daysMergedSubjectSegments.Add(currentDay, new Dictionary<string, List<TimeFrameObj>>()) ;
                }
                if (!daysMergedSubjectSegments[currentDay].ContainsKey(szMapId))
                {
                    daysMergedSubjectSegments[currentDay].Add(szMapId, mergedSegments);
                }
            }


        }


        public void setMappings(String szDates)
        {
            foreach (String szDate in szDates.Trim().Split(','))
            {
                DayMapping dm = new DayMapping();

                DaySubjectMapping daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14862";
                daySubjectMapping.type = "CHILD";
                daySubjectMapping.leftTag = "00:11:CE:00:00:00:01:DE";
                daySubjectMapping.rightTag = "00:11:CE:00:00:00:02:5F";
                dm.subjectMappings.Add("10D", daySubjectMapping);


                //00:11:CE:00:00:00:01:DE	10L	00:11:CE:00:00:00:02:5F
               // 00:11:CE: 00:00:00:02:14 1L  00:11:CE: 00:00:00:01:DA

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14866";
                daySubjectMapping.type = "CHILD";
                daySubjectMapping.leftTag = "00:11:CE: 00:00:00:02:14";
                daySubjectMapping.rightTag = "00:11:CE: 00:00:00:01:DA";
                dm.subjectMappings.Add("1D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14865";
                daySubjectMapping.type = "CHILD";
                daySubjectMapping.leftTag = "00:11:CE:00:00:00:03:74";
                daySubjectMapping.rightTag = "00:11:CE:00:00:00:01:E7";
                dm.subjectMappings.Add("2D", daySubjectMapping);

 

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14859";
                daySubjectMapping.type = "CHILD";
                daySubjectMapping.leftTag = "00:11:CE:00:00:00:02:B0";
                daySubjectMapping.rightTag = "00:11:CE:00:00:00:03:1C";
                dm.subjectMappings.Add("3D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14864";
                daySubjectMapping.type = "CHILD";
                daySubjectMapping.leftTag = "00:11:CE:00:00:00:03:48";
                daySubjectMapping.rightTag = "00:11:CE:00:00:00:01:E5";
                dm.subjectMappings.Add("4D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14870";
                daySubjectMapping.type = "CHILD";
                //00:11:CE:00:00:00:02:30	5L	00:11:CE:00:00:00:02:02
                daySubjectMapping.leftTag = "00:11:CE:00:00:00:02:30";
                daySubjectMapping.rightTag = "00:11:CE:00:00:00:02:02";
                dm.subjectMappings.Add("5D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14867";
                daySubjectMapping.type = "CHILD";
                daySubjectMapping.leftTag = "00:11:CE:00:00:00:02:4D";
                daySubjectMapping.rightTag = "00:11:CE:00:00:00:03:23";
                dm.subjectMappings.Add("6D", daySubjectMapping);
                /*2D	Child2	00:11:CE:00:00:00:03:74	2L	00:11:CE:00:00:00:01:E7	2R
3D	Child3	00:11:CE:00:00:00:02:B0	3L	00:11:CE:00:00:00:03:1C	3R
3D	Child4	00:11:CE:00:00:00:03:48	4L	00:11:CE:00:00:00:01:E5	4R
3D	Child3	00:11:CE:00:00:00:02:B0	3L	00:11:CE:00:00:00:03:1C	3R
4D	Child4	00:11:CE:00:00:00:03:48	4L	00:11:CE:00:00:00:01:E5	4R
4D	Child3	00:11:CE:00:00:00:02:B0	3L	00:11:CE:00:00:00:03:1C	3R
4D	Child4	00:11:CE:00:00:00:03:48	4L	00:11:CE:00:00:00:01:E5	4R
5D	Child5	00:11:CE:00:00:00:02:30	5L	00:11:CE:00:00:00:02:02	5R
6D	Child6	00:11:CE:00:00:00:02:4D	6L	00:11:CE:00:00:00:03:23	6R
7D	Child7	00:11:CE:00:00:00:02:11	7L	00:11:CE:00:00:00:01:C6	7R
8D	Child8	00:11:CE:00:00:00:02:1B	8L	00:11:CE:00:00:00:02:05	8R
9D	Child9	00:11:CE:00:00:00:02:4C	9L	00:11:CE:00:00:00:02:01	9R
9D	Child9	00:11:CE:00:00:00:02:4C	9L	00:11:CE:00:00:00:02:01	9R
*/
                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14868";
                daySubjectMapping.type = "CHILD";
                daySubjectMapping.leftTag = "00:11:CE:00:00:00:02:11";
                daySubjectMapping.rightTag = "00:11:CE:00:00:00:01:C6";
                dm.subjectMappings.Add("7D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14861";
                daySubjectMapping.type = "CHILD";
                daySubjectMapping.leftTag = "00:11:CE:00:00:00:02:1B";
                daySubjectMapping.rightTag = "00:11:CE:00:00:00:02:05";
                dm.subjectMappings.Add("8D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "8236";
                daySubjectMapping.type = "CHILD";
                daySubjectMapping.leftTag = "00:11:CE:00:00:00:02:4C";
                daySubjectMapping.rightTag = "00:11:CE:00:00:00:02:01";
                dm.subjectMappings.Add("9D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14863";
                daySubjectMapping.type = "TEACHER";
                daySubjectMapping.leftTag = "00:11:CE:00:00:00:02:CE";
                daySubjectMapping.rightTag = "00:11:CE:00:00:00:02:F2";
                dm.subjectMappings.Add("T1D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "7539";
                daySubjectMapping.type = "TEACHER";
                daySubjectMapping.leftTag = "00:11:CE:00:00:00:01:EA";
                daySubjectMapping.rightTag = "00:11:CE:00:00:00:01:C7";
                dm.subjectMappings.Add("T2D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "11564";
                daySubjectMapping.type = "TEACHER";
                daySubjectMapping.leftTag = "00:11:CE:00:00:00:02:7C";
                daySubjectMapping.rightTag = "00:11:CE:00:00:00:02:81";
                dm.subjectMappings.Add("T3D", daySubjectMapping);
                /*00:11:CE:00:00:00:02:CE	T1L	00:11:CE:00:00:00:02:F2	
00:11:CE:00:00:00:01:EA	T2L	00:11:CE:00:00:00:01:C7	
00:11:CE:00:00:00:02:7C	T3L	00:11:CE:00:00:00:02:81	*/

                dayMappings.Add(Convert.ToDateTime(szDate), dm);
            }
        }

        public List<BasicSegment> processSubjectLenaFile(String szDate, String szMapId)
        {
            List<BasicSegment> lsegments = new List<BasicSegment>();

            string[] szDayLenaItsFiles = Directory.GetFiles(dir + "//" + szDate + "//", "*.its");

            foreach (string itsFile in szDayLenaItsFiles)
            {
                String szLenaId = itsFile;
                szLenaId = szLenaId.Substring(szLenaId.LastIndexOf("_") + 2).Replace(".its", "");
                if (szLenaId.Substring(0, 1) == "0")
                    szLenaId = szLenaId.Substring(1);

                {
                    if (szLenaId == dayMappings[currentDay].subjectMappings[szMapId].lenaId)
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(itsFile);
                        XmlNodeList rec = doc.ChildNodes[0].SelectNodes("ProcessingUnit/Recording") != null ? doc.ChildNodes[0].SelectNodes("ProcessingUnit/Recording") : doc.ChildNodes[2].SelectNodes("ProcessingUnit/Recording");
                        int lenaSegmentNumber = 0;
                        int lenaUttNumber = 0;
                        int lenaCryNumber = 0;

                        foreach (XmlNode recording in rec)
                        {

                            double recStartSecs = Convert.ToDouble(recording.Attributes["startTime"].Value.Substring(2, recording.Attributes["startTime"].Value.Length - 3));
                            DateTime recStartTime = DateTime.Parse(recording.Attributes["startClockTime"].Value);
                            var est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                            recStartTime = TimeZoneInfo.ConvertTime(recStartTime, est);
                            XmlNodeList nodes = recording.SelectNodes("Conversation|Pause");


                            if (Utilities.isSameDay(recStartTime, currentDay) &&
                                recStartTime.Hour >= startHour &&
                                recStartTime.Hour <= endHour)
                            {
                                foreach (XmlNode conv in nodes)
                                {

                                    XmlNodeList segments = conv.SelectNodes("Segment");
                                    double startSecs = Convert.ToDouble(conv.Attributes["startTime"].Value.Substring(2, conv.Attributes["startTime"].Value.Length - 3)) - recStartSecs;
                                    double endSecs = Convert.ToDouble(conv.Attributes["endTime"].Value.Substring(2, conv.Attributes["endTime"].Value.Length - 3)) - recStartSecs;
                                    DateTime start = Utilities.geFullTime(recStartTime.AddSeconds(startSecs));
                                    DateTime end = Utilities.geFullTime(recStartTime.AddSeconds(endSecs));
                                    if (!startTimes.ContainsKey(szMapId))
                                        startTimes.Add(szMapId, recStartTime);
                                    else
                                        startTimes[szMapId]=recStartTime;
                                    //if (false)//
                                    if (Utilities.isSameDay(start, currentDay) &&
                                    start.Hour >= startHour &&
                                    (start.Hour < endHour || (start.Hour == endHour && start.Minute <= endMinute)))
                                    {
                                        if (conv.Name == "Conversation")
                                        {
                                            double tc = Convert.ToDouble(conv.Attributes["turnTaking"].Value); ;
                                            //TURNCOUNTS


                                            if (tc > 0)
                                            {
                                                DateTime time = start;
                                                int ms = time.Millisecond > 0 ? time.Millisecond / 100 * 100 : time.Millisecond;// + 100;
                                                time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, ms);
                                                double bdSecs = (end - start).Seconds;
                                                double bdMilliseconds = (end - start).Milliseconds > 0 ? ((end - start).Milliseconds / 1000.00) : 0.00;
                                                double bd = bdSecs + bdMilliseconds;


                                            }
                                        }
                                    }
                                    //if (false)
                                    {
                                        foreach (XmlNode seg in segments)
                                        {
                                            lenaSegmentNumber++;
                                            startSecs = Convert.ToDouble(seg.Attributes["startTime"].Value.Substring(2, seg.Attributes["startTime"].Value.Length - 3)) - recStartSecs;
                                            endSecs = Convert.ToDouble(seg.Attributes["endTime"].Value.Substring(2, seg.Attributes["endTime"].Value.Length - 3)) - recStartSecs;
                                            start = Utilities.geFullTime(recStartTime.AddMilliseconds(startSecs * 1000));
                                            end = Utilities.geFullTime(recStartTime.AddMilliseconds(endSecs * 1000));
                                            String speaker = seg.Attributes["spkr"].Value;

                                            if (speaker == "CHN" || speaker == "CHF")
                                            {
                                                foreach (XmlAttribute atts in seg.Attributes)
                                                {
                                                    if (atts.Name.IndexOf("startUtt") == 0)
                                                    {
                                                        lenaUttNumber++;
                                                        String attStep = atts.Name.Substring(8);
                                                        String att = atts.Name;
                                                        double astartSecs = Convert.ToDouble(seg.Attributes[att].Value.Substring(2, seg.Attributes[att].Value.Length - 3)) - recStartSecs;
                                                        double aendSecs = Convert.ToDouble(seg.Attributes["endUtt" + attStep].Value.Substring(2, seg.Attributes["endUtt" + attStep].Value.Length - 3)) - recStartSecs;
                                                        DateTime astart = Utilities.geFullTime(recStartTime.AddMilliseconds(astartSecs * 1000));
                                                        DateTime aend = Utilities.geFullTime(recStartTime.AddMilliseconds(aendSecs * 1000));

                                                        BasicSegment lenaS = new BasicSegment();
                                                        lenaS.timeInSecs = astartSecs;
                                                        lenaS.timeEndISecs = aendSecs;
                                                        lenaS.segmentNumber = lenaSegmentNumber;
                                                        lenaS.uttNumber = lenaUttNumber;
                                                        lsegments.Add(lenaS);
                                                        dayMappings[currentDay].subjectMappings[szMapId].lenaUtts += 1;

                                                    }
                                                    /*else if (atts.Name.IndexOf("startCry") == 0)
                                                    {
                                                        lenaUttNumber++;
                                                        String attStep = atts.Name.Substring(8);
                                                        String att = atts.Name;
                                                        double astartSecs = Convert.ToDouble(seg.Attributes[att].Value.Substring(2, seg.Attributes[att].Value.Length - 3)) - recStartSecs;
                                                        double aendSecs = Convert.ToDouble(seg.Attributes["endCry" + attStep].Value.Substring(2, seg.Attributes["endCry" + attStep].Value.Length - 3)) - recStartSecs;
                                                        DateTime astart = Utilities.geFullTime(recStartTime.AddMilliseconds(astartSecs * 1000));
                                                        DateTime aend = Utilities.geFullTime(recStartTime.AddMilliseconds(aendSecs * 1000));

                                                        BasicSegment lenaS = new BasicSegment();
                                                        lenaS.timeInSecs = astartSecs;
                                                        lenaS.timeEndISecs = aendSecs;
                                                        lenaS.segmentNumber = lenaSegmentNumber;
                                                        lenaS.cryNumber = lenaCryNumber;
                                                        lsegments.Add(lenaS);
                                                        dayMappings[currentDay].subjectMappings[szMapId].lenaCries += 1;

                                                    }*/
            }

        }

                                        }
                                    }

                                }

                            }
                        }
                    }
                }


            }

            return lsegments;
        }
        public List<BasicSegment> processSubjectAliceFile(String szDate, String szMapId)
        {
            List<BasicSegment> asegments = new List<BasicSegment>();
            if (File.Exists(aliceFile))
            {
                int uttNumber = 0;
                using (StreamReader sr = new StreamReader(aliceFile))
                {
                        while (!sr.EndOfStream)
                    {
                        String commaLine = sr.ReadLine();
                        String[] line = commaLine.Split(' ');

                        if (line.Length > 3 && line[1] != "")
                        {
                            String szLenaId = line[1].Trim();
                            szLenaId = szLenaId.Substring(szLenaId.LastIndexOf("_") + 2);
                            if (szLenaId.Substring(0, 1) == "0")
                                szLenaId = szLenaId.Substring(1);
                            if (szLenaId == dayMappings[currentDay].subjectMappings[szMapId].lenaId)
                            {

                                //DateTime time = new DateTime();

                                if (szMapId != "" && startTimes.ContainsKey(szMapId))
                                {
                                    double aliceDurSecs = Convert.ToDouble(line[4]);
                                    double aliceOnsetSecsStart = Convert.ToDouble(line[3]);
                                    double aliceOnsetSecsEnd = aliceOnsetSecsStart + aliceDurSecs;

                                    //time = new DateTime(startTimes[szLenaId].Year, startTimes[szLenaId].Month, startTimes[szLenaId].Day, startTimes[szLenaId].Hour, startTimes[szLenaId].Minute, startTimes[szLenaId].Second);
                                    //time = time.AddSeconds(Convert.ToDouble(line[3]));

                                    String szType = line[7].Trim();

                                    if (szType == "KCHI")
                                    {
                                        uttNumber++;
                                        BasicSegment aliceS = new BasicSegment();
                                        aliceS.timeInSecs = aliceOnsetSecsStart;
                                        aliceS.timeEndISecs = aliceOnsetSecsEnd;
                                        aliceS.uttNumber = uttNumber;
                                        asegments.Add(aliceS);
                                        dayMappings[currentDay].subjectMappings[szMapId].aliceUtts += 1;
                                    }

                                }
                            }

                        }
                    }
                }
            }
            return asegments;
        }


        public List<TimeFrameObj> mergeSegments(List<BasicSegment> a, List<BasicSegment> l, String szId)
        {
            Dictionary<double, TimeFrameObj> timeStamps = new Dictionary<double, TimeFrameObj>();
            foreach (BasicSegment b in l)
            {
                TimeFrameObj timeStampS = new TimeFrameObj();
                timeStampS.id = szId;
                timeStampS.timeInSecs = b.timeInSecs;
                timeStampS.isStartLena = true;
                timeStampS.lenaSegment = b;
                if (!timeStamps.ContainsKey(timeStampS.timeInSecs))
                {
                    timeStamps.Add(b.timeInSecs, timeStampS);
                }
                else
                {
                    timeStamps[timeStampS.timeInSecs].isStartLena = true;
                    timeStamps[timeStampS.timeInSecs].lenaSegment = b;
                }

                TimeFrameObj timeStampE = new TimeFrameObj();
                timeStampE.timeInSecs = b.timeEndISecs;
                timeStampE.isEndLena = true; ;
                timeStampE.id = szId;
                if (!timeStamps.ContainsKey(timeStampE.timeInSecs))
                {
                    timeStamps.Add(b.timeEndISecs, timeStampE);
                }
                else
                {
                    timeStamps[timeStampS.timeInSecs].timeInSecs = b.timeEndISecs;
                    timeStamps[timeStampS.timeInSecs].isEndLena = true; ; 
                     
                }


                 
            }

            foreach (BasicSegment b in a)
            {
                if (timeStamps.ContainsKey(b.timeInSecs))
                {
                    timeStamps[b.timeInSecs].isStartAlice = true;
                    timeStamps[b.timeInSecs].aliceSegment = b;
                }
                else
                {
                    TimeFrameObj timeStampS = new TimeFrameObj();
                    timeStampS.id = szId;
                    timeStampS.timeInSecs = b.timeInSecs;
                    timeStampS.isStartAlice = true;
                    timeStampS.aliceSegment = b;
                    timeStamps.Add(b.timeInSecs, timeStampS);

                }

                if (timeStamps.ContainsKey(b.timeEndISecs))
                {
                    timeStamps[b.timeInSecs].isEndAlice = true;
                }
                else
                {
                    TimeFrameObj timeStampE = new TimeFrameObj();
                    timeStampE.id = szId;
                    timeStampE.timeInSecs = b.timeEndISecs;
                    timeStampE.isEndAlice = true;
                    timeStamps.Add(b.timeEndISecs, timeStampE);

                }


            }

            // List<TimeFrameObj> timeStampsList = timeStamps.OrderBy(o => o.timeInSecs).ToList(); // timeStamps.Select(item => item.Value).ToList().OrderBy(o => o.timeInSecs).ToList();
            //List<TimeFrameObj> TimeFrameObjs = new List<TimeFrameObj>();

            /*bool lenaStarted = false;
            bool lenaFinished = false;
            bool aliceStarted = false;
            bool aliceFinished = false;*/
            //

            List<TimeFrameObj> finalList = new List<TimeFrameObj>();

            List<TimeFrameObj> timeStampsList = timeStamps.Values.ToList();
            timeStampsList = timeStampsList.OrderBy(o => o.timeInSecs).ToList();

            bool lenaStartedTrack = false;
            bool lenaFinishedTrack = false;
            bool aliceStartedTrack = false;
            bool aliceFinishedTrack = false;

            BasicSegment currentAliceSegment=null;
            BasicSegment currentLenaSegment=null;
            
            for (int i = 1; i <= timeStampsList.Count - 1; i++)
            {
                TimeFrameObj startToEnd = new TimeFrameObj();
                TimeFrameObj startTime = timeStampsList[i - 1];
                TimeFrameObj endTime = timeStampsList[i];
                if(i== timeStampsList.Count )//startTime.timeInSecs == 82.011)
                {
                    int qai = 0;
                }

                startToEnd.timeInSecs = startTime.timeInSecs;
                startToEnd.timeEndISecs = endTime.timeInSecs;

                startToEnd.isStartLena = startTime.isStartLena;
                startToEnd.isEndLena = endTime.isEndLena;
                startToEnd.isMiddleLena = lenaStartedTrack && (!endTime.isEndLena);
                startToEnd.isStartAlice = startTime.isStartAlice;
                startToEnd.isEndAlice = endTime.isEndAlice;
                startToEnd.isMiddleAlice = aliceStartedTrack && (!endTime.isEndAlice);

               // startToEnd.lenaSegment = startToEnd.isMiddleLena ? currentLenaSegment : startTime.lenaSegment;
                //startToEnd.aliceSegment = startToEnd.isMiddleAlice? currentAliceSegment: startTime.aliceSegment;


                startToEnd.lenaSegment = startToEnd.isStartLena ? startTime.lenaSegment:
                    lenaStartedTrack && (!lenaFinishedTrack) ? currentLenaSegment : null;
                //|| (aliceStartedTrack && (!aliceFinishedTrack)) ? currentAliceSegment : startTime.aliceSegment;
                startToEnd.aliceSegment = startToEnd.isStartAlice ? startTime.aliceSegment :
                aliceStartedTrack && (!aliceFinishedTrack) ? currentAliceSegment : null;



                if(startToEnd.isEndLena)
                {
                    currentLenaSegment = null;
                }
                else if (startToEnd.isStartLena)
                {
                    currentLenaSegment = startTime.lenaSegment;
                }

                if (startToEnd.isEndAlice)
                {
                    currentAliceSegment = null;
                }
                else if (startToEnd.isStartAlice)
                {
                    currentAliceSegment = startTime.aliceSegment;
                }
 


                
                lenaFinishedTrack = endTime.isEndLena;
                lenaStartedTrack = (!lenaFinishedTrack) ? startToEnd.isStartLena || startToEnd.isMiddleLena : false;

                aliceFinishedTrack = endTime.isEndAlice;
                aliceStartedTrack = (!aliceFinishedTrack) ? startToEnd.isStartAlice || startToEnd.isMiddleAlice : false;


                startToEnd.id = szId;
                if(startToEnd.isMiddleLena || startToEnd.isMiddleAlice)
                {
                    bool qa = true;
                }
                finalList.Add(startToEnd);
            }

            if (finalList.Count > 0 && finalList[0].timeInSecs > 0)
            {
                TimeFrameObj startToEnd = new TimeFrameObj();
                startToEnd.timeInSecs = 0;
                startToEnd.timeEndISecs = finalList[0].timeInSecs;
                finalList.Insert(0,startToEnd);
            }
            return finalList;
            
        }

    }


    internal class DayMapping
    {
        public Dictionary<String, DaySubjectMapping> subjectMappings = new Dictionary<String, DaySubjectMapping>();
        public void setDayMappins1718(String fileName, DateTime day)
        {
            Dictionary<String, int> columnIndexes = new Dictionary<String, int>();
            columnIndexes.Add("ID", -1);
            columnIndexes.Add("LENA", -1);
            columnIndexes.Add("START", -1);
            columnIndexes.Add("END", -1);
            columnIndexes.Add("ABSENT", -1);
            columnIndexes.Add("TYPE", -1);

            using (StreamReader sr = new StreamReader(fileName))
            {
                if (!sr.EndOfStream)
                {
                    String commaLine = sr.ReadLine();
                    String[] line = commaLine.Split(',');
                    int c = 1;
                    foreach (String s in line)
                    {
                        if (s.Trim().ToUpper() == "SUBJECT_ID")
                        {
                            columnIndexes["ID"] = c;
                        }
                        else if (s.Trim().ToUpper() == "LENA")
                        {
                            columnIndexes["LENA"] = c;
                        }
                        else if (s.Trim().ToUpper() == "LENA")
                        {
                            columnIndexes["LENA"] = c;
                        }
                        else if (s.Trim().ToUpper() == "TYPE")
                        {
                            columnIndexes["TYPE"] = c;
                        }
                        else if (s.Trim().ToUpper() == "STARTS")
                        {
                            columnIndexes["START"] = c;
                        }
                        else if (s.Trim().ToUpper() == "EXPIRES")
                        {
                            columnIndexes["END"] = c;
                        }
                        else if (s.Trim().ToUpper() == "ABSENT")
                        {
                            columnIndexes["ABSENT"] = c;
                        }
                        c++;
                    }

                    while (!sr.EndOfStream)
                    {
                        commaLine = sr.ReadLine();
                        line = commaLine.Split(',');
                        if (line[2] != "")
                        {
                            DaySubjectMapping daySubjectMapping = new DaySubjectMapping();
                            daySubjectMapping.id = line[columnIndexes["ID"]];
                            daySubjectMapping.lenaId = line[columnIndexes["LENA"]];
                            daySubjectMapping.type = line[columnIndexes["TYPE"]];

                            Boolean absent = false;
                            String[] absences = line[columnIndexes["ABSENT"]].Split('|');
                            String szDay = (day.Month < 10 ? "0" + day.Month.ToString() : day.Month.ToString()) + "/" +
                                (day.Day < 10 ? "0" + day.Day.ToString() : day.Day.ToString()) + "/" +
                                 day.Year.ToString();

                            foreach (String s in absences)
                            {
                                if (s != "")
                                    if (s.Trim() == szDay)
                                    {
                                        absent = true;
                                        break;
                                    }
                            }
                            if (!absent)
                            {
                                DateTime s = Convert.ToDateTime(line[columnIndexes["START"]]);
                                DateTime e = Convert.ToDateTime(line[columnIndexes["END"]]);
                                if (day >= s && day < e)
                                {
                                    this.subjectMappings.Add(daySubjectMapping.id, daySubjectMapping);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    internal class DaySubjectMapping
    {
        public String id = "";
        public String lenaId = "";
        public String type = "";
        public String leftTag = "";
        public String rightTag = "";
        public DateTime startTime = new DateTime();
        public DateTime endTime = new DateTime();

        public int lenaUtts = 0;
        public int lenaCries = 0;
        public int aliceUtts = 0;
    }
    internal class TimeFrameObj
    {
        public String id = "";
        public String szType = "";
        public double timeInSecs = 0;
        public double timeEndISecs = 0;
        public bool isStartLena = false;
        public bool isEndLena = false;
        public bool isMiddleLena = false;

        public bool isStartAlice = false;
        public bool isEndAlice = false;
        public bool isMiddleAlice = false;

        public BasicSegment lenaSegment = null;
        public BasicSegment aliceSegment = null;

        public String ubiSocialContacts = "";
    }

    internal class BasicSegment
    {
        public double timeInSecs = 0;
        public double timeEndISecs = 0;
        public int segmentNumber = 0;
        public int uttNumber = 0;
        public int cryNumber = 0;
    }


















    internal class aliceAndLenaVars
    {
        public double lenaTCConvCount = 0;
        public double lenaTCDur = 0; 

        
        public double lenaKChiSegDur = 0;
        public double lenaKChiSegUttDur = 0;
        public double lenaKChiSegUttCount = 0;

        public double lenaKChiUttUttDur = 0;
        public double lenaKChiUttUttCount = 0;

        public double lenaKChiSegDurN = 0;
        public double lenaKChiSegUttDurN = 0;
        public double lenaKChiSegUttCountN = 0;

        public double lenaKChiUttUttDurN = 0;
        public double lenaKChiUttUttCountN = 0;

        public double lenaKChiSegDurF = 0;
        public double lenaKChiSegUttDurF = 0;
        public double lenaKChiSegUttCountF = 0;

        public double lenaKChiUttUttDurF = 0;
        public double lenaKChiUttUttCountF = 0;

        public double lenaChiSegDur = 0;
        public double lenaChiSegUttCount = 0;

        public double lenaChiUttUttDur = 0;
        public double lenaChiUttUttCount = 0;

        public double lenaChiSegDurN = 0;
        public double lenaChiSegUttCountN = 0;

        public double lenaChiUttUttDurN = 0;
        public double lenaChiUttUttCountN = 0;

        public double lenaChiSegDurF = 0;
        public double lenaChiSegUttCountF = 0;

        public double lenaChiUttUttDurF = 0;
        public double lenaChiUttUttCountF = 0;


        public double lenaACSegDur = 0;
        public double lenaACSegUttDur = 0;
        public double lenaACSegWordCount = 0; 
         
        public double aliceChiDur = 0;
        public double aliceKChiDur = 0;
        public double aliceACDur = 0;
        public double aliceTCDur = 0;

         
        

        public double aliceChiCount = 0;
        public double aliceKChiCount = 0;
        public double aliceACCount = 0;
        public double aliceTCCount = 0;
        public double aliceSpeechCount = 0;
        public double aliceSpeechDur = 0;

    }

    public class aliceLenaNodes
    {
        public double dur = 0;
        public double count = 0;
        public double fromSecs= 0;
        public double toSecs= 0;
        public String type = "";

    }
     
    internal class AliceLenaSegmets
    {
        public string id = "";
        public string lenaId = "";
        public string types = "";

        public double startOnset = 0.0;
        public double endOnset = 0.0;

        // ALICE
        public bool hasAlice = false;
        public double countA = 0;
        public double startOnsetA = 0.0;
        public double endOnsetA = 0.0;

        // LENA
        public bool hasLena = false;
        public double countL = 0;
        public double startOnsetL = 0.0;
        public double endOnsetL = 0.0;

        public bool matchFound = false;
        public bool notCovered = false;

        // References to original source segments (for safe merging)
        public AliceLenaSegmets originalLenaSegment = null;
        public AliceLenaSegmets originalAliceSegment = null;
    }
    class TimelineEvent
    {
        public double time;
        public string id;
        public bool isStart;
        public string source; // "LENA" or "ALICE"
    }
    public class Interval
    {
        public double start;
        public double end;

        public Interval(double start, double end)
        {
            this.start = start;
            this.end = end;
        }
    }

    class SegmentMerger
    {
        const double epsilon = 0.0001;

        static bool AlmostEqual(double a, double b) => Math.Abs(a - b) < epsilon;

        public static List<AliceLenaSegmets> MergeSegments(
            List<AliceLenaSegmets> lsegments,
            List<AliceLenaSegmets> alsegments)
        {
            // 1. Collect all unique boundary times from both lists, with tolerance
            var allBoundaries = lsegments.SelectMany(s => new[] { s.startOnset, s.endOnset })
                .Concat(alsegments.SelectMany(s => new[] { s.startOnset, s.endOnset }))
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            // Deduplicate boundaries with epsilon tolerance
            var timeline = new List<double>();
            foreach (var t in allBoundaries)
            {
                if (!timeline.Any(x => AlmostEqual(x, t)))
                {
                    timeline.Add(t);
                }
            }

            // 2. Build intervals between timeline points using the Interval class
            var intervals = new List<Interval>();
            for (int i = 0; i < timeline.Count - 1; i++)
            {
                intervals.Add(new Interval(timeline[i], timeline[i + 1]));
            }

            // 3. For each interval, find active LENA and ALICE segments (must match id)
            var finalSegments = new List<AliceLenaSegmets>();

            foreach (var interval in intervals)
            {
                double start = interval.start;
                double end = interval.end;

                // Find LENA segments active during [start, end)
                var activeLena = lsegments
                    .Where(s => s.startOnset <= start && s.endOnset >= end)
                    .ToList();

                // Find ALICE segments active during [start, end)
                var activeAlice = alsegments
                    .Where(s => s.startOnset <= start && s.endOnset >= end)
                    .ToList();

                // Pick first active segments (or null)
                var matchedLena = activeLena.FirstOrDefault();
                var matchedAlice = activeAlice.FirstOrDefault();

                // Check id equality if both present
                bool idsMatch = (matchedLena != null && matchedAlice != null) ?
                    (matchedLena.id == matchedAlice.id) : false;

                var seg = new AliceLenaSegmets
                {
                    startOnset = start,
                    endOnset = end,
                    hasLena = matchedLena != null,
                    hasAlice = matchedAlice != null,
                    matchFound = idsMatch,
                    notCovered = !(matchedLena != null || matchedAlice != null),
                    // Copy original LENA times if available
                    startOnsetL = matchedLena?.startOnsetL ?? 0,
                    endOnsetL = matchedLena?.endOnsetL ?? 0,
                    // Copy original ALICE times if available
                    startOnsetA = matchedAlice?.startOnsetA ?? 0,
                    endOnsetA = matchedAlice?.endOnsetA ?? 0,
                    // Reference original segments
                    originalLenaSegment = matchedLena,
                    originalAliceSegment = matchedAlice,
                    id = matchedLena?.id ?? matchedAlice?.id ?? "",
                    lenaId = matchedLena?.lenaId ?? "",
                    types = matchedLena?.types ?? matchedAlice?.types ?? ""
                };

                finalSegments.Add(seg);
            }

            // 4. Merge consecutive intervals with same flags and same original source segments
            var mergedSegments = new List<AliceLenaSegmets>();

            foreach (var seg in finalSegments)
            {
                if (mergedSegments.Count == 0)
                {
                    mergedSegments.Add(seg);
                    continue;
                }

                var last = mergedSegments.Last();

                bool consecutive = AlmostEqual(last.endOnset, seg.startOnset);
                bool sameState =
                    last.hasAlice == seg.hasAlice &&
                    last.hasLena == seg.hasLena &&
                    last.matchFound == seg.matchFound &&
                    last.notCovered == seg.notCovered;

                bool sameLena = ReferenceEquals(last.originalLenaSegment, seg.originalLenaSegment);
                bool sameAlice = ReferenceEquals(last.originalAliceSegment, seg.originalAliceSegment);

                if (consecutive && sameState && sameLena && sameAlice)
                {
                    // Merge by extending end time
                    last.endOnset = seg.endOnset;
                }
                else
                {
                    mergedSegments.Add(seg);
                }
            }

            return mergedSegments;
        }
    }
    internal class CompareAlice
    {
        public String pairActivityFile = "";
        public String aliceFile = "";
        public String dir = "C:\\IBSS\\LB1718\\";
        public String mappingFile = "C:\\IBSS\\LB1718\\";

        //public Dictionary<String, String> subjectLenas = new Dictionary<String, String>();
        //public Dictionary<String, String> subjectTypes = new Dictionary<String, String>();
        public Dictionary<String, DateTime> startTimes = new Dictionary<string, DateTime>();
        public int startHour = 7;
        public int endHour = 14;
        public int endMinute = 0;
        public Dictionary<DateTime, Dictionary<String, aliceAndLenaVars>> tenths = new Dictionary<DateTime, Dictionary<string, aliceAndLenaVars>>();


        public Dictionary<DateTime, DayMapping> dayMappings = new Dictionary<DateTime, DayMapping>();
        public DateTime currentDay;
        DayMapping dm = new DayMapping();
        //public Dictionary<DateTime day, String str> mappings = new Dictionary<DateTime, day, string, str>();
        public CompareAlice(String paFile, String aFile, Boolean oldMappingFormat, String mapFile)
        {
            List<AliceLenaSegmets> aliceLenaSegmets = new List<AliceLenaSegmets>();
            
            mappingFile = mapFile;
            String szDates = "02-16-2018";
            pairActivityFile = paFile;
            aliceFile = aFile;

            foreach (String szDate in szDates.Trim().Split(','))
            {
                currentDay = Convert.ToDateTime(szDate);
                dm = new DayMapping();
                //dm.setDayMappins1718(mappingFile, currentDay);
                dm.subjectMappings = new Dictionary<string, DaySubjectMapping>();
                DaySubjectMapping daySubjectMapping = new DaySubjectMapping(); 
                daySubjectMapping.lenaId = "14862";
                daySubjectMapping.type = "CHILD";
                dm.subjectMappings.Add("10D", daySubjectMapping);


                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14866";
                daySubjectMapping.type = "CHILD";
                dm.subjectMappings.Add("1D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14865";
                daySubjectMapping.type = "CHILD";
                dm.subjectMappings.Add("2D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14859";
                daySubjectMapping.type = "CHILD";
                dm.subjectMappings.Add("3D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14864";
                daySubjectMapping.type = "CHILD";
                dm.subjectMappings.Add("4D", daySubjectMapping);
               
                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14870";
                daySubjectMapping.type = "CHILD";
                dm.subjectMappings.Add("5D", daySubjectMapping);
                
                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14867";
                daySubjectMapping.type = "CHILD";
                dm.subjectMappings.Add("6D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14868";
                daySubjectMapping.type = "CHILD";
                dm.subjectMappings.Add("7D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14861";
                daySubjectMapping.type = "CHILD";
                dm.subjectMappings.Add("8D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "8236";
                daySubjectMapping.type = "CHILD";
                dm.subjectMappings.Add("9D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "14863";
                daySubjectMapping.type = "TEACHER";
                dm.subjectMappings.Add("T1D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "7539";
                daySubjectMapping.type = "TEACHER";
                dm.subjectMappings.Add("T2D", daySubjectMapping);

                daySubjectMapping = new DaySubjectMapping();
                daySubjectMapping.lenaId = "11564";
                daySubjectMapping.type = "TEACHER";
                dm.subjectMappings.Add("T3D", daySubjectMapping);
                

                /* 
                subjectLenas.Add("1D", daySubjectMapping);
                subjectLenas.Add("2D", "14865");
                subjectLenas.Add("3D", "14859");
                subjectLenas.Add("4D", "14864");
                subjectLenas.Add("5D", "14870");
                subjectLenas.Add("6D", "14867");
                subjectLenas.Add("7D", "14868");
                subjectLenas.Add("8D", "14861");
                subjectLenas.Add("9D", "8236");
                subjectLenas.Add("Lab1D", "11563");
                subjectLenas.Add("Lab2D", "13841");
                subjectLenas.Add("Lab3D", "11566");
                subjectLenas.Add("Lab4D", "11564");
                subjectLenas.Add("Lab5D", "11563");
                subjectLenas.Add("Lab6D", "24624");
                subjectLenas.Add("T1D", "14863");
                subjectLenas.Add("T2D", "7539");
                subjectLenas.Add("T3D", "11564");


                subjectTypes.Add("10D", "CHILD");
                subjectTypes.Add("1D", "CHILD");
                subjectTypes.Add("2D", "CHILD");
                subjectTypes.Add("3D", "CHILD");
                subjectTypes.Add("4D", "CHILD");
                subjectTypes.Add("5D", "CHILD");
                subjectTypes.Add("6D", "CHILD");
                subjectTypes.Add("7D", "CHILD");
                subjectTypes.Add("8D", "CHILD");
                subjectTypes.Add("9D", "CHILD");
                subjectTypes.Add("Lab1D", "LAB");
                subjectTypes.Add("Lab2D", "LAB");
                subjectTypes.Add("Lab3D", "LAB");
                subjectTypes.Add("Lab4D", "LAB");
                subjectTypes.Add("Lab5D", "LAB");
                subjectTypes.Add("Lab6D", "LAB");
                subjectTypes.Add("T1D", "TEACHER");
                subjectTypes.Add("T2D", "TEACHER");
                subjectTypes.Add("T3D", "TEACHER");*/

                dayMappings.Add(currentDay, dm);


                List<AliceLenaSegmets> ls = setLenaSegments(szDate, currentDay);
                List<AliceLenaSegmets> als = setAliceSegments(ls);

                TextWriter sw = new StreamWriter(aliceFile.Replace(".", "_ALICECOMPARE_" + new Random().Next()) + ".csv");

                sw.WriteLine("SUBJECT,START_ONSET_SEC,END_ONSET_SEC,ALICE_START_ONSET_SEC,ALICE_END_ONSET_SEC,LENA_START_ONSET_SEC,LENA_END_ONSET_SEC,TYPE,HASALICE,HASLENA,matchFound,NOTCOVERED");


                foreach (AliceLenaSegmets s in als)
                {
                    sw.WriteLine(s.id + "," + s.startOnset + "," + s.endOnset + "," +
                        s.startOnsetA + "," + s.endOnsetA + "," +
                        s.startOnsetL + "," + s.endOnsetL + "," +
                        s.types + "," + s.hasAlice + "," +
                        s.hasLena + "," + s.matchFound + "," + s.notCovered
                        ) ;
                }

                TextWriter sw2 = new StreamWriter(aliceFile.Replace(".", "_ALICECOMPARE_TOTALS" + new Random().Next()) + ".csv");

                sw2.WriteLine("SUBJECT,LENAUTTS,ALICEUTTS");


                foreach (String s in dm.subjectMappings.Keys)
                {
                    sw2.WriteLine(s + "," + dm.subjectMappings[s].lenaUtts + "," + dm.subjectMappings[s].aliceUtts
                        );
                }
                sw2.Close();
                /*
                TextWriter sw = new StreamWriter(aliceFile.Replace(".", "_ALICECOMPARE_" + new Random().Next()) + ".csv");
                sw.WriteLine("SUBJECT,TYPE,DATE,DATETIME,MS," +
                    "
                
                _DUR_SECS,ALICE_KCHI_COUNT," +
                    "ALICE_CHI_DUR_SECS,ALICE_CHI_COUNT," +
                    "ALICE_AC_DUR_SECS,ALICE_AC_COUNT," +
                    "ALICE_SPEECH_DUR_SECS,ALICE_SPEECH_COUNT," +
                    "LENA_SEGMENT_TC_DUR_SECS,LENA_SEGMENT_TC_COUNT," +
                    "LENA_SEGMENT_KCHI_DUR_SECS,LENA_SEGMENT_KCHI_UTT_DUR_SECS,LENA_SEGMENT_KCHI_UTTCOUNT," +
                    "LENA_KCHI_UTT_UTTDUR_SECS,LENA_KCHI_UTT_UTTCOUNT," +
                    "LENA_SEGMENT_KCHI_DUR_SECS_NEAR,LENA_SEGMENT_KCHI_UTT_DUR_SECS_NEAR,LENA_SEGMENT_KCHI_UTTCOUNT_NEAR," +
                    "LENA_KCHI_UTT_UTTDUR_SECS_NEAR,LENA_KCHI_UTT_UTTCOUNT_NEAR," +
                    "LENA_SEGMENT_KCHI_DUR_SECS_FAR,LENA_SEGMENT_KCHI_UTT_DUR_SECS_FAR,LENA_SEGMENT_KCHI_UTTCOUNT_FAR," +
                    "LENA_KCHI_UTT_UTTDUR_SECS_FAR,LENA_KCHI_UTT_UTTCOUNT_FAR," +
                    "LENA_SEGMENT_CHI_DUR_SECS,LENA_SEGMENT_CHI_UTTCOUNT," +
                    "LENA_CHI_UTT_UTTDUR_SECS,LENA_CHI_UTT_UTTCOUNT," +

                    "LENA_SEGMENT_CHI_DUR_SECS_NEAR,LENA_SEGMENT_CHI_UTTCOUNT_NEAR," +
                    "LENA_CHI_UTT_UTTDUR_SECS_NEAR,LENA_CHI_UTT_UTTCOUNT_NEAR," +

                    "LENA_SEGMENT_CHI_DUR_SECS_FAR,LENA_SEGMENT_CHI_UTTCOUNT_FAR," +
                    "LENA_CHI_UTT_UTTDUR_SECS_FAR,LENA_CHI_UTT_UTTCOUNT_FAR," +

                    "LENA_SEGMENT_AC_DUR_SECS,LENA_SEGMENT_AC_UTT_DUR_SECS,LENA_SEGMENT_AC_WORD_COUNT," +
                    "");



                using (StreamReader sr = new StreamReader(pairActivityFile))
                {
                    foreach (DateTime szDay in tenths.Keys)
                    {
                        foreach (String subject in tenths[szDay].Keys)
                        {
                            sw.Write(subject + "," + dayMappings[currentDay].subjectMappings[subject].type + "," + szDay + "," +
                                szDay.Month + "/" + szDay.Day + "/" + szDay.Year + " " + szDay.Hour + ":" + szDay.Minute + ":" + szDay.Second + "." + szDay.Millisecond + "," + szDay.Millisecond + ",");
                            aliceAndLenaVars al = tenths[szDay][subject];
                            String szVars = al.aliceKChiDur + "," +
            al.aliceKChiCount + "," +
            al.aliceChiDur + "," +
            al.aliceChiCount + "," +
            al.aliceACDur + "," +
            al.aliceACCount + "," +

            al.aliceSpeechDur + "," +
            al.aliceSpeechCount + "," +

            al.lenaTCDur + "," +
            al.lenaTCConvCount + "," +

            al.lenaKChiSegDur + "," +
            al.lenaKChiSegUttDur + "," +
            al.lenaKChiSegUttCount + "," +

            al.lenaKChiUttUttDur + "," +
            al.lenaKChiUttUttCount + "," +

            al.lenaKChiSegDurN + "," +
            al.lenaKChiSegUttDurN + "," +
            al.lenaKChiSegUttCountN + "," +

            al.lenaKChiUttUttDurN + "," +
            al.lenaKChiUttUttCountN + "," +

            al.lenaKChiSegDurF + "," +
            al.lenaKChiSegUttDurF + "," +
            al.lenaKChiSegUttCountF + "," +

            al.lenaKChiUttUttDurF + "," +
            al.lenaKChiUttUttCountF + "," +

            al.lenaChiSegDur + "," +
            al.lenaChiSegUttCount + "," +

            al.lenaChiUttUttDur + "," +
            al.lenaChiUttUttCount + "," +

            al.lenaChiSegDurN + "," +
            al.lenaChiSegUttCountN + "," +

            al.lenaChiUttUttDurN + "," +
            al.lenaChiUttUttCountN + "," +

            al.lenaChiSegDurF + "," +
            al.lenaChiSegUttCountF + "," +

            al.lenaChiUttUttDurF + "," +
            al.lenaChiUttUttCountF + "," +

            al.lenaACSegDur + "," +
            al.lenaACSegUttDur + "," +
            al.lenaACSegWordCount + ",";

                            sw.WriteLine(szVars);
                        }
                    }
                }
                 */
                sw.Close();
            }


        }

        public AliceLenaSegmets copyFrom(AliceLenaSegmets seg )
        {
            //ALICE
            AliceLenaSegmets seg2 = new AliceLenaSegmets();
            seg2.hasAlice = seg.hasAlice;
            seg2.countA = seg.countA;
            seg2.startOnsetA = seg.startOnsetA;
            seg2.endOnsetA = seg.endOnsetA;

            String id = "";
            seg2.lenaId = "";
            seg2.types = "";
             
            seg2.startOnset = seg.startOnset;
            seg2.endOnset = seg.endOnset;

            //LENA
            seg2.hasLena = seg.hasLena;
            seg2.countL = seg.countL;
            seg2.startOnsetL = seg.startOnsetL;
            seg2.endOnsetL = seg.endOnsetL;

            return seg2;

        }
        public AliceLenaSegmets copyAliceFrom(ref AliceLenaSegmets seg)
        {
            //ALICE

            AliceLenaSegmets seg2 = new AliceLenaSegmets();
            seg2.id = seg.id; 
            seg2.types += seg.types;
            seg2.hasAlice = seg.hasAlice;
            seg2.countA = seg.countA;
            seg2.startOnsetA = seg.startOnsetA;
            seg2.endOnsetA = seg.endOnsetA;
            return seg2;
        }
        public AliceLenaSegmets copyLenaFrom(AliceLenaSegmets seg)
        {
          
            AliceLenaSegmets seg2 = new AliceLenaSegmets();
            seg2.id = seg.id; 
            seg2.types+= seg.types;
            //LENA
            seg2.hasLena = seg.hasLena;
            seg2.countL = seg.countL;
            seg2.startOnsetL = seg.startOnsetL;
            seg2.endOnsetL = seg.endOnsetL;
            return seg2;
        }
        public void copyLenaFrom(AliceLenaSegmets seg, ref AliceLenaSegmets seg2)
        {
            seg2.id = seg.id; ;
            seg2.types += seg.types;        //LENA
            seg2.hasLena = seg.hasLena;
            seg2.countL = seg.countL;
            seg2.startOnsetL = seg.startOnsetL;
            seg2.endOnsetL = seg.endOnsetL;
         
        }
        public void copyAliceFrom(AliceLenaSegmets seg, ref AliceLenaSegmets seg2)
        {
            //ALICE 
            seg2.id = seg.id; ;
            seg2.types += seg.types;
            seg2.hasAlice = seg.hasAlice;
            seg2.countA = seg.countA;
            seg2.startOnsetA = seg.startOnsetA;
            seg2.endOnsetA = seg.endOnsetA; 
        }
        public List<AliceLenaSegmets> setLenaSegments(String szDate, DateTime day)
        {
            List<AliceLenaSegmets> lsegments = new List<AliceLenaSegmets>();
            string[] szDayLenaItsFiles = Directory.GetFiles(dir + "//" + szDate + "//", "*.its");

            foreach (string itsFile in szDayLenaItsFiles)
            {
                String szLenaId = itsFile;
                szLenaId = szLenaId.Substring(szLenaId.LastIndexOf("_") + 2).Replace(".its", "");
                if (szLenaId.Substring(0, 1) == "0")
                    szLenaId = szLenaId.Substring(1);

                String szMapId = "";
                foreach (String mapId in dayMappings[currentDay].subjectMappings.Keys)
                {
                    if (dayMappings[currentDay].subjectMappings[mapId].lenaId == szLenaId)
                    {
                        szMapId = mapId;
                        break;
                    }
                }


                if (szMapId != "")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(itsFile);
                    XmlNodeList rec = doc.ChildNodes[0].SelectNodes("ProcessingUnit/Recording") != null ? doc.ChildNodes[0].SelectNodes("ProcessingUnit/Recording") : doc.ChildNodes[2].SelectNodes("ProcessingUnit/Recording");

                    foreach (XmlNode recording in rec)
                    {

                        double recStartSecs = Convert.ToDouble(recording.Attributes["startTime"].Value.Substring(2, recording.Attributes["startTime"].Value.Length - 3));
                        DateTime recStartTime = DateTime.Parse(recording.Attributes["startClockTime"].Value);
                        var est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                        recStartTime = TimeZoneInfo.ConvertTime(recStartTime, est);
                        XmlNodeList nodes = recording.SelectNodes("Conversation|Pause");


                        if (Utilities.isSameDay(recStartTime, day) &&
                            recStartTime.Hour >= startHour &&
                            recStartTime.Hour <= endHour)
                        {
                            foreach (XmlNode conv in nodes)
                            {

                                XmlNodeList segments = conv.SelectNodes("Segment");
                                double startSecs = Convert.ToDouble(conv.Attributes["startTime"].Value.Substring(2, conv.Attributes["startTime"].Value.Length - 3)) - recStartSecs;
                                double endSecs = Convert.ToDouble(conv.Attributes["endTime"].Value.Substring(2, conv.Attributes["endTime"].Value.Length - 3)) - recStartSecs;
                                DateTime start = Utilities.geFullTime(recStartTime.AddSeconds(startSecs));
                                DateTime end = Utilities.geFullTime(recStartTime.AddSeconds(endSecs));
                                if (!startTimes.ContainsKey(szLenaId))
                                    startTimes.Add(szLenaId, recStartTime);

                                //if (false)//
                                if (Utilities.isSameDay(start, day) &&
                                start.Hour >= startHour &&
                                (start.Hour < endHour || (start.Hour == endHour && start.Minute <= endMinute)))
                                {
                                    if (conv.Name == "Conversation")
                                    {
                                        double tc = Convert.ToDouble(conv.Attributes["turnTaking"].Value); ;
                                        //TURNCOUNTS


                                        if (tc > 0)
                                        {
                                            DateTime time = start;
                                            int ms = time.Millisecond > 0 ? time.Millisecond / 100 * 100 : time.Millisecond;// + 100;
                                            time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, ms);
                                            double bdSecs = (end - start).Seconds;
                                            double bdMilliseconds = (end - start).Milliseconds > 0 ? ((end - start).Milliseconds / 1000.00) : 0.00;
                                            double bd = bdSecs + bdMilliseconds;
                                             

                                        }
                                    }
                                }
                                //if (false)
                                {
                                    foreach (XmlNode seg in segments)
                                    {

                                        startSecs = Convert.ToDouble(seg.Attributes["startTime"].Value.Substring(2, seg.Attributes["startTime"].Value.Length - 3)) - recStartSecs;
                                        endSecs = Convert.ToDouble(seg.Attributes["endTime"].Value.Substring(2, seg.Attributes["endTime"].Value.Length - 3)) - recStartSecs;
                                        start = Utilities.geFullTime(recStartTime.AddMilliseconds(startSecs * 1000));
                                        end = Utilities.geFullTime(recStartTime.AddMilliseconds(endSecs * 1000));
                                        String speaker = seg.Attributes["spkr"].Value;

              

                                       /* switch (speaker)
                                        {
                                            case "CXN":
                                            case "CXF":
                                                count10 = (1.00 / bd10) / 10.00;
                                                dur10Seg = (bd / bd10) / 10.00;

                                                process = true;
                                                break;

                                            case "FAN":
                                            case "MAN":
                                                Boolean isFemale = speaker == "FAN";
                                                count10 = ((isFemale ? Convert.ToDouble(seg.Attributes["femaleAdultWordCnt"].Value) : Convert.ToDouble(seg.Attributes["maleAdultWordCnt"].Value)) / bd10) / 10.00;
                                                dur10 = ((isFemale ? Convert.ToDouble(seg.Attributes["femaleAdultUttLen"].Value.Substring(1, seg.Attributes["femaleAdultUttLen"].Value.Length - 2)) : Convert.ToDouble(seg.Attributes["maleAdultUttLen"].Value.Substring(1, seg.Attributes["maleAdultUttLen"].Value.Length - 2))) / bd10) / 10.00;
                                                dur10Seg = (bd / bd10) / 10.00;


                                                process = true;
                                                break;
                                        }


                                        */

                                        if (speaker == "CHN" || speaker == "CHF")
                                        {
                                            foreach (XmlAttribute atts in seg.Attributes)
                                            {
                                                if (atts.Name.IndexOf("startUtt") == 0)
                                                {
                                                    String attStep = atts.Name.Substring(8);
                                                    String att = atts.Name;
                                                    double astartSecs = Convert.ToDouble(seg.Attributes[att].Value.Substring(2, seg.Attributes[att].Value.Length - 3)) - recStartSecs;
                                                    double aendSecs = Convert.ToDouble(seg.Attributes["endUtt" + attStep].Value.Substring(2, seg.Attributes["endUtt" + attStep].Value.Length - 3)) - recStartSecs;
                                                    DateTime astart = Utilities.geFullTime(recStartTime.AddMilliseconds(astartSecs * 1000));
                                                    DateTime aend = Utilities.geFullTime(recStartTime.AddMilliseconds(aendSecs * 1000));
                                                     
                                                    AliceLenaSegmets lenaSegmet = new AliceLenaSegmets();
                                                    lenaSegmet.id = szMapId;
                                                    lenaSegmet.lenaId = szLenaId;
                                                    lenaSegmet.types = speaker;
                                                     
                                                    lenaSegmet.startOnset = startSecs;
                                                    lenaSegmet.endOnset = aendSecs;

                                                    lenaSegmet.countL = 1;
                                                    lenaSegmet.startOnsetL = startSecs;
                                                    lenaSegmet.endOnsetL = aendSecs;
                                                    lenaSegmet.hasLena = true;

                                                    lsegments.Add(lenaSegmet);
                                                    dm.subjectMappings[szMapId].lenaUtts++;

                                                }
                                            }
                                            
                                        }

                                    }
                                }

                            }

                        }
                    }
                }


            }

            return lsegments;
        }
        public List<AliceLenaSegmets> setAliceSegments(List<AliceLenaSegmets> lsegments)
        {
            List<AliceLenaSegmets> alsegments = new List<AliceLenaSegmets>();
            List<AliceLenaSegmets> segments = new List<AliceLenaSegmets>();


            if (File.Exists(aliceFile))
            {
                using (StreamReader sr = new StreamReader(aliceFile))
                {
                    //if (!sr.EndOfStream)
                    {
                        //String commaLine = sr.ReadLine();
                        //String[] line = commaLine.Split(' ');

                        while (!sr.EndOfStream)
                        {
                            String commaLine = sr.ReadLine();
                            String[] line = commaLine.Split(' ');

                            if (line.Length > 3 && line[1] != "")
                            {
                                String szLenaId = line[1].Trim();
                                szLenaId = szLenaId.Substring(szLenaId.LastIndexOf("_") + 2);
                                if (szLenaId.Substring(0, 1) == "0")
                                    szLenaId = szLenaId.Substring(1);




                                String szMapId = "";//dayMappings[day].subjectMappings[subject]
                                foreach (String mapId in dayMappings[currentDay].subjectMappings.Keys)
                                {
                                    if (dayMappings[currentDay].subjectMappings[mapId].lenaId == szLenaId)
                                    {
                                        szMapId = mapId;
                                        break;
                                    }
                                }
                                //DateTime time = new DateTime();

                                if (szMapId != "" && startTimes.ContainsKey(szLenaId))
                                {
                                    double aliceDurSecs = Convert.ToDouble(line[4]);
                                    double aliceOnsetSecsStart = Convert.ToDouble(line[3]);
                                    double aliceOnsetSecsEnd = aliceOnsetSecsStart + aliceDurSecs;

                                    //time = new DateTime(startTimes[szLenaId].Year, startTimes[szLenaId].Month, startTimes[szLenaId].Day, startTimes[szLenaId].Hour, startTimes[szLenaId].Minute, startTimes[szLenaId].Second);
                                    //time = time.AddSeconds(Convert.ToDouble(line[3]));

                                    String szType = line[7].Trim();

                                    if (szType == "KCHI")
                                    {
                                        Boolean set = false;
                                        AliceLenaSegmets aliceSegmet = new AliceLenaSegmets();
                                        aliceSegmet.id = szMapId;
                                        aliceSegmet.lenaId = szLenaId;
                                        aliceSegmet.types = szType;

                                        aliceSegmet.startOnset = aliceOnsetSecsStart;
                                        aliceSegmet.endOnset = aliceOnsetSecsEnd;

                                        aliceSegmet.countA = 1;
                                        aliceSegmet.startOnsetA = aliceOnsetSecsStart;
                                        aliceSegmet.endOnsetA = aliceOnsetSecsEnd;
                                        aliceSegmet.hasAlice = true;
                                        alsegments.Add(aliceSegmet);
                                        dm.subjectMappings[szMapId].aliceUtts++;

                                        if (Math.Round(aliceOnsetSecsStart, 3) == 561.936)//1649.992)// == 561.936)
                                        {
                                            bool stop = true;
                                        }
                                         
                                    }

                                }

                            }
                        }
                    }
                }
            }
            /*
            var boundaries = new HashSet<double>();

            foreach (var seg in lsegments)
            {
                boundaries.Add(seg.startOnset);
                boundaries.Add(seg.endOnset);
            }

            foreach (var seg in alsegments)
            {
                boundaries.Add(seg.startOnset);
                boundaries.Add(seg.endOnset);
            }

            var sortedBoundaries = boundaries.OrderBy(x => x).ToList();
            var finalSegments = new List<AliceLenaSegmets>();

            for (int i = 0; i < sortedBoundaries.Count - 1; i++)
            {
                double intervalStart = sortedBoundaries[i];
                double intervalEnd = sortedBoundaries[i + 1];

                // For each ID that exists in either list
                var ids = lsegments.Select(s => s.id)
                           .Union(alsegments.Select(s => s.id))
                           .Distinct();

                foreach (var id in ids)
                {
                    var lenaMatch = lsegments.FirstOrDefault(seg =>
                        seg.id == id &&
                        seg.startOnset < intervalEnd &&
                        seg.endOnset > intervalStart);

                    var aliceMatch = alsegments.FirstOrDefault(seg =>
                        seg.id == id &&
                        seg.startOnset < intervalEnd &&
                        seg.endOnset > intervalStart);

                    bool isMatch = lenaMatch != null && aliceMatch != null;

                    if (lenaMatch != null || aliceMatch != null)
                    {
                        var newSegment = new AliceLenaSegmets
                        {
                            id = id,
                            startOnset = intervalStart,
                            endOnset = intervalEnd,
                            matchFound = isMatch,

                            hasLena = lenaMatch != null,
                            startOnsetL = lenaMatch?.startOnsetL ?? 0,
                            endOnsetL = lenaMatch?.endOnsetL ?? 0,

                            hasAlice = aliceMatch != null,
                            startOnsetA = aliceMatch?.startOnsetA ?? 0,
                            endOnsetA = aliceMatch?.endOnsetA ?? 0
                        };

                        finalSegments.Add(newSegment);
                    }
                }
            }
            */
            var merged = SegmentMerger.MergeSegments(lsegments, alsegments);
            List<AliceLenaSegmets> timeStamps = merged.OrderBy(item => item.id)
                                 .ThenBy(item => item.startOnset)
                                 .ToList(); // Convert back to List<T> if needed;






            return timeStamps;
        }
            public List<AliceLenaSegmets> setAliceSegments1(List<AliceLenaSegmets> lsegments)
        { 
            List<AliceLenaSegmets> alsegments = new List<AliceLenaSegmets>();



            if (File.Exists(aliceFile))
            {
                using (StreamReader sr = new StreamReader(aliceFile))
                {
                    //if (!sr.EndOfStream)
                    {
                        //String commaLine = sr.ReadLine();
                        //String[] line = commaLine.Split(' ');

                        while (!sr.EndOfStream)
                        {
                            String commaLine = sr.ReadLine();
                            String[] line = commaLine.Split(' ');

                            if (line.Length > 3 && line[1] != "")
                            {
                                String szLenaId = line[1].Trim();
                                szLenaId = szLenaId.Substring(szLenaId.LastIndexOf("_") + 2);
                                if (szLenaId.Substring(0, 1) == "0")
                                    szLenaId = szLenaId.Substring(1);




                                String szMapId = "";//dayMappings[day].subjectMappings[subject]
                                foreach (String mapId in dayMappings[currentDay].subjectMappings.Keys)
                                {
                                    if (dayMappings[currentDay].subjectMappings[mapId].lenaId == szLenaId)
                                    {
                                        szMapId = mapId;
                                        break;
                                    }
                                }
                                DateTime time = new DateTime();

                                if (szMapId != "" && startTimes.ContainsKey(szLenaId))
                                {
                                    double aliceDurSecs = Convert.ToDouble(line[4]);
                                    double aliceOnsetSecsStart = Convert.ToDouble(line[3]);
                                    double aliceOnsetSecsEnd = aliceOnsetSecsStart + aliceDurSecs;

                                    time = new DateTime(startTimes[szLenaId].Year, startTimes[szLenaId].Month, startTimes[szLenaId].Day, startTimes[szLenaId].Hour, startTimes[szLenaId].Minute, startTimes[szLenaId].Second);
                                    time = time.AddSeconds(Convert.ToDouble(line[3]));

                                    String szType = line[7].Trim();

                                    if (szType == "KCHI")
                                    {
                                        Boolean set = false;
                                        AliceLenaSegmets aliceSegmet = new AliceLenaSegmets();
                                        aliceSegmet.id = szMapId;
                                        aliceSegmet.lenaId = szLenaId;
                                        aliceSegmet.types = szType;

                                        aliceSegmet.startOnset = aliceOnsetSecsStart;
                                        aliceSegmet.endOnset = aliceOnsetSecsEnd;

                                        aliceSegmet.countA = 1;
                                        aliceSegmet.startOnsetA = aliceOnsetSecsStart;
                                        aliceSegmet.endOnsetA = aliceOnsetSecsEnd;
                                        aliceSegmet.hasAlice = true;
                                        if(Math.Round(aliceOnsetSecsStart,3)== 561.936)//1649.992)// == 561.936)
                                        {
                                            bool stop = true;
                                        }

                                            //LENA 9:10-9:20
                                            foreach (AliceLenaSegmets lseg in lsegments)
                                        { 
                                            if (lseg.id == szMapId)
                                            {
                                                AliceLenaSegmets extraSeg1 = new AliceLenaSegmets();
                                                AliceLenaSegmets extraSeg2 = new AliceLenaSegmets();

                                                if (aliceOnsetSecsStart < (lseg.endOnset)
                                                    && aliceOnsetSecsEnd > lseg.startOnset)//EXCLUDES 9:20-9:21 9:25-9:33
                                                {
                                                    if (aliceOnsetSecsStart < lseg.startOnset) //ALICE 9:01-9:02----  --  9:01-9:20--- -------9:01-9:22      DONE:9:01-9:20 9:01-9:10 9:01-9:12-
                                                    {
                                                        if (aliceOnsetSecsEnd == lseg.endOnset)//  9:01-9:20
                                                        {
                                                            //910 920 LENA ALICE
                                                            extraSeg1 = copyLenaFrom(lseg);
                                                            copyAliceFrom(aliceSegmet, ref extraSeg1);
                                                            extraSeg1.countA = .5;
                                                            extraSeg1.startOnset = lseg.startOnset;
                                                            extraSeg1.endOnset = aliceSegmet.endOnset;

                                                            //901-910 ALICE//
                                                            aliceSegmet.endOnset = lseg.startOnset;
                                                            aliceSegmet.countA = .5;


                                                            alsegments.Add(aliceSegmet);
                                                            alsegments.Add(extraSeg1);
                                                            set = true;
                                                            lseg.matchFound = true;

                                                        }
                                                        else if (aliceOnsetSecsEnd < lseg.endOnset)// 9:01-9:12
                                                        {


                                                            //  9:01-9:10 ALICE   
                                                            aliceSegmet.endOnset = lseg.startOnset;
                                                            aliceSegmet.countA = .5;

                                                            
                                                            
                                                            //    9:10-9:12 ALICE LENA 
                                                            extraSeg1 = copyLenaFrom(lseg);
                                                            copyAliceFrom(aliceSegmet, ref extraSeg1);
                                                            extraSeg1.countL = .5;
                                                            extraSeg1.countA = .5;
                                                            extraSeg1.startOnset = lseg.startOnset;
                                                            extraSeg1.endOnset =aliceOnsetSecsEnd;


                                                            //    9:12-9:20 LENA 
                                                            extraSeg2 = copyLenaFrom(lseg);
                                                            extraSeg2.countL = .5;
                                                            extraSeg2.startOnset = aliceOnsetSecsEnd;
                                                            extraSeg2.endOnset = lseg.endOnset;
                                                             
                                                            alsegments.Add(aliceSegmet);
                                                            alsegments.Add(extraSeg1);
                                                            alsegments.Add(extraSeg2);
                                                            lseg.matchFound = true;
                                                            set = true;
                                                           // break;
                                                        }
                                                        else if (aliceOnsetSecsEnd < lseg.endOnset)// 9:01-9:22
                                                        {
                                                            //LENA ALICE 9:10-9:20
                                                            extraSeg1 = copyLenaFrom(lseg);
                                                            copyAliceFrom(aliceSegmet, ref extraSeg1);
                                                            extraSeg1.startOnset = lseg.startOnset;
                                                            extraSeg1.endOnset = lseg.endOnset;
                                                            extraSeg1.countL = .5;
                                                            extraSeg1.countA = .5;

                                                            //ALICE 9:20-9:22 haslena no alice 
                                                            copyAliceFrom(aliceSegmet, ref extraSeg2);
                                                            extraSeg2.countL = .5;
                                                            extraSeg2.startOnset = lseg.endOnset;
                                                            extraSeg2.endOnset = aliceSegmet.endOnset;

                                                            //ALICE 9:01-9:10
                                                            aliceSegmet.endOnset = lseg.startOnset;


                                                            alsegments.Add(aliceSegmet);
                                                            alsegments.Add(extraSeg1);
                                                            alsegments.Add(extraSeg2);
                                                            lseg.matchFound = true;
                                                            set = true;
                                                          //  break;
                                                        }

                                                    }
                                                    else if (aliceOnsetSecsStart == lseg.startOnset) //9:10-9:12 9:10-9:20 9:10-9:25
                                                    {
                                                        if (aliceOnsetSecsEnd == lseg.endOnset)//  9:10-9:20
                                                        {
                                                            copyLenaFrom(lseg, ref aliceSegmet);
                                                            aliceSegmet.countL = .5;
                                                            aliceSegmet.countA = .5;
                                                            alsegments.Add(aliceSegmet);
                                                            set = true;
                                                            lseg.matchFound = true;
                                                            // break;
                                                        }
                                                        if (aliceOnsetSecsEnd < lseg.endOnset)//  9:10-9:12
                                                        {
                                                            //ALICE LENA to 9:10-9:12 hasalice and lena
                                                            aliceSegmet.countL = .5;
                                                            copyLenaFrom(lseg, ref aliceSegmet);

                                                            //LENA 9:12-9:20 has only lena
                                                            extraSeg1 = copyLenaFrom(lseg);
                                                            extraSeg2.countL = .5;
                                                            extraSeg2.startOnset = aliceSegmet.endOnset;
                                                            extraSeg2.endOnset = lseg.endOnset;

                                                            alsegments.Add(aliceSegmet);
                                                            alsegments.Add(extraSeg1);
                                                            lseg.matchFound = true;
                                                            set = true;
                                                          //  break;
                                                        }
                                                        if (aliceOnsetSecsEnd > lseg.endOnset)//  9:10-9:22
                                                        {

                                                            //LENA ALICE to 9:10-9:20 hasalice
                                                            extraSeg1 = copyLenaFrom(lseg);
                                                            copyAliceFrom(aliceSegmet, ref extraSeg1);
                                                            extraSeg1.startOnset = aliceSegmet.endOnset;
                                                            extraSeg1.endOnset = lseg.endOnset;
                                                            extraSeg1.countA = .5;

                                                            //ALICE 9:20-9:22 
                                                            aliceSegmet.startOnset = lseg.endOnset;
                                                            aliceSegmet.countA = .5;

                                                            alsegments.Add(aliceSegmet);
                                                            alsegments.Add(extraSeg1);

                                                            lseg.matchFound = true;
                                                            set = true;
                                                           // break;
                                                        }
                                                    }
                                                    else if (aliceOnsetSecsStart > lseg.startOnset)//9:12
                                                    {

                                                        if (aliceOnsetSecsEnd < lseg.endOnset)//9:12-9:18
                                                        {

                                                            //LENA add 9:10-9:12 lena 
                                                            extraSeg1 = copyLenaFrom(lseg);
                                                            extraSeg1.startOnset = lseg.startOnset;
                                                            extraSeg1.endOnset = aliceSegmet.startOnset;
                                                            extraSeg1.countL = 1.00 / 3.00;

                                                            //LENA ALICE  9:12-9:18 lena and alice 
                                                            copyLenaFrom(lseg, ref aliceSegmet);
                                                            aliceSegmet.countL = 1.00 / 3.00;


                                                            //LENA add 9:18-9:20 lena
                                                            extraSeg2 = copyLenaFrom(lseg);
                                                            extraSeg2.startOnset = aliceSegmet.endOnset;
                                                            extraSeg2.endOnset = lseg.endOnset;
                                                            extraSeg2.countL = 1.00 / 3.00;

                                                            alsegments.Add(aliceSegmet);
                                                            alsegments.Add(extraSeg1);
                                                            alsegments.Add(extraSeg2);
                                                            set = true;
                                                            lseg.matchFound = true;
                                                        }

                                                        else if (aliceOnsetSecsEnd == lseg.endOnset) //9:12-9:20
                                                        {
                                                            //LENA ALICE add 9:12-9:20 both  

                                                            copyLenaFrom(lseg, ref aliceSegmet);
                                                            aliceSegmet.countL = .5;

                                                            //edit 9:10-9:12  only lena
                                                            extraSeg1 = copyLenaFrom(lseg);
                                                            extraSeg1.startOnset = lseg.startOnset;
                                                            extraSeg1.endOnset = aliceSegmet.startOnset;
                                                            extraSeg1.countL = 1.00 / 2.00;

                                                            alsegments.Add(aliceSegmet);
                                                            alsegments.Add(extraSeg1);
                                                            set = true;
                                                            lseg.matchFound = true;
                                                        }
                                                        if (aliceOnsetSecsEnd > lseg.endOnset)//9:12-9:25
                                                        {
                                                            //LENA 9:10-9:12 LENA/////////////////////
                                                            extraSeg1 = copyLenaFrom(lseg);

                                                            extraSeg1.startOnset = lseg.startOnset;
                                                            extraSeg1.endOnset = aliceSegmet.startOnset;
                                                            extraSeg1.countL = 1.00 / 2.00;

                                                            //LENA ALICE edit 9:12-9:20 LENA ALICE
                                                            copyLenaFrom(lseg, ref aliceSegmet);
                                                            aliceSegmet.countL = .5;
                                                            aliceSegmet.countA = .5;
                                                            aliceSegmet.endOnset = lseg.endOnset;


                                                            //ALICE 9:20-9:25 ALICE 
                                                            extraSeg2 = copyAliceFrom(ref aliceSegmet);
                                                            extraSeg2.startOnset = lseg.endOnset;
                                                            extraSeg2.endOnset = aliceOnsetSecsEnd;
                                                            extraSeg2.countA = 1.00 / 3.00;

                                                             




                                                            
                                                            alsegments.Add(extraSeg1);
                                                            alsegments.Add(aliceSegmet);
                                                            alsegments.Add(extraSeg2);
                                                            set = true;
                                                            lseg.matchFound = true;
                                                        }

                                                    }

                                                }
                                            }


                                        }
                                        if (!set) //9:20-9:21 9:25-9:33
                                        {

                                            alsegments.Add(aliceSegmet);
                                        }
                                    }

                                } 

                            }
                        }
                    }
                }
            }
            foreach (AliceLenaSegmets lseg in lsegments)
            {
                if(!lseg.matchFound)
                {
                    alsegments.Add(lseg);
                }
            }
            //List<AliceLenaSegmets> sortedList = alsegments.OrderBy(o => o.id).ToList().OrderBy(o => o.startOnset).ToList();
            /*var sortedList = originalList.OrderBy(item => item.PrimarySortProperty)
                                 .ThenBy(item => item.SecondarySortProperty)
                                 .ToList(); // Convert back to List<T> if needed*/

            alsegments = alsegments.OrderBy(item => item.id)
                                 .ThenBy(item => item.startOnset)
                                 .ToList(); // Convert back to List<T> if needed
            return alsegments;//.OrderBy(o => o.id).ToList().OrderBy(o => o.startOnset).ToList(); //alsegments;
        }
        public void CompareAliceTen(String paFile, String aFile, Boolean oldMappingFormat, String mapFile)
        {
            mappingFile = mapFile;
            String szDates = "02-16-2018";
            pairActivityFile = paFile;
            aliceFile = aFile;

            foreach (String szDate in szDates.Trim().Split(','))
            {
                currentDay = Convert.ToDateTime(szDate);
                DayMapping dm = new DayMapping();
                dm.setDayMappins1718(mappingFile, currentDay);
                dayMappings.Add(currentDay, dm);



                /*subjectLenas.Add("10D", "14862");
                subjectLenas.Add("1D", "14866");
                subjectLenas.Add("2D", "14865");
                subjectLenas.Add("3D", "14859");
                subjectLenas.Add("4D", "14864");
                subjectLenas.Add("5D", "14870");
                subjectLenas.Add("6D", "14867");
                subjectLenas.Add("7D", "14868");
                subjectLenas.Add("8D", "14861");
                subjectLenas.Add("9D", "8236");
                subjectLenas.Add("Lab1D", "11563");
                subjectLenas.Add("Lab2D", "13841");
                subjectLenas.Add("Lab3D", "11566");
                subjectLenas.Add("Lab4D", "11564");
                subjectLenas.Add("Lab5D", "11563");
                subjectLenas.Add("Lab6D", "24624");
                subjectLenas.Add("T1D", "14863");
                subjectLenas.Add("T2D", "7539");
                subjectLenas.Add("T3D", "11564");


                subjectTypes.Add("10D", "CHILD");
                subjectTypes.Add("1D", "CHILD");
                subjectTypes.Add("2D", "CHILD");
                subjectTypes.Add("3D", "CHILD");
                subjectTypes.Add("4D", "CHILD");
                subjectTypes.Add("5D", "CHILD");
                subjectTypes.Add("6D", "CHILD");
                subjectTypes.Add("7D", "CHILD");
                subjectTypes.Add("8D", "CHILD");
                subjectTypes.Add("9D", "CHILD");
                subjectTypes.Add("Lab1D", "LAB");
                subjectTypes.Add("Lab2D", "LAB");
                subjectTypes.Add("Lab3D", "LAB");
                subjectTypes.Add("Lab4D", "LAB");
                subjectTypes.Add("Lab5D", "LAB");
                subjectTypes.Add("Lab6D", "LAB");
                subjectTypes.Add("T1D", "TEACHER");
                subjectTypes.Add("T2D", "TEACHER");
                subjectTypes.Add("T3D", "TEACHER");*/

        }
    }
        public void setAliceToTenths()
        {
            if (File.Exists(aliceFile))
            {
                using (StreamReader sr = new StreamReader(aliceFile))
                {
                    //if (!sr.EndOfStream)
                    {
                        //String commaLine = sr.ReadLine();
                        //String[] line = commaLine.Split(' ');

                        while (!sr.EndOfStream)
                        {
                            String commaLine = sr.ReadLine();
                            String[] line = commaLine.Split(' ');

                            if (line.Length > 3 && line[1] != "")
                            {
                                String lenaId = line[1].Trim();
                                lenaId = lenaId.Substring(lenaId.LastIndexOf("_") + 2);
                                if (lenaId.Substring(0, 1) == "0")
                                    lenaId = lenaId.Substring(1);

                                
                                
                                
                                String szMapId = "";//dayMappings[day].subjectMappings[subject]
                                foreach (String mapId in dayMappings[currentDay].subjectMappings.Keys)
                                {
                                    if (dayMappings[currentDay].subjectMappings[mapId].lenaId == lenaId)
                                    {
                                        szMapId = mapId;
                                        break;
                                    }
                                }
                                DateTime time = new DateTime();

                                if (szMapId != "" && startTimes.ContainsKey(lenaId))
                                {
                                    time = new DateTime(startTimes[lenaId].Year, startTimes[lenaId].Month, startTimes[lenaId].Day, startTimes[lenaId].Hour, startTimes[lenaId].Minute, startTimes[lenaId].Second);
                                    time = time.AddSeconds(Convert.ToDouble(line[3]));

                                    String szType = line[7].Trim();

                                    DateTime end = Utilities.geFullTime(time.AddSeconds(Convert.ToDouble(line[4])));
                                    DateTime start = time;
                                    double bd = ((end - start).Seconds + ((end - start).Milliseconds)/1000.00);
                                    int ms = time.Millisecond > 0 ? time.Millisecond / 100 * 100 : time.Millisecond;// + 100;
                                    time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, ms);
                                    
                                    DateTime start10 = time;
                                    ms = end.Millisecond > 0 ? end.Millisecond / 100 * 100 : end.Millisecond;// + 100;
                                    DateTime end10 = new DateTime(end.Year, end.Month, end.Day, end.Hour, end.Minute, end.Second, ms);
                                    double bdSecs = (end10 - start10).Seconds;
                                    double bdMilliseconds = (end10 - start10).Milliseconds > 0 ? ((end10 - start10).Milliseconds / 1000.00) : 0.00;
                                    double bd10 = bdSecs + bdMilliseconds;



                                    double dur10 = (bd/bd10) / 10.00;
                                    double count10 = (Convert.ToDouble(line[2]) / bd10) / 10.00; 
                                    //if (szType == "KCHI")
                                    { 
                                        do
                                    {


                                        if (!tenths.ContainsKey(time))
                                        {
                                            tenths.Add(time, new Dictionary<string, aliceAndLenaVars>());
                                        }
                                        if (!tenths[time].ContainsKey(szMapId))
                                        {
                                            tenths[time].Add(szMapId, new aliceAndLenaVars());
                                        }
                                        else
                                        {
                                            bool stop = true;
                                        }

                                        switch (szType)
                                        {
                                            case "KCHI":
                                                tenths[time][szMapId].aliceKChiDur += dur10;
                                                tenths[time][szMapId].aliceKChiCount += count10;
                                                break;
                                            case "CHI":
                                                tenths[time][szMapId].aliceChiDur += dur10;
                                                tenths[time][szMapId].aliceChiCount += count10;
                                                break;
                                            case "FEM":
                                                tenths[time][szMapId].aliceACDur += dur10;
                                                tenths[time][szMapId].aliceACCount += count10;
                                                break;
                                            case "MAL":
                                                tenths[time][szMapId].aliceACDur += dur10;
                                                tenths[time][szMapId].aliceACCount += count10;
                                                break;
                                            case "SPEECH":
                                                tenths[time][szMapId].aliceSpeechDur += dur10;
                                                tenths[time][szMapId].aliceSpeechCount += count10;
                                                break;

                                        }

                                        bd10 = Math.Round(bd10 - 0.10, 2);
                                        time = time.AddMilliseconds(100);

                                    } while (bd10 > 0);
                                }

                                   
                                }

                               // tenths[time][szMapId].lenaTCSegDur += .1;
                               // tenths[time][szMapId].lenaTCCount += tcCount10;

                                //lenasAlice[lenaId].fem

                            }
                        }
                    }
                }
            }
        }
        public void setItsToTenth()
        {
            String szDate = "02-16-2018";
            DateTime day = Convert.ToDateTime(szDate);
            string[] szDayLenaItsFiles = Directory.GetFiles(dir + "//" + szDate + "//", "*.its");

            foreach (string itsFile in szDayLenaItsFiles)
            {
                String szLenaId = itsFile;
                szLenaId = szLenaId.Substring(szLenaId.LastIndexOf("_") + 2).Replace(".its","");
                if (szLenaId.Substring(0, 1) == "0")
                    szLenaId = szLenaId.Substring(1);

                String szMapId = "";
                foreach (String mapId in dayMappings[currentDay].subjectMappings.Keys)
                {
                    if (dayMappings[currentDay].subjectMappings[mapId].lenaId == szLenaId)
                    {
                        szMapId = mapId;
                        break;
                    }
                }

                 
                if (szMapId != "")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(itsFile);
                    XmlNodeList rec = doc.ChildNodes[0].SelectNodes("ProcessingUnit/Recording")!=null? doc.ChildNodes[0].SelectNodes("ProcessingUnit/Recording"): doc.ChildNodes[2].SelectNodes("ProcessingUnit/Recording");

                    foreach (XmlNode recording in rec)
                    {

                        double recStartSecs = Convert.ToDouble(recording.Attributes["startTime"].Value.Substring(2, recording.Attributes["startTime"].Value.Length - 3));
                        DateTime recStartTime = DateTime.Parse(recording.Attributes["startClockTime"].Value);
                        var est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                        recStartTime = TimeZoneInfo.ConvertTime(recStartTime, est);
                        XmlNodeList nodes = recording.SelectNodes("Conversation|Pause");
                         

                        if (Utilities.isSameDay(recStartTime, day) &&
                            recStartTime.Hour >= startHour &&
                            recStartTime.Hour <= endHour)
                        {
                            foreach (XmlNode conv in nodes)
                            {

                                XmlNodeList segments = conv.SelectNodes("Segment");
                                double startSecs = Convert.ToDouble(conv.Attributes["startTime"].Value.Substring(2, conv.Attributes["startTime"].Value.Length - 3)) - recStartSecs;
                                double endSecs = Convert.ToDouble(conv.Attributes["endTime"].Value.Substring(2, conv.Attributes["endTime"].Value.Length - 3)) - recStartSecs;
                                DateTime start = Utilities.geFullTime(recStartTime.AddSeconds(startSecs));
                                DateTime end = Utilities.geFullTime(recStartTime.AddSeconds(endSecs));
                                if (!startTimes.ContainsKey(szLenaId))
                                    startTimes.Add(szLenaId, recStartTime);

                                //if (false)//
                                if(Utilities.isSameDay(start, day) &&
                                start.Hour >= startHour &&
                                (start.Hour < endHour || (start.Hour == endHour && start.Minute <= endMinute)))
                                {
                                    if (conv.Name == "Conversation")
                                    {
                                        double tc = Convert.ToDouble(conv.Attributes["turnTaking"].Value); ;
                                        //TURNCOUNTS


                                        if (tc > 0)
                                        {
                                            DateTime time = start;
                                            int ms = time.Millisecond > 0 ? time.Millisecond / 100 * 100 : time.Millisecond;// + 100;
                                            time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, ms);
                                            double bdSecs = (end - start).Seconds;
                                            double bdMilliseconds = (end - start).Milliseconds > 0 ? ((end - start).Milliseconds / 1000.00) : 0.00;
                                            double bd = bdSecs + bdMilliseconds;

                                            DateTime timeEnd = start.AddSeconds(bd);//    //lenaOnset.durSecs);
                                            ms = timeEnd.Millisecond > 0 ? timeEnd.Millisecond / 100 * 100 : timeEnd.Millisecond;// + 100;
                                            timeEnd = new DateTime(timeEnd.Year, timeEnd.Month, timeEnd.Day, timeEnd.Hour, timeEnd.Minute, timeEnd.Second, ms);

                                            bdSecs = (timeEnd - time).Seconds;
                                            bdMilliseconds = (timeEnd - time).Milliseconds > 0 ? ((timeEnd - time).Milliseconds / 1000.00) : 0.00;
                                            double bd10 = bdSecs + bdMilliseconds;
                                             
                                            double tcDur10 = (bd / bd10) / 10.00;
                                            double tcCount10 = (tc / bd10) / 10;

                                             

                                            do
                                            {

                                                
                                                if (!tenths.ContainsKey(time))
                                                {
                                                    tenths.Add(time, new Dictionary<string, aliceAndLenaVars>());
                                                }
                                                if (!tenths[time].ContainsKey(szMapId))
                                                {
                                                    tenths[time].Add(szMapId, new aliceAndLenaVars());
                                                }
                                                tenths[time][szMapId].lenaTCDur += tcDur10;
                                                tenths[time][szMapId].lenaTCConvCount += tcCount10;
                                                bd10 = Math.Round(bd10 - 0.1, 2);
                                                time = time.AddMilliseconds(100);

                                            } while (bd10 > 0);

                                        }
                                    }
                                }
                                //if (false)
                                {
                                    foreach (XmlNode seg in segments)
                                    {

                                        startSecs = Convert.ToDouble(seg.Attributes["startTime"].Value.Substring(2, seg.Attributes["startTime"].Value.Length - 3)) - recStartSecs;
                                        endSecs = Convert.ToDouble(seg.Attributes["endTime"].Value.Substring(2, seg.Attributes["endTime"].Value.Length - 3)) - recStartSecs;
                                        start = Utilities.geFullTime(recStartTime.AddMilliseconds(startSecs * 1000));
                                        end = Utilities.geFullTime(recStartTime.AddMilliseconds(endSecs * 1000));
                                        String speaker = seg.Attributes["spkr"].Value;

                                        DateTime time = start;
                                        int ms = time.Millisecond > 0 ? time.Millisecond / 100 * 100 : time.Millisecond;// + 100;
                                        time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, ms);
                                        double bdSecs = (end - start).Seconds;
                                        double bdMilliseconds = (end - start).Milliseconds > 0 ? ((end - start).Milliseconds / 1000.00) : 0.00;
                                        double bd = bdSecs + bdMilliseconds;
                                        

                                         
                                        DateTime timeEnd = start.AddSeconds(bd);//    //lenaOnset.durSecs);
                                        ms = timeEnd.Millisecond > 0 ? timeEnd.Millisecond / 100 * 100 : timeEnd.Millisecond;// + 100;
                                        timeEnd = new DateTime(timeEnd.Year, timeEnd.Month, timeEnd.Day, timeEnd.Hour, timeEnd.Minute, timeEnd.Second, ms);


                                        bdSecs = (timeEnd - time).Seconds;
                                        bdMilliseconds = (timeEnd - time).Milliseconds > 0 ? ((timeEnd - time).Milliseconds / 1000.00) : 0.00;
                                        double bd10 = bdSecs + bdMilliseconds;

                                       



                                        double count10 = 0;// (tc / bd) / 10;
                                        double dur10 = 0;// (tc / bd) / 10;
                                        double dur10Seg = 0;// (tc / bd) / 10;
                                        Boolean process = false;

                                        double testUttCountS = 0;

                                        switch (speaker)
                                        {
                                            case "CHN":
                                            case "CHF":
                                                if(Convert.ToDouble(seg.Attributes["childUttCnt"].Value)==0)
                                                {
                                                    bool stop = true;
                                                }
                                                count10 = (Convert.ToDouble(seg.Attributes["childUttCnt"].Value) / bd10) / 10.00;
                                                dur10 = (Convert.ToDouble(seg.Attributes["childUttLen"].Value.Substring(1, seg.Attributes["childUttLen"].Value.Length - 2)) / bd10) / 10.00;
                                                dur10Seg= (bd / bd10) / 10.00; 
                                                 
                                                process = true;
                                                break;

                                            case "CXN":
                                            case "CXF":
                                                count10 = (1.00 / bd10) / 10.00;
                                                dur10Seg = (bd / bd10) / 10.00;

                                                process = true;
                                                break;

                                            case "FAN":
                                            case "MAN":
                                                Boolean isFemale = speaker == "FAN";
                                                count10 = ((isFemale ? Convert.ToDouble(seg.Attributes["femaleAdultWordCnt"].Value) : Convert.ToDouble(seg.Attributes["maleAdultWordCnt"].Value)) / bd10) / 10.00;
                                                dur10 = ((isFemale ? Convert.ToDouble(seg.Attributes["femaleAdultUttLen"].Value.Substring(1, seg.Attributes["femaleAdultUttLen"].Value.Length - 2)) : Convert.ToDouble(seg.Attributes["maleAdultUttLen"].Value.Substring(1, seg.Attributes["maleAdultUttLen"].Value.Length - 2))) / bd10) / 10.00;
                                                dur10Seg = (bd / bd10) / 10.00;


                                                process = true;
                                                break;
                                        }


                                        if (process)
                                            do
                                            {

                                                if (!tenths.ContainsKey(time))
                                                {
                                                    tenths.Add(time, new Dictionary<string, aliceAndLenaVars>());
                                                }
                                                if (!tenths[time].ContainsKey(szMapId))
                                                {
                                                    tenths[time].Add(szMapId, new aliceAndLenaVars());
                                                }

                                                switch (speaker)
                                                {
                                                    case "CHN":
                                                    case "CHF":
                                                        tenths[time][szMapId].lenaKChiSegDur += dur10Seg;
                                                        tenths[time][szMapId].lenaKChiSegUttCount += count10;
                                                        tenths[time][szMapId].lenaKChiSegUttDur += dur10;
                                                        if(speaker=="CHN")
                                                        {
                                                            tenths[time][szMapId].lenaKChiSegDurN += dur10Seg;
                                                            tenths[time][szMapId].lenaKChiSegUttCountN += count10;
                                                            tenths[time][szMapId].lenaKChiSegUttDurN += dur10;
                                                        }
                                                        else
                                                        {
                                                            tenths[time][szMapId].lenaKChiSegDurF += dur10Seg;
                                                            tenths[time][szMapId].lenaKChiSegUttCountF += count10;
                                                            tenths[time][szMapId].lenaKChiSegUttDurF += dur10;
                                                        }
                                                        testUttCountS += count10;
                                                        break;
                                                    case "CXN":
                                                    case "CXF":
                                                        tenths[time][szMapId].lenaChiSegDur += dur10Seg;
                                                        tenths[time][szMapId].lenaChiSegUttCount += count10;
                                                        if (speaker == "CHN")
                                                        {
                                                            tenths[time][szMapId].lenaChiSegDurN += dur10Seg;
                                                            tenths[time][szMapId].lenaChiSegUttCountN += count10;
                                                        }
                                                        else
                                                        {
                                                            tenths[time][szMapId].lenaChiSegDurF += dur10Seg;
                                                            tenths[time][szMapId].lenaChiSegUttCountF += count10;
                                                        }
                                                        break;
                                                    case "FAN":
                                                    case "MAN":
                                                        tenths[time][szMapId].lenaACSegDur += dur10Seg;
                                                        tenths[time][szMapId].lenaACSegWordCount += count10;
                                                        tenths[time][szMapId].lenaACSegUttDur += dur10;
                                                        break;
                                                }

                                                bd10 = Math.Round(bd10 - 0.1, 2);

                                                time = time.AddMilliseconds(100);
                                            } while (bd10 > 0);

                                        double testUttCountU = 0;

                                        if (speaker == "CHN" || speaker == "CHF")
                                        {
                                            foreach (XmlAttribute atts in seg.Attributes)
                                            {
                                                if (atts.Name.IndexOf("startUtt") == 0)
                                                {
                                                    String attStep = atts.Name.Substring(8);
                                                    String att = atts.Name;
                                                    double astartSecs = Convert.ToDouble(seg.Attributes[att].Value.Substring(2, seg.Attributes[att].Value.Length - 3)) - recStartSecs;
                                                    double aendSecs = Convert.ToDouble(seg.Attributes["endUtt" + attStep].Value.Substring(2, seg.Attributes["endUtt" + attStep].Value.Length - 3)) - recStartSecs;
                                                    DateTime astart = Utilities.geFullTime(recStartTime.AddMilliseconds(astartSecs * 1000));
                                                    DateTime aend = Utilities.geFullTime(recStartTime.AddMilliseconds(aendSecs * 1000));

                                                    time = astart;
                                                    ms = time.Millisecond > 0 ? time.Millisecond / 100 * 100 : time.Millisecond;// + 100;
                                                    time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, ms);


                                                    double apiutts = (aend - astart).Seconds + ((aend - astart).Milliseconds > 0 ? (aend - astart).Milliseconds / 1000.00 : 0); //cendSecs - cstartSecs;
                                                     
                                                    ms = aend.Millisecond > 0 ? aend.Millisecond / 100 * 100 : aend.Millisecond;// + 100;
                                                    aend = new DateTime(aend.Year, aend.Month, aend.Day, aend.Hour, aend.Minute, aend.Second, ms);


                                                    bdSecs = (aend - time).Seconds;
                                                    bdMilliseconds = (aend - time).Milliseconds > 0 ? ((aend - time).Milliseconds / 1000.00) : 0.00;
                                                    double apiutts10 = bd10 = bdSecs + bdMilliseconds;

                                                
                                                    count10 = (1.00 / apiutts10) / 10.00;
                                                    dur10 = (apiutts / apiutts10) / 10.00;




                                                    do
                                                    {


                                                        if (!tenths.ContainsKey(time))
                                                        {
                                                            tenths.Add(time, new Dictionary<string, aliceAndLenaVars>());
                                                        }
                                                        if (!tenths[time].ContainsKey(szMapId))
                                                        {
                                                            tenths[time].Add(szMapId, new aliceAndLenaVars());
                                                        }
                                                        
                                                        
                                                        tenths[time][szMapId].lenaKChiUttUttCount += count10;
                                                        tenths[time][szMapId].lenaKChiUttUttDur += dur10;


                                                        if (speaker == "CHN")
                                                        {
                                                            tenths[time][szMapId].lenaKChiUttUttCountN += count10;
                                                            tenths[time][szMapId].lenaKChiUttUttDurN += dur10;
                                                        }
                                                        else
                                                        {
                                                            tenths[time][szMapId].lenaKChiUttUttCountF += count10;
                                                            tenths[time][szMapId].lenaKChiUttUttDurF += dur10;
                                                        }


                                                        testUttCountU += count10;

                                                        apiutts10 = Math.Round(apiutts10 - 0.1, 2);
                                                        time = time.AddMilliseconds(100);

                                                    } while (apiutts10 > 0);
                                                     

                                                }
                                            }
                                            if (Math.Round(testUttCountS, 0) != Math.Round(testUttCountU, 0))
                                            {
                                                bool f = true;
                                            }
                                        }
                                         
                                    }
                                }

                            }

                        }
                    }
                }


            }


        }
    /*    public void compareToITS()
        {
            String szDate = "02-16-2028";
            Dictionary<String, AliceVars> lenasAlice = new Dictionary<String, AliceVars>();
            Dictionary<String, AliceVars> subjectAlicesVars = new Dictionary<String, AliceVars>();
            Dictionary<String, AliceVars> subjectLenasVars = new Dictionary<String, AliceVars>();

            subjectLenas.Add("10D", "14862");
            subjectLenas.Add("1D", "14866");
            subjectLenas.Add("2D", "14865");
            subjectLenas.Add("3D", "14859");
            subjectLenas.Add("4D", "14864");
            subjectLenas.Add("5D", "14870");
            subjectLenas.Add("6D", "14867");
            subjectLenas.Add("7D", "14868");
            subjectLenas.Add("8D", "14861");
            subjectLenas.Add("9D", "8236");
            subjectLenas.Add("Lab1D", "11563");
            subjectLenas.Add("Lab2D", "13841");
            subjectLenas.Add("Lab3D", "11566");
            subjectLenas.Add("Lab4D", "11564");
            subjectLenas.Add("Lab5D", "11563");
            subjectLenas.Add("Lab6D", "24624");
            subjectLenas.Add("T1D", "14863");
            subjectLenas.Add("T2D", "7539");
            subjectLenas.Add("T3D", "11564");


            subjectTypes.Add("10D", "CHILD");
            subjectTypes.Add("1D", "CHILD");
            subjectTypes.Add("2D", "CHILD");
            subjectTypes.Add("3D", "CHILD");
            subjectTypes.Add("4D", "CHILD");
            subjectTypes.Add("5D", "CHILD");
            subjectTypes.Add("6D", "CHILD");
            subjectTypes.Add("7D", "CHILD");
            subjectTypes.Add("8D", "CHILD");
            subjectTypes.Add("9D", "CHILD");
            subjectTypes.Add("Lab1D", "LAB");
            subjectTypes.Add("Lab2D", "LAB");
            subjectTypes.Add("Lab3D", "LAB");
            subjectTypes.Add("Lab4D", "LAB");
            subjectTypes.Add("Lab5D", "LAB");
            subjectTypes.Add("Lab6D", "LAB");
            subjectTypes.Add("T1D", "TEACHER");
            subjectTypes.Add("T2D", "TEACHER");
            subjectTypes.Add("T3D", "TEACHER");


            if (File.Exists(aliceFile))
            {
                using (StreamReader sr = new StreamReader(aliceFile))
                {
                    if (!sr.EndOfStream)
                    {
                        String commaLine = sr.ReadLine();
                        String[] line = commaLine.Split(' ');

                        while (!sr.EndOfStream)
                        {
                            commaLine = sr.ReadLine();
                            line = commaLine.Split(' ');
                            if (line.Length > 3 && line[1] != "")
                            {
                                String lenaId = line[1].Trim();
                                lenaId = lenaId.Substring(lenaId.LastIndexOf("_") + 2);
                                if (lenaId.Substring(0, 1) == "0")
                                    lenaId = lenaId.Substring(1);

                                if (!lenasAlice.ContainsKey(lenaId))
                                {
                                    lenasAlice.Add(lenaId, new AliceVars());
                                }
                                String szType = line[7].Trim();
                                Double dur = Convert.ToDouble(line[4]);

                                switch (szType)
                                {
                                    case "KCHI":
                                        lenasAlice[lenaId].kchi += Convert.ToInt16(line[2]);
                                        lenasAlice[lenaId].kchiDur += dur;
                                        break;
                                    case "CHI":
                                        lenasAlice[lenaId].chi += Convert.ToInt16(line[2]);
                                        lenasAlice[lenaId].chiDur += dur;

                                        break;
                                    case "FEM":
                                        lenasAlice[lenaId].fem += Convert.ToInt16(line[2]);
                                        lenasAlice[lenaId].femDur += dur;

                                        break;
                                    case "MAL":
                                        lenasAlice[lenaId].mal += Convert.ToInt16(line[2]);
                                        lenasAlice[lenaId].malDur += dur;

                                        break;
                                    case "SPEECH":
                                        lenasAlice[lenaId].speech += Convert.ToInt16(line[2]);
                                        lenasAlice[lenaId].speechDur += dur;

                                        break;

                                }
                               


                                //lenasAlice[lenaId].fem

                            }
                        }
                    }
                }
                TextWriter sw = new StreamWriter(pairActivityFile.Replace(".", "_ALICECOMPARE_" + new Random().Next() + "."));
                sw.WriteLine("SUBJECT,TYPE,DATE,LENA_VD,LENA_VC,LENA_TC,LENA_AC,ALICE_KCHI_COUNT,ALICE_KCHI_DUR,ALICE_CHI_COUNT,ALICE_CHI_DUR,ALICE_FEM_COUNT,ALICE_FEM_DUR ,ALICE_MAL_COUNT, ALICE_MAL_DUR,ALICE_SPEECH_COUNT, ALICE_SPEECH_DUR");


                using (StreamReader sr = new StreamReader(pairActivityFile))
                {
                    Dictionary<String, int> columIdx = new Dictionary<String, int>();
                    columIdx.Add("Subject", -1);
                    columIdx.Add("Date", -1);
                    columIdx.Add("TotalVD", -1);
                    columIdx.Add("TotalVC", -1);
                    columIdx.Add("TotalTC", -1);
                    columIdx.Add("TotalAC", -1);

                    if (!sr.EndOfStream)
                    {
                        String commaLine = sr.ReadLine();
                        String[] line = commaLine.Split(',');
                        int idx = 0;
                        foreach (String col in line)
                        {
                            if (col == "TotalVD")
                                columIdx[col] = idx;
                            else if (col == "TotalVC")
                                columIdx[col] = idx;
                            else if (col == "TotalTC")
                                columIdx[col] = idx;
                            else if (col == "TotalAC")
                                columIdx[col] = idx;
                            else if (col == "Date")
                                columIdx[col] = idx;
                            else if (col == "Subject")
                                columIdx["Subject"] = idx;

                            idx++;
                        }
                        //TotalVD TotalVC TotalTC TotalAC

                        while (!sr.EndOfStream)
                        {
                            commaLine = sr.ReadLine();
                            line = commaLine.Split(',');
                            if (line.Length > 3 && line[1] != "")
                            {
                                String subjectId = line[columIdx["Subject"]].Trim();
                                if (!subjectAlicesVars.ContainsKey(subjectId))
                                {
                                    String lenaId = subjectLenas[subjectId];
                                    if (lenasAlice.ContainsKey(lenaId))
                                    {
                                        subjectAlicesVars.Add(subjectId, lenasAlice[lenaId]);
                                        sw.WriteLine(subjectId + "," + subjectTypes[subjectId] + "," + line[columIdx["Date"]] + "," +
                                        line[columIdx["TotalVD"]] + "," + line[columIdx["TotalVC"]] + "," + line[columIdx["TotalTC"]] + "," + line[columIdx["TotalAC"]] + "," +
                                        lenasAlice[lenaId].kchi + "," + lenasAlice[lenaId].kchiDur + "," +
                                        lenasAlice[lenaId].chi + "," + lenasAlice[lenaId].chiDur + "," +
                                        lenasAlice[lenaId].fem + "," + lenasAlice[lenaId].femDur + "," +
                                        lenasAlice[lenaId].mal + "," + lenasAlice[lenaId].malDur + "," +
                                        lenasAlice[lenaId].speech + "," + lenasAlice[lenaId].speechDur);
                                    }
                                }

                            }
                        }
                    }
                }
                sw.Close();
            }
        }
        public void compareToPairActivity()
        {
            Dictionary<String, AliceVars> lenasAlice = new Dictionary<String, AliceVars>();
            Dictionary<String, AliceVars> subjectAlicesVars = new Dictionary<String, AliceVars>();
            Dictionary<String, AliceVars> subjectLenasVars = new Dictionary<String, AliceVars>();

            Dictionary<String, String> subjectLenas = new Dictionary<String, String>();
            subjectLenas.Add("10D", "14862");
            subjectLenas.Add("1D", "14866");
            subjectLenas.Add("2D", "14865");
            subjectLenas.Add("3D", "14859");
            subjectLenas.Add("4D", "14864");
            subjectLenas.Add("5D", "14870");
            subjectLenas.Add("6D", "14867");
            subjectLenas.Add("7D", "14868");
            subjectLenas.Add("8D", "14861");
            subjectLenas.Add("9D", "8236");
            subjectLenas.Add("Lab1D", "11563");
            subjectLenas.Add("Lab2D", "13841");
            subjectLenas.Add("Lab3D", "11566");
            subjectLenas.Add("Lab4D", "11564");
            subjectLenas.Add("Lab5D", "11563");
            subjectLenas.Add("Lab6D", "24624");
            subjectLenas.Add("T1D", "14863");
            subjectLenas.Add("T2D", "7539");
            subjectLenas.Add("T3D", "11564");

            Dictionary<String, String> subjectTypes = new Dictionary<String, String>();
            subjectTypes.Add("10D", "CHILD");
            subjectTypes.Add("1D", "CHILD");
            subjectTypes.Add("2D", "CHILD");
            subjectTypes.Add("3D", "CHILD");
            subjectTypes.Add("4D", "CHILD");
            subjectTypes.Add("5D", "CHILD");
            subjectTypes.Add("6D", "CHILD");
            subjectTypes.Add("7D", "CHILD");
            subjectTypes.Add("8D", "CHILD");
            subjectTypes.Add("9D", "CHILD");
            subjectTypes.Add("Lab1D", "LAB");
            subjectTypes.Add("Lab2D", "LAB");
            subjectTypes.Add("Lab3D", "LAB");
            subjectTypes.Add("Lab4D", "LAB");
            subjectTypes.Add("Lab5D", "LAB");
            subjectTypes.Add("Lab6D", "LAB");
            subjectTypes.Add("T1D", "TEACHER");
            subjectTypes.Add("T2D", "TEACHER");
            subjectTypes.Add("T3D", "TEACHER");

            if (File.Exists(pairActivityFile) && File.Exists(aliceFile))
            {
                using (StreamReader sr = new StreamReader(aliceFile))
                {
                    if (!sr.EndOfStream)
                    {
                        String commaLine = sr.ReadLine();
                        String[] line = commaLine.Split(' ');

                        while (!sr.EndOfStream)
                        {
                            commaLine = sr.ReadLine();
                            line = commaLine.Split(' ');
                            if (line.Length > 3 && line[1] != "")
                            {
                                String lenaId = line[1].Trim();
                                lenaId= lenaId.Substring(lenaId.LastIndexOf("_")+2);
                                if (lenaId.Substring(0, 1) == "0")
                                    lenaId = lenaId.Substring(1);

                                if(!lenasAlice.ContainsKey(lenaId))
                                {
                                    lenasAlice.Add(lenaId, new AliceVars());
                                }
                                String szType = line[7].Trim();
                                Double dur = Convert.ToDouble(line[4]);

                                switch (szType)
                                {
                                    case "KCHI":
                                        lenasAlice[lenaId].kchi += Convert.ToInt16(line[2]);
                                        lenasAlice[lenaId].kchiDur += dur;
                                        break;
                                    case "CHI":
                                        lenasAlice[lenaId].chi += Convert.ToInt16(line[2]);
                                        lenasAlice[lenaId].chiDur += dur;

                                        break;
                                    case "FEM":
                                        lenasAlice[lenaId].fem += Convert.ToInt16(line[2]);
                                        lenasAlice[lenaId].femDur += dur;

                                        break;
                                    case "MAL":
                                        lenasAlice[lenaId].mal += Convert.ToInt16(line[2]);
                                        lenasAlice[lenaId].malDur += dur;

                                        break;
                                    case "SPEECH":
                                        lenasAlice[lenaId].speech += Convert.ToInt16(line[2]);
                                        lenasAlice[lenaId].speechDur += dur;

                                        break;

                                }
                                //lenasAlice[lenaId].fem

                            }
                        }
                    }
                }
                TextWriter sw = new StreamWriter(pairActivityFile.Replace(".","_ALICECOMPARE_"+ new Random().Next()+"."));
                sw.WriteLine("SUBJECT,TYPE,DATE,LENA_VD,LENA_VC,LENA_TC,LENA_AC,ALICE_KCHI_COUNT,ALICE_KCHI_DUR,ALICE_CHI_COUNT,ALICE_CHI_DUR,ALICE_FEM_COUNT,ALICE_FEM_DUR ,ALICE_MAL_COUNT, ALICE_MAL_DUR,ALICE_SPEECH_COUNT, ALICE_SPEECH_DUR");


                using (StreamReader sr = new StreamReader(pairActivityFile))
                {
                    Dictionary<String,int> columIdx = new Dictionary<String,int>();
                    columIdx.Add("Subject", -1);
                    columIdx.Add("Date", -1);
                    columIdx.Add("TotalVD", -1);
                    columIdx.Add("TotalVC", -1);
                    columIdx.Add("TotalTC", -1);
                    columIdx.Add("TotalAC", -1);

                    if (!sr.EndOfStream)
                    {
                        String commaLine = sr.ReadLine();
                        String[] line = commaLine.Split(',');
                        int idx= 0;
                        foreach (String col in line)
                        {
                            if (col == "TotalVD")
                                columIdx[col]=idx ;
                            else if (col == "TotalVC")
                                columIdx[col] = idx;
                            else if (col == "TotalTC")
                                columIdx[col] = idx;
                            else if (col == "TotalAC")
                                columIdx[col] = idx;
                            else if (col == "Date")
                                columIdx[col] = idx;
                            else if (col == "Subject")
                                columIdx["Subject"]=idx;

                            idx++;
                        }
                        //TotalVD TotalVC TotalTC TotalAC

                        while (!sr.EndOfStream)
                        {
                            commaLine = sr.ReadLine();
                            line = commaLine.Split(',');
                            if (line.Length > 3 && line[1] != "")
                            {
                                String subjectId = line[columIdx["Subject"]].Trim();
                                if(!subjectAlicesVars.ContainsKey(subjectId))
                                {
                                    String lenaId = subjectLenas[subjectId];
                                    if (lenasAlice.ContainsKey(lenaId))
                                    {
                                        subjectAlicesVars.Add(subjectId, lenasAlice[lenaId]);
                                        sw.WriteLine(subjectId + "," + subjectTypes[subjectId] + "," + line[columIdx["Date"]] + "," +
                                        line[columIdx["TotalVD"]] + "," + line[columIdx["TotalVC"]] + "," + line[columIdx["TotalTC"]] + "," + line[columIdx["TotalAC"]] + "," +
                                        lenasAlice[lenaId].kchi + "," + lenasAlice[lenaId].kchiDur + "," +
                                        lenasAlice[lenaId].chi + "," + lenasAlice[lenaId].chiDur + "," +
                                        lenasAlice[lenaId].fem + "," + lenasAlice[lenaId].femDur + "," +
                                        lenasAlice[lenaId].mal + "," + lenasAlice[lenaId].malDur + "," +
                                        lenasAlice[lenaId].speech + "," + lenasAlice[lenaId].speechDur);
                                    }
                                }

                            }
                        }
                    }
                }
                sw.Close();
            }
        }*/
        }
    }

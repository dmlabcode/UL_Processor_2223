using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static IronPython.Modules._ast;
using System.Xml;
using IronPython.Compiler;
using System.Web.UI.WebControls;
using System.Security.Policy;
using System.Threading;

namespace UL_Processor_V2023
{
    class Classroom
    {
        public Boolean isSewio = false;
        public Boolean includeLabs = true;
        public Boolean justPLS = false;
        public Boolean ubiCleanup = false;
        public Boolean doRecInfo = true;
        public Boolean processData = true;
        public Boolean reDenoise = false;
        public Boolean kalman = true;
        public Boolean includeAlice = false;
        public String dir = "";
        public String className = "";
        public String classFolder = "";
        public String mapPrefix = "";
        public double grMin = 0;
        public double grMax = 0;
        public double angle = 45;
        public String mapById = "LONGID";
        public List<DateTime> classRoomDays = new List<DateTime>();
        public Dictionary<String, Person> personBaseMappings = new Dictionary<string, Person>();
        public int startHour = 7;
        public int endHour = 16;
        public int endMinute = 0;
        public Dictionary<String, List<String>> filesToMerge = new Dictionary<String, List<string>>();
        public Boolean toFilter = false;
        public double maxY = 2;
        public double maxGapSecs = 60;

        public List<String> diagnosisList = new List<string>();
        public List<String> languagesList = new List<string>();
        int numCols = 0;

        public void getPairActLeadsFromFiles()
        {
            getPairActLeadsFromFiles("SYNC", "PAIRACTIVITY", "PAIRACTIVITY");
        }
        public void getPairActLeadsFromDir(String subFolder, String filePrefix)
        {
            getPairActLeadsFromFiles("SYNC", subFolder, filePrefix);
        }
        public void getPairActLeadsFromFiles(String syncFolder, String subFolder, String filePrefix)
        {
            String reportClassYear = className;
            String fileFolder = subFolder != "" ? subFolder : "PAIRACTIVITY";
            String classYear = "";
            try
            {
                classYear = string.Join("", reportClassYear.Where(Char.IsDigit));

            }
            catch (Exception e) 
            {
            
            }
            if(classYear =="")
            {
                if(this.classRoomDays.Count > 0)
                {
                    classYear = this.classRoomDays[0].Year.ToString();
                    if (this.classRoomDays.Count > 1)
                    {
                        classYear = classYear+this.classRoomDays[this.classRoomDays.Count-1].Year.ToString();
                    }
                }
                reportClassYear = reportClassYear + classYear;
            }
             
            TextWriter sw = new StreamWriter(dir + "//"+syncFolder+"//"+ fileFolder+"//"+ filePrefix + "_" + reportClassYear+"_"+ Utilities.szVersion + "ALL.CSV");
            int numOfDays = classRoomDays.Count;
            Dictionary<String, String> prevPairLines = new Dictionary<string, string>();
            Dictionary<String, String> pairLines = new Dictionary<string, string>();
           
            foreach (DateTime dayDate in classRoomDays)
            {
                pairLines = new Dictionary<string, string>();
                String[] szFiles = Directory.GetFiles(dir + "//"+syncFolder+"//"+ fileFolder+"//");
                String fileDayPart = Utilities.getDateStr(dayDate, "", 2);
                String headerLine = "";

                foreach (String szFile in szFiles)
                {
                    String szFileName= szFile.Substring(szFile.LastIndexOf("/")+1);
                    if (szFileName.Contains(fileDayPart) && szFileName.Contains(filePrefix) && szFileName.Contains(Utilities.szVersion + "."))
                    {
                        using (StreamReader sr = new StreamReader(szFile))
                        {
                            if (!sr.EndOfStream)
                            {
                                if (numOfDays == classRoomDays.Count)
                                    headerLine = sr.ReadLine();//12 on
                                else
                                    sr.ReadLine();
                            }
                            
                            if (headerLine != "")
                            {
                                String[] headerCols = headerLine.Split ( ',');
                                numCols = headerCols.Length;
                                sw.WriteLine(headerLine + ",Lead_" + headerLine.Replace(",", ",Lead_"));
                                headerLine = "";
                            }
                            while ((!sr.EndOfStream))// && lineCount < 10000)
                            {
                                String commaLine = sr.ReadLine();
                                String[] commaLineCols = commaLine.Split(',');
                                if (commaLineCols.Length > 33)
                                {
                                    //String pairKey = commaLineCols[3].Trim() != "" && commaLineCols[4].Trim() != "" ? commaLineCols[3] + "-" + commaLineCols[4] : commaLineCols[1] + "-" + commaLineCols[2];
                                    String pairKey = commaLineCols[1] + "-" + commaLineCols[2];
                                    pairLines.Add(pairKey, commaLine);
                                }
                            }
                        }

                        if (prevPairLines.Keys.Count > 0)
                            getPairActLead(ref sw, prevPairLines, pairLines);
                        prevPairLines = pairLines;

                        break;

                    }

                }

                numOfDays--;
            }
            if (prevPairLines.Keys.Count > 0)
                getPairActLead(ref sw, prevPairLines, new Dictionary<string, string>());

            sw.Close();
        }

        public void getPairActLead(ref TextWriter sw, Dictionary<String, String> prevPairLines, Dictionary<String, String> pairLines )
        {
            foreach (String szPair in prevPairLines.Keys)
            {
                String leadLine = pairLines.ContainsKey(szPair) ? pairLines[szPair] : new StringBuilder().Insert(0, "NA,", numCols).ToString() ;

                sw.WriteLine(prevPairLines[szPair] + "," + leadLine);

            }

        }

        public void setBaseMappings()
        {
            String mappingBaseFileName = dir + "//MAPPING_" + (mapPrefix!=""?mapPrefix: className) + "_BASE.CSV";
            List<int> dList = new List<int>();
            List<int> lList = new List<int>();
            Dictionary<String, int> columnIndexBase = new Dictionary<string, int>();
            columnIndexBase.Add("LONGID", -1);
            columnIndexBase.Add("SHORTID", -1);
            columnIndexBase.Add("TYPE", -1);
            columnIndexBase.Add("DOB", -1);
            columnIndexBase.Add("SEX", -1);

            columnIndexBase.Add("PREPLSDATE", -1);
            columnIndexBase.Add("PREPLSLENA", -1);
            columnIndexBase.Add("POSTPLSDATE", -1);
            columnIndexBase.Add("POSTPLSLENA", -1);


            if (File.Exists(mappingBaseFileName))
                using (StreamReader sr = new StreamReader(mappingBaseFileName))
                {
                    if (!sr.EndOfStream)
                    {//
                        String commaLine = sr.ReadLine();
                        String[] line = commaLine.Split(',');
                        int cp = line[0].Trim().ToUpper()!="ROSTER"?0:1;
                        foreach (String szCol in line)
                        {
                            if (szCol.ToUpper().Trim().Contains("DIAGNOSIS") || szCol.ToUpper().Trim().Contains("DEVICE"))
                            {
                                diagnosisList.Add(szCol.Trim().Replace("Subject_", "").Replace("Subject", ""));
                                dList.Add(cp);
                            }
                            else if (szCol.ToUpper().Trim().Contains("LANGUAGE"))
                            {
                                languagesList.Add(szCol.Trim());
                                lList.Add(cp);
                            }
                            else if (szCol.ToUpper().Trim().Contains("ID") && (szCol.ToUpper().Trim().Contains("SUBJECT")|| szCol.ToUpper().Trim().Contains("LONG")))
                            {
                                columnIndexBase["LONGID"] = cp;
                            }
                            else if (szCol.ToUpper().Trim().Contains("ID") && szCol.ToUpper().Trim().Contains("SHORT"))
                            {
                                columnIndexBase["SHORTID"] = cp;
                            }
                            else if (szCol.ToUpper().Trim().Contains("TYPE")  )
                            {
                                columnIndexBase["TYPE"] = cp;
                            }
                            else if (szCol.ToUpper().Trim().Contains("DOB"))
                            {
                                columnIndexBase["DOB"] = cp;
                            }
                            else if (szCol.ToUpper().Trim().Contains("SEX") || szCol.ToUpper().Trim().Contains("GENDER"))
                            {
                                columnIndexBase["SEX"] = cp;
                            }
                            else if (szCol.ToUpper().Trim().Contains("PLS"))
                            {
                                if (szCol.ToUpper().Trim().Contains("PRE"))
                                {
                                    if (szCol.ToUpper().Trim().Contains("DATE"))
                                    {
                                        columnIndexBase["PREPLSDATE"] = cp;
                                    }
                                    else if (szCol.ToUpper().Trim().Contains("LENA"))
                                    {
                                        columnIndexBase["PREPLSLENA"] = cp;
                                    }
                                }
                                else if (szCol.ToUpper().Trim().Contains("POST"))
                                {
                                    if (szCol.ToUpper().Trim().Contains("DATE"))
                                    {
                                        columnIndexBase["POSTPLSDATE"] = cp;
                                    }
                                    else if (szCol.ToUpper().Trim().Contains("LENA"))
                                    {
                                        columnIndexBase["POSTPLSLENA"] = cp;
                                    }
                                }
                            }
                            cp++;

                        }

                    }

                    while ((!sr.EndOfStream))// && lineCount < 10000)
                    {
                        String commaLine = sr.ReadLine();
                        String[] line = commaLine.Split(',');
                        if (line.Length > 5 && line[1] != "")
                        {
                            Person person = new Person(commaLine, mapById, dList,lList,columnIndexBase);//longid
                             
                            if (person.mapId != "" && (!personBaseMappings.ContainsKey(person.mapId)) && (this.includeLabs || person.subjectType!="LAB") )
                            {
                                personBaseMappings.Add(person.mapId, person);
                            }

                        }
                    }
                }
        }
        public void setDirs()
        {
            classFolder = classFolder == "" ? className : classFolder;
            mapPrefix = mapPrefix == "" ? className : mapPrefix;
            dir = dir + classFolder;
        }
        public void createReportDirs()
        {
            if (!Directory.Exists(dir + "//SYNC"))
                Directory.CreateDirectory(dir + "//SYNC");
            if (!Directory.Exists(dir + "//SYNC//ONSETS"))
                Directory.CreateDirectory(dir + "//SYNC//ONSETS");
            if (!Directory.Exists(dir + "//SYNC//SOCIALONSETS"))
                Directory.CreateDirectory(dir + "//SYNC//SOCIALONSETS");
            if (!Directory.Exists(dir + "//SYNC//GR"))
                Directory.CreateDirectory(dir + "//SYNC//GR");
            if (!Directory.Exists(dir + "//SYNC//COTALK"))
                Directory.CreateDirectory(dir + "//SYNC//COTALK");
            if (!Directory.Exists(dir + "//SYNC//PAIRACTIVITY"))
                Directory.CreateDirectory(dir + "//SYNC//PAIRACTIVITY");

            if (!Directory.Exists(dir + "//SYNC//PAIRANGLES"))
                Directory.CreateDirectory(dir + "//SYNC//PAIRANGLES");

            if (!Directory.Exists(dir + "//SYNC//APPROACH"))
                Directory.CreateDirectory(dir + "//SYNC//APPROACH");
            if (!Directory.Exists(dir + "//SYNC//UBIGR"))
                Directory.CreateDirectory(dir + "//SYNC//UBIGR");
        }
        public void makeDayReportLists()
        {
            filesToMerge.Add("ONSETS", new List<string>());
            filesToMerge.Add("ACTIVITIES", new List<string>());
            filesToMerge.Add("PAIRACTIVITIES", new List<string>());
            filesToMerge.Add("MINACTIVITIES", new List<string>());
            filesToMerge.Add("SOCIALONSETS", new List<string>());

        }
        public void cleanUbiFiles()
        {
            foreach (DateTime day in classRoomDays)
            {
                ClassroomDay classRoomDay = new ClassroomDay(day);
                //classRoomDay.setMappings(dir + "//" + Utilities.getDateDashStr(day) + "//MAPPINGS//MAPPING_" + className + ".CSV", personBaseMappings, mapById, startHour, endHour, endMinute);
                classRoomDay.setMappings(dir ,mapPrefix, personBaseMappings, mapById, startHour, endHour, endMinute);

                if (this.isSewio)
                    handleSewio(ref classRoomDay);

                //CLEAN UBI
                classRoomDay.createCleanUbiFile(dir, startHour, endHour);
            }

       }
        public void handleSewio(ref ClassroomDay classRoomDay)
        {
            //HANDLE SWEIO TO UBI FORMAT
            Dictionary<DateTime, List<String>> timeSewios = new Dictionary<DateTime, List<string>>();
            String szUbiFolder = dir + "//" + Utilities.getDateDashStr(classRoomDay.classDay) + "//Ubisense_Data";

            if (Directory.Exists(szUbiFolder))
            {
                Directory.Delete(szUbiFolder, true);

            }
            Directory.CreateDirectory(szUbiFolder);
            TextWriter sw = new StreamWriter(szUbiFolder+ "//MiamiLocation."+ Utilities.getDateDashStr(classRoomDay.classDay)+"_00-00-SE-WIO.log");// dir + "//" + syncFolder + "//PAIRACTIVITY//PAIRACTIVITY_" + reportClassYear + "_" + Utilities.szVersion + "ALL.CSV");

            String sewioFolder = dir + "//" + Utilities.getDateDashStr(classRoomDay.classDay) + "//Sewio_Data"; 
            if (Directory.Exists(sewioFolder))
            {
                String[] files = Directory.GetFiles(sewioFolder);
                foreach(String f in files)
                {
                    if(f.EndsWith(".csv"))
                    {
                        using (StreamReader sr = new StreamReader(f))
                        {
                            if (!sr.EndOfStream)
                            {
                                sr.ReadLine();
                            }

                            while (!sr.EndOfStream)
                            {
                                String szLine = sr.ReadLine();
                                String[] lineCols = szLine.Split(',');
                                //TagLabel	X	Y	Time (UTC)
                                if (lineCols.Length > 3 && lineCols[3] != "")
                                {
                                    //Location,00:11:CE:00:00:00:FE:CA,2024-05-17 08:58:23.004,1.59619808197021,3.38081121444702,0.5,
                                    String szSewioTag = lineCols[0].Trim();//1lb t1lb 11lb
                                    String szSide = szSewioTag.Contains("B")? szSewioTag.Substring(szSewioTag.Length - 2,1):szSewioTag.Substring(szSewioTag.Length - 1);
                                        //1L 11L T11L 1LB 11LB T11LB
                                        szSewioTag =    (szSewioTag.Length == 2 ? "00:11:CE:00:00:00:00:" + szSewioTag :
                                                          szSewioTag.Length == 3 ? "00:11:CE:00:00:00:0" + szSewioTag.Substring(0, 1) + ":" + szSewioTag.Substring(1, 2) :
                                                          szSewioTag.Length == 4 ? "00:11:CE:00:00:00:" + szSewioTag.Substring(0, 2) + ":" + szSewioTag.Substring(2, 2) :
                                                          szSewioTag.Length == 5 ? "00:11:CE:00:00:0" + szSewioTag.Substring(0, 1) + ":" + szSewioTag.Substring(1, 2) + ":" + szSewioTag.Substring(2, 2) : "") ;

                                        String szDate = lineCols[3];
                                        if (!(szDate.Contains("-")|| szDate.Contains("/")))
                                        {
                                            szDate = Utilities.getDateDashStr(classRoomDay.classDay) + " " + szDate;
                                        }
                                        DateTime convertedDate = DateTime.SpecifyKind(
                                                                 DateTime.Parse(szDate),
                                                                 DateTimeKind.Utc);
                                        TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                                        DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(convertedDate, easternZone);
                                        //easternTime = easternTime.AddHours(-1);
                                        String szTime = Utilities.getDateStrYYMMDD(easternTime, "-", 0)+" "+Utilities.getTimeStr(easternTime);
                                        if(!timeSewios.ContainsKey(easternTime))
                                        {
                                            timeSewios.Add(easternTime, new List<String>());
                                        }
                                        timeSewios[easternTime].Add("Location," + szSewioTag + "," + szTime + "," + lineCols[1] + "," + lineCols[2] + ",0.5");
                                        //sw.WriteLine("Location," + szSewioTag+ "," + szTime +","+ lineCols[1] + "," + lineCols[2]+ ",0.5");
                                    }
                                
                            }
                        }
                    }
                }

            }
            timeSewios = timeSewios.OrderBy(x => x.Key).ThenBy(x => x.Key.Millisecond).ToDictionary(x => x.Key, x => x.Value);
            
            foreach(DateTime dt in timeSewios.Keys)
            {
                foreach(String line in timeSewios[dt])
                {
                    sw.WriteLine(line);
                }
            }
            
            sw.Close();
        }
        public void denoise()
        {
            foreach (DateTime day in classRoomDays)
            {
                ClassroomDay classRoomDay = new ClassroomDay(day);
                classRoomDay.setMappings(dir, mapPrefix, personBaseMappings, mapById, startHour, endHour, endMinute);
                //classRoomDay.setMappings(dir + "//" + Utilities.getDateDashStr(day) + "//MAPPINGS//MAPPING_" + className + ".CSV", personBaseMappings, mapById, startHour, endHour, endMinute);

                //CLEAN, DENOISE
                if (this.reDenoise)
                {
                    String szDayFolder = Utilities.getDateDashStr(day);
                    String szDenoisedFolder = dir + "//" + szDayFolder + "//Ubisense_Denoised_Data";

                    if (Directory.Exists(szDenoisedFolder))
                    {
                        Directory.Delete(szDenoisedFolder,true);
                    }


                }
                classRoomDay.createDenoisedFile(dir, mapPrefix);//, startHour, endHour);
            }

        }

        public void mergeAndCleanExistingDenoised()
        {///
            foreach (DateTime day in classRoomDays)
            {
                ClassroomDay classRoomDay = new ClassroomDay(day);
                //classRoomDay.setMappings(dir + "//" + Utilities.getDateDashStr(day) + "//MAPPINGS//MAPPING_" + className + ".CSV", personBaseMappings, mapById, startHour, endHour, endMinute);
                classRoomDay.setMappings(dir, className, personBaseMappings, mapById, startHour, endHour, endMinute);

                //CLEAN UBI
                classRoomDay.mergeAndCleanExistingDenoised(dir, startHour, endHour);
            }

        }
        
        public void writePLSOnsets(TextWriter sw, String PLStype, String PLSLENA, String PLSDay, Person pi)
        {

            try
            {
                String newDiagnosis = "";
                int pos = 0;
                foreach (String d in pi.diagnosisList)
                {
                    newDiagnosis +=  (pos>0?","+d: d);
                    pos++;
                }
                String newLanguages = ""; 
                pos = 0;
                foreach (String l in pi.languagesList)
                {
                    newLanguages += (pos > 0 ? "," + l : l);
                    pos++;
                }


                String[] files = Directory.GetFiles(dir + "//PLS//", "*.its", SearchOption.AllDirectories);
                DateTime classDay= Convert.ToDateTime(PLSDay);

                foreach (string itsFile in files)
                {
                    String szLenaId = Utilities.getLenaIdFromFileName(Path.GetFileName(itsFile));
                    if (szLenaId == PLSLENA)
                    { 
                        XmlDocument doc = new XmlDocument();
                    doc.Load(itsFile);
                    XmlNodeList rec = doc.ChildNodes[0].SelectNodes("ProcessingUnit/Recording");
                    double convId = 0;
                    foreach (XmlNode recording in rec)
                    {

                        double recStartSecs = Convert.ToDouble(recording.Attributes["startTime"].Value.Substring(2, recording.Attributes["startTime"].Value.Length - 3));
                        DateTime recStartTime = DateTime.Parse(recording.Attributes["startClockTime"].Value);
                        var est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                        recStartTime = TimeZoneInfo.ConvertTime(recStartTime, est);

                        XmlNodeList nodes = recording.SelectNodes("Conversation|Pause");
                        
                        //if (pdi.mapId != "")
                        {
                             
                            if (Utilities.isSameDay(recStartTime, classDay) &&
                                    recStartTime.Hour >= startHour &&
                                    (recStartTime.Hour < endHour ||
                                        (recStartTime.Hour == endHour &&
                                            recStartTime.Minute <= endMinute
                                        )
                                    )
                                )
                            {

                                foreach (XmlNode conv in nodes)
                                {
                                    convId++;
                                    XmlNodeList segments = conv.SelectNodes("Segment");
                                    double startSecs = Convert.ToDouble(conv.Attributes["startTime"].Value.Substring(2, conv.Attributes["startTime"].Value.Length - 3)) - recStartSecs;
                                    double endSecs = Convert.ToDouble(conv.Attributes["endTime"].Value.Substring(2, conv.Attributes["endTime"].Value.Length - 3)) - recStartSecs;
                                    DateTime start = Utilities.geFullTime(recStartTime.AddSeconds(startSecs));
                                    DateTime end = Utilities.geFullTime(recStartTime.AddSeconds(endSecs));
                                    double dbAvg = Convert.ToDouble(conv.Attributes["average_dB"].Value);
                                    double dbPeak = Convert.ToDouble(conv.Attributes["peak_dB"].Value);
                                    double bdSecs = (end - start).Seconds;
                                    double bdMilliseconds = (end - start).Milliseconds > 0 ? ((end - start).Milliseconds / 1000.00) : 0.00;
                                    double bd = bdSecs + bdMilliseconds;
                                     
                                    if (Utilities.isSameDay(start, classDay))
                                    {
                                        if (conv.Name == "Conversation")
                                        {
                                            double tc = Convert.ToDouble(conv.Attributes["turnTaking"].Value); ;

                                           
                                            sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                            classDay + "," +
                                                                            pi.shortId + "," +
                                                                            PLSLENA + "," +
                                                                            pi.subjectType + "," +
                                                                            convId +
                                                                            ",Conversation_turnTaking," +
                                                                            Utilities.getTimeStr(recStartTime) + "," +
                                                                            startSecs + "," +
                                                                            endSecs + "," +
                                                                            Utilities.getTimeStr(start) + "," +
                                                                            Utilities.getTimeStr(end) + "," +
                                                                            String.Format("{0:0.00}", 0) + "," +
                                                                            String.Format("{0:0.00}", bd) + "," +
                                                                            "," +
                                                                            String.Format("{0:0.00}", dbAvg) + "," +
                                                                            String.Format("{0:0.00}", dbPeak) + "," +
                                                                            String.Format("{0:0.00}", tc)+","+ PLStype+","+newDiagnosis+","+newLanguages+","+pi.gender+","+pi.dob);
                                             
                                        }


                                        foreach (XmlNode seg in segments)
                                        {

                                            startSecs = Convert.ToDouble(seg.Attributes["startTime"].Value.Substring(2, seg.Attributes["startTime"].Value.Length - 3)) - recStartSecs;
                                            endSecs = Convert.ToDouble(seg.Attributes["endTime"].Value.Substring(2, seg.Attributes["endTime"].Value.Length - 3)) - recStartSecs;
                                            start = Utilities.geFullTime(recStartTime.AddMilliseconds(startSecs * 1000));
                                            end = Utilities.geFullTime(recStartTime.AddMilliseconds(endSecs * 1000));


                                            bd = (end - start).Seconds + ((end - start).Milliseconds > 0 ? ((end - start).Milliseconds / 1000.00) : 0); //endSecs - startSecs;
                                            dbAvg = Convert.ToDouble(seg.Attributes["average_dB"].Value);
                                            dbPeak = Convert.ToDouble(seg.Attributes["peak_dB"].Value);
                                            String speaker = seg.Attributes["spkr"].Value;
                                                 

                                            switch (speaker)
                                            {
                                                case "CHN":
                                                case "CHF":
                                                     
                                                    double pivd = Convert.ToDouble(seg.Attributes["childUttLen"].Value.Substring(1, seg.Attributes["childUttLen"].Value.Length - 2));
                                                    double pivc = Convert.ToDouble(seg.Attributes["childUttCnt"].Value);
                                                    sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                            classDay + "," +
                                                                            pi.shortId + "," +
                                                                            PLSLENA + "," +
                                                                            pi.subjectType + "," +
                                                                            convId +
                                                                            ",CHN_CHF SegmentUttCount," +
                                                                            Utilities.getTimeStr(recStartTime) + "," +
                                                                            startSecs + "," +
                                                                            endSecs + "," +
                                                                            Utilities.getTimeStr(start) + "," +
                                                                            Utilities.getTimeStr(end) + "," +
                                                                            String.Format("{0:0.00}", pivd) + "," +
                                                                            String.Format("{0:0.00}", bd) + "," +
                                                                            "," +
                                                                            String.Format("{0:0.00}", dbAvg) + "," +
                                                                            String.Format("{0:0.00}", dbPeak) + ",,"+ PLStype+","+newDiagnosis+","+newLanguages+","+pi.gender+","+pi.dob);

                                                  
                                                    foreach (XmlAttribute atts in seg.Attributes)
                                                    {
                                                        if (atts.Name.IndexOf("startCry") == 0)
                                                        {
                                                            String attStep = atts.Name.Substring(8);
                                                            String att = atts.Name;
                                                            double astartSecs = Convert.ToDouble(seg.Attributes[att].Value.Substring(2, seg.Attributes[att].Value.Length - 3)) - recStartSecs;
                                                            double aendSecs = Convert.ToDouble(seg.Attributes["endCry" + attStep].Value.Substring(2, seg.Attributes["endCry" + attStep].Value.Length - 3)) - recStartSecs;
                                                            DateTime astart = Utilities.geFullTime(recStartTime.AddMilliseconds(astartSecs * 1000));
                                                            DateTime aend = Utilities.geFullTime(recStartTime.AddMilliseconds(aendSecs * 1000));
                                                            double apicry = (aend - astart).Seconds + ((aend - astart).Milliseconds > 0 ? (aend - astart).Milliseconds / 1000.00 : 0); //cendSecs - cstartSecs;

                                                            sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                        classDay + "," +
                                                                        pi.shortId + "," +
                                                                        PLSLENA + "," +
                                                                        pi.subjectType + "," +
                                                                        convId +
                                                                        ",CHN_CHF CryDur," +
                                                                        Utilities.getTimeStr(recStartTime) + "," +
                                                                        astartSecs + "," +
                                                                        aendSecs + "," +
                                                                        Utilities.getTimeStr(astart) + "," +
                                                                        Utilities.getTimeStr(aend) + "," +
                                                                        String.Format("{0:0.00}", apicry) + "," +
                                                                        String.Format("{0:0.00}", bd) + "," +
                                                                        "," +
                                                                        "," +
                                                                        "," + ","+ PLStype+","+newDiagnosis+","+newLanguages+","+pi.gender+","+pi.dob); //String.Format("{0:0.00}", dbPeak) + ",");

                                                                 
                                                        }
                                                        else if (atts.Name.IndexOf("startUtt") == 0)
                                                        {
                                                            String attStep = atts.Name.Substring(8);
                                                            String att = atts.Name;
                                                            double astartSecs = Convert.ToDouble(seg.Attributes[att].Value.Substring(2, seg.Attributes[att].Value.Length - 3)) - recStartSecs;
                                                            double aendSecs = Convert.ToDouble(seg.Attributes["endUtt" + attStep].Value.Substring(2, seg.Attributes["endUtt" + attStep].Value.Length - 3)) - recStartSecs;
                                                            DateTime astart = Utilities.geFullTime(recStartTime.AddMilliseconds(astartSecs * 1000));
                                                            DateTime aend = Utilities.geFullTime(recStartTime.AddMilliseconds(aendSecs * 1000));
                                                            double apiutts = (aend - astart).Seconds + ((aend - astart).Milliseconds > 0 ? (aend - astart).Milliseconds / 1000.00 : 0); //cendSecs - cstartSecs;
                                                            sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                        classDay + "," +
                                                                        pi.shortId + "," +
                                                                        PLSLENA + "," +
                                                                        pi.subjectType + "," +
                                                                        convId +
                                                                        ",CHN_CHF UttDur," +
                                                                        Utilities.getTimeStr(recStartTime) + "," +
                                                                        astartSecs + "," +
                                                                        aendSecs + "," +
                                                                        Utilities.getTimeStr(astart) + "," +
                                                                        Utilities.getTimeStr(aend) + "," +
                                                                        String.Format("{0:0.00}", apiutts) + "," +
                                                                        String.Format("{0:0.00}", bd) + "," +
                                                                        "," +
                                                                        "," +
                                                                        "," + ","+ PLStype+","+newDiagnosis+","+newLanguages+","+pi.gender+","+pi.dob);   //String.Format("{0:0.00}", dbPeak) + ",");

                                                            
                                                        }
                                                    }
                                                    break;
                                                case "FAN":
                                                case "MAN":
                                                    Boolean isFemale = speaker == "FAN";
                                                    double piac = isFemale ? Convert.ToDouble(seg.Attributes["femaleAdultWordCnt"].Value) : Convert.ToDouble(seg.Attributes["maleAdultWordCnt"].Value);
                                                    double piad = isFemale ? Convert.ToDouble(seg.Attributes["femaleAdultUttLen"].Value.Substring(1, seg.Attributes["femaleAdultUttLen"].Value.Length - 2)) : Convert.ToDouble(seg.Attributes["maleAdultUttLen"].Value.Substring(1, seg.Attributes["maleAdultUttLen"].Value.Length - 2));
                                                    piad = bd;
                                                    sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                        classDay + "," +
                                                                        pi.shortId + "," +
                                                                        PLSLENA + "," +
                                                                        pi.subjectType + "," +
                                                                        convId +
                                                                        (isFemale ? ",FAN SegmentUtt," : ",MAN SegmentUtt,") +
                                                                        Utilities.getTimeStr(recStartTime) + "," +
                                                                        startSecs + "," +
                                                                        endSecs + "," +
                                                                        Utilities.getTimeStr(start) + "," +
                                                                        Utilities.getTimeStr(end) + "," +
                                                                        String.Format("{0:0.00}", piad) + "," +
                                                                        String.Format("{0:0.00}", bd) + "," +
                                                                        String.Format("{0:0.00}", piac) + "," +
                                                                        String.Format("{0:0.00}", dbAvg) + "," +
                                                                        String.Format("{0:0.00}", dbPeak) + "," + ","+ PLStype+","+newDiagnosis+","+newLanguages+","+pi.gender+","+pi.dob);

                                                  

                                                    break;


                                                case "CXN":
                                                case "CXF":
                                                    sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                         classDay + "," +
                                                                         pi.shortId + "," +
                                                                         PLSLENA + "," +
                                                                         pi.subjectType + "," +
                                                                         convId +
                                                                         ",CXN_CXF SegmentUttDur," +
                                                                         Utilities.getTimeStr(recStartTime) + "," +
                                                                         startSecs + "," +
                                                                         endSecs + "," +
                                                                         Utilities.getTimeStr(start) + "," +
                                                                         Utilities.getTimeStr(end) + "," +
                                                                         String.Format("{0:0.00}", 0) + "," +
                                                                         String.Format("{0:0.00}", bd) + "," +
                                                                         "0.00," +
                                                                         String.Format("{0:0.00}", dbAvg) + "," +
                                                                         String.Format("{0:0.00}", dbPeak) + "," + ","+ PLStype+","+newDiagnosis+","+newLanguages+","+pi.gender+","+pi.dob);

                                                   
                                                    break;
                                                case "OLN":
                                                    sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                        classDay + "," +
                                                                        pi.shortId + "," +
                                                                        PLSLENA + "," +
                                                                        pi.subjectType + "," +
                                                                        convId +
                                                                        ",OLN Dur," +
                                                                        Utilities.getTimeStr(recStartTime) + "," +
                                                                        startSecs + "," +
                                                                        endSecs + "," +
                                                                        Utilities.getTimeStr(start) + "," +
                                                                        Utilities.getTimeStr(end) + "," +
                                                                        String.Format("{0:0.00}", 0) + "," +
                                                                        String.Format("{0:0.00}", bd) + "," +
                                                                        "0.00," +
                                                                        String.Format("{0:0.00}", dbAvg) + "," +
                                                                        String.Format("{0:0.00}", dbPeak) + "," + ","+ PLStype+","+newDiagnosis+","+newLanguages+","+pi.gender+","+pi.dob);
 

                                                    break;
                                                case "NON":
                                                    sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                       classDay + "," +
                                                                       pi.shortId + "," +
                                                                       PLSLENA + "," +
                                                                       pi.subjectType + "," +
                                                                       convId +
                                                                       ",NON Dur," +
                                                                       Utilities.getTimeStr(recStartTime) + "," +
                                                                       startSecs + "," +
                                                                       endSecs + "," +
                                                                       Utilities.getTimeStr(start) + "," +
                                                                       Utilities.getTimeStr(end) + "," +
                                                                       String.Format("{0:0.00}", 0) + "," +
                                                                       String.Format("{0:0.00}", bd) + "," +
                                                                       "0.00," +
                                                                       String.Format("{0:0.00}", dbAvg) + "," +
                                                                       String.Format("{0:0.00}", dbPeak) + "," + ","+ PLStype+","+newDiagnosis+","+newLanguages+","+pi.gender+","+pi.dob);

                                                    
                                                    break;

                                                default:
                                                    sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                       classDay + "," +
                                                                       pi.shortId + "," +
                                                                       PLSLENA + "," +
                                                                       pi.subjectType + "," +
                                                                       convId + "," +
                                                                       speaker + "," +
                                                                       Utilities.getTimeStr(recStartTime) + "," +
                                                                       startSecs + "," +
                                                                       endSecs + "," +
                                                                       Utilities.getTimeStr(start) + "," +
                                                                       Utilities.getTimeStr(end) + "," +
                                                                       String.Format("{0:0.00}", bd) + "," +
                                                                       String.Format("{0:0.00}", bd) + "," +
                                                                       String.Format("{0:0.00}", 0) + "," +
                                                                       String.Format("{0:0.00}", dbAvg) + "," +
                                                                       String.Format("{0:0.00}", dbPeak) + "," + ","+ PLStype+","+newDiagnosis+","+newLanguages+","+pi.gender+","+pi.dob);
                                                        break;


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
            catch (Exception e)
            {


            }
             
        }
        public void processOutsideLenas()
        {
            String szInputFile = dir + "//PLAYGROUNDTIMES.csv";
            String szOnsetOutputFile = dir + "//PLAYGROUNDLENASUM_" + Utilities.szVersion + ".CSV";
            TextWriter sw = new StreamWriter(szOnsetOutputFile);
            
            sw.WriteLine("ChildKey,ID,RecordingDate,VestOn,Playground Start Time,Playground End Time,Vest Off/Ubi End,context,AWC_AM,AWC_PG,AWC_NOON,CT_AM,CT_PG,CT_NOON,CV_AM,CV_PG,CV_NOON,AWC,CT,CV");



            Dictionary<String, Dictionary<String, List<String[]>>> dayLines = new Dictionary<string, Dictionary<string, List<string[]>>>();
            if (File.Exists(szInputFile))
                using (StreamReader sr = new StreamReader(szInputFile))
                {
                    if (!sr.EndOfStream)
                    {
                        String commaLine = sr.ReadLine();
                        String[] line = commaLine.Split(',');
                        while ((!sr.EndOfStream)) 
                        {
                            commaLine = sr.ReadLine();
                            line = commaLine.Split(',');
                            if (line.Length > 5 && line[1] != "")
                            {
                                String szd = Utilities.getDateDashStr(Convert.ToDateTime( line[2].Trim()));
                                String sid = line[1].Trim().ToUpper();
                                if (!dayLines.ContainsKey(szd))
                                {
                                    dayLines.Add(szd, new Dictionary<string, List<string[]>>());
                                }
                                if (!dayLines[szd].ContainsKey(sid))
                                {
                                    dayLines[szd].Add(sid, new List<string[]>());
                                }
                                dayLines[szd][sid].Add(line);
                            }
                        }
                    }
                }
            foreach (DateTime day in classRoomDays)
            {
                ClassroomDay classRoomDay = new ClassroomDay(day);
                //classRoomDay.setMappings(dir + "//" + Utilities.getDateDashStr(day) + "//MAPPINGS//MAPPING_" + className + ".CSV", personBaseMappings, mapById, startHour, endHour, endMinute);
                classRoomDay.setMappings(dir, className, personBaseMappings, mapById, startHour, endHour, endMinute);


                String szDayFolder = Utilities.getDateDashStr(day);

                if (dayLines.ContainsKey(szDayFolder))
                {

                    foreach (String sid in dayLines[szDayFolder].Keys)
                    {
                        string[] szDayLenaItsFiles = Directory.GetFiles(dir + "//" + szDayFolder + "//LENA_Data//ITS//", "*.its");



                        foreach (string itsFile in szDayLenaItsFiles)
                        {
                            String szLenaId = Utilities.getLenaIdFromFileName(Path.GetFileName(itsFile));

                            PersonDayInfo pdi = new PersonDayInfo();

                            foreach (PersonDayInfo pd in classRoomDay.personDayMappings.Values)
                            {
                                if (pd.lenaId == szLenaId && pd.present && pd.status == "PRESENT")
                                {
                                    pdi = pd;
                                    break;
                                }
                            }

                            if (pdi.mapId != "")
                            {
                                Person pi = personBaseMappings[pdi.mapId];

                                foreach (String[] line in dayLines[szDayFolder][sid])
                                {
                                    String context = line[7].Trim().ToUpper();
                                    DateTime vestOn = Convert.ToDateTime(line[3]);
                                    DateTime vestOff = Convert.ToDateTime(line[6]);
                                    DateTime pgOn = context == "FULL_PRESCHOOL_DAY" || (day.Month == 11 && day.Day == 29) || (day.Month == 1 && day.Day == 30) ? Convert.ToDateTime(line[4]) : vestOn;
                                    DateTime pgOff = context == "FULL_PRESCHOOL_DAY" ? Convert.ToDateTime(line[5]) : vestOff;
                                    vestOn = new DateTime(day.Year, day.Month, day.Day, vestOn.Hour, vestOn.Minute, vestOn.Second);
                                    vestOff = new DateTime(day.Year, day.Month, day.Day, vestOff.Hour, vestOff.Minute, vestOff.Second);

                                    pgOn = new DateTime(day.Year, day.Month, day.Day, pgOn.Hour, pgOn.Minute, pgOn.Second);
                                    pgOff = new DateTime(day.Year, day.Month, day.Day, pgOff.Hour, pgOff.Minute, pgOff.Second);

                                    double awc = 0;
                                    double awcAm = 0;
                                    double awcPg = 0;
                                    double awcNoon = 0;

                                    double ct = 0; 
                                    double ctAm = 0; ;
                                    double ctPg = 0;
                                    double ctNoon = 0;

                                    double cv = 0; 
                                    double cvAm = 0;
                                    double cvPg = 0;
                                    double cvNoon = 0;


                                    XmlDocument doc = new XmlDocument();
                                    doc.Load(itsFile);
                                    XmlNodeList rec = doc.ChildNodes[0].SelectNodes("ProcessingUnit/Recording");
                                    foreach (XmlNode recording in rec)
                                    {

                                        double recStartSecs = Convert.ToDouble(recording.Attributes["startTime"].Value.Substring(2, recording.Attributes["startTime"].Value.Length - 3));
                                        DateTime recStartTime = DateTime.Parse(recording.Attributes["startClockTime"].Value);
                                        var est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                                        recStartTime = TimeZoneInfo.ConvertTime(recStartTime, est);




                                        XmlNodeList nodes = recording.SelectNodes("Conversation|Pause");


                                        if (Utilities.isSameDay(recStartTime, day) &&
                                        vestOn.Hour >= startHour &&
                                        (vestOn.Hour < endHour ||
                                        (vestOn.Hour == endHour &&
                                        vestOn.Minute <= endMinute)))
                                        {


                                            foreach (XmlNode conv in nodes)
                                            {
                                                XmlNodeList segments = conv.SelectNodes("Segment");
                                                double startSecs = Convert.ToDouble(conv.Attributes["startTime"].Value.Substring(2, conv.Attributes["startTime"].Value.Length - 3)) - recStartSecs;
                                                double endSecs = Convert.ToDouble(conv.Attributes["endTime"].Value.Substring(2, conv.Attributes["endTime"].Value.Length - 3)) - recStartSecs;
                                                DateTime start = Utilities.geFullTime(recStartTime.AddSeconds(startSecs));
                                                DateTime end = Utilities.geFullTime(recStartTime.AddSeconds(endSecs));
                                                //double bdSecs = (end - start).Seconds;
                                                //double bdMilliseconds = (end - start).Milliseconds > 0 ? ((end - start).Milliseconds / 1000.00) : 0.00;
                                                //double bd = bdSecs + bdMilliseconds;

                                                if (Utilities.isSameDay(start, day) &&
                                                start.Hour >= startHour &&
                                                (start.Hour < endHour || (start.Hour == endHour && start.Minute <= endMinute)) &&
                                                start >= vestOn &&
                                                start <= vestOff)
                                                {
                                                    if (conv.Name == "Conversation")
                                                    {
                                                        double tc = Convert.ToDouble(conv.Attributes["turnTaking"].Value); ;

                                                        if (tc > 0)
                                                        {
                                                            if (context == "FULL_PRESCHOOL_DAY")
                                                            {
                                                                if (start < pgOn)
                                                                {
                                                                    ctAm += tc;

                                                                }
                                                                if (start >= pgOff)
                                                                {
                                                                    ctNoon += tc;
                                                                }
                                                            }
                                                            if (start >= pgOn && start < pgOff)
                                                            {
                                                                ctPg += tc;
                                                            }
                                                            ct += tc;
                                                        }
                                                    }
                                                }
                                                foreach (XmlNode seg in segments)
                                                {

                                                    startSecs = Convert.ToDouble(seg.Attributes["startTime"].Value.Substring(2, seg.Attributes["startTime"].Value.Length - 3)) - recStartSecs;
                                                    endSecs = Convert.ToDouble(seg.Attributes["endTime"].Value.Substring(2, seg.Attributes["endTime"].Value.Length - 3)) - recStartSecs;
                                                    start = Utilities.geFullTime(recStartTime.AddMilliseconds(startSecs * 1000));
                                                    end = Utilities.geFullTime(recStartTime.AddMilliseconds(endSecs * 1000));


                                                    //bd = (end - start).Seconds + ((end - start).Milliseconds > 0 ? ((end - start).Milliseconds / 1000.00) : 0); //endSecs - startSecs;
                                                    //dbAvg = Convert.ToDouble(seg.Attributes["average_dB"].Value);
                                                    //dbPeak = Convert.ToDouble(seg.Attributes["peak_dB"].Value);
                                                    String speaker = seg.Attributes["spkr"].Value;
                                                    switch (speaker)
                                                    {
                                                        case "CHN":
                                                        case "CHF":
                                                            double pivd = Convert.ToDouble(seg.Attributes["childUttLen"].Value.Substring(1, seg.Attributes["childUttLen"].Value.Length - 2));
                                                            double pivc = Convert.ToDouble(seg.Attributes["childUttCnt"].Value);
                                                            if (context == "FULL_PRESCHOOL_DAY")
                                                            {
                                                                if (start < pgOn)
                                                                {
                                                                    cvAm += pivc;

                                                                }
                                                                if (start >= pgOff)
                                                                {
                                                                    cvNoon += pivc;
                                                                }
                                                            }
                                                            if (start >= pgOn && start < pgOff)
                                                            {
                                                                cvPg += pivc;
                                                            }
                                                            cv += pivc;
                                                            break;
                                                        case "FAN":
                                                        case "MAN":
                                                            Boolean isFemale = speaker == "FAN";
                                                            double piac = isFemale ? Convert.ToDouble(seg.Attributes["femaleAdultWordCnt"].Value) : Convert.ToDouble(seg.Attributes["maleAdultWordCnt"].Value);
                                                            double piad = isFemale ? Convert.ToDouble(seg.Attributes["femaleAdultUttLen"].Value.Substring(1, seg.Attributes["femaleAdultUttLen"].Value.Length - 2)) : Convert.ToDouble(seg.Attributes["maleAdultUttLen"].Value.Substring(1, seg.Attributes["maleAdultUttLen"].Value.Length - 2));
                                                            if (context == "FULL_PRESCHOOL_DAY")
                                                            {
                                                                if (start < pgOn)
                                                                {
                                                                    awcAm += piac;

                                                                }
                                                                if (start >= pgOff)
                                                                {
                                                                    awcNoon += piac;
                                                                }
                                                            }
                                                            if (start >= pgOn && start < pgOff)
                                                            {
                                                                awcPg += piac;
                                                            }
                                                            awc += piac;

                                                            //piad = bd;
                                                            break;
                                                    }

                                                }


                                            }


                                        }
                                    }

                                    //WRITE SUM
                                    sw.WriteLine( String.Join(",", line) + "," +
                                        (context == "FULL_PRESCHOOL_DAY" ? awcAm.ToString() : "NA") + "," +
                                        awcPg.ToString() + "," +
                                        (context == "FULL_PRESCHOOL_DAY" ? awcNoon.ToString() : "NA") + "," +
                                        (context == "FULL_PRESCHOOL_DAY" ? ctAm.ToString() : "NA") + "," +
                                        ctPg.ToString() + "," +
                                        (context == "FULL_PRESCHOOL_DAY" ? ctNoon.ToString() : "NA") + "," +
                                        (context == "FULL_PRESCHOOL_DAY" ? cvAm.ToString() : "NA") + "," +
                                        cvPg.ToString() + "," +
                                        (context == "FULL_PRESCHOOL_DAY" ? cvNoon.ToString() : "NA") + ","+
                                        awc.ToString()+","+ct.ToString()+","+cv.ToString());
                                    //"ChildKey,ID,RecordingDate,VestOn,Playground Start Time,Playground End Time,Vest Off/Ubi End,context,AWC_AM,AWC,PG,AWC_NOON,CT_AM,CT_PG,CT_NOON,CV_AM,CV_PG,CV_NOON");




                                }
                            }


                        }

                    }





                }
            }
            sw.Close();
        }
        public void processPLSs()
        {

            //ONSETS
            String szOnsetOutputFile = dir + "//PLSONSETS_" + "_" + Utilities.szVersion + ".CSV";
            TextWriter sw= new StreamWriter(szOnsetOutputFile);

            String newDiagnosis = "";
            foreach (String d in diagnosisList)
            {
                newDiagnosis += ( d +  ",");
            }
            String newLanguages = "";
            foreach (String l in languagesList)
            {
                newLanguages += ( l +   ",");
            }


            sw.WriteLine("File,Date,Subject,LenaID,SubjectType,ConversationId," +
               "voctype,recstart,startsec,endsec,starttime,endtime,duration," +
               "seg_duration,wordcount,avg_db,avg_peak,turn_taking," +
               "PLS_Type,"+newDiagnosis+newLanguages+"Sex,DOB");//,children,teachers");//add subj info
             
               
 
            foreach (Person pi in personBaseMappings.Values)
            {
                String[] preLENAS = pi.prePLSLENA.Split('|');
                String[] postLENAS = pi.postPLSLENA.Split('|');
                String[] preDates = pi.prePLSDay.Split('|');
                String[] postDates = pi.postPLSDay.Split('|');
                int p = 0;
                foreach (String l in preLENAS)
                {
                    writePLSOnsets(sw, "PRE", l, preDates.Length > 0 ? preDates[p] : preDates[0],pi);

                }
                p = 0;
                foreach (String l in postLENAS)
                {
                    writePLSOnsets(sw, "POST", l, postDates.Length > 0 ? postDates[p] : postDates[0],pi);

                }
            }
            sw.Close();

        }
        public void processUbi(Boolean doTenths)
        {

            /*4.1 For each Collection Day process daily files*/
            foreach (DateTime day in classRoomDays)
            {
                ClassroomDay classRoomDay = new ClassroomDay(day);
                //classRoomDay.setMappings(dir + "//" + Utilities.getDateDashStr(day) + "//MAPPINGS//MAPPING_" + className + ".CSV", personBaseMappings, mapById, startHour, endHour, endMinute);
                classRoomDay.setMappings(dir, mapPrefix, personBaseMappings, mapById, startHour, endHour, endMinute);

                //GR
                String sGrOutputFile = dir + "//SYNC//"+( doTenths?"": "UBI") +"GR//DAYUBIGR_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
                if (doTenths)
                {
                    sGrOutputFile=sGrOutputFile.Replace(".CSV", "10TH.CSV");
                    classRoomDay.getTenthsFromUbi(dir, sGrOutputFile);
                }  
                else
                classRoomDay.makeGofRFilesAndTimeDictFromUbi(dir, sGrOutputFile);//
            }
        }
        public void processUbiInterpolation()
        {

            /*4.1 For each Collection Day process daily files*/
            foreach (DateTime day in classRoomDays)
            {
                ClassroomDay classRoomDay = new ClassroomDay(day);
                classRoomDay.setMappings(dir, className, personBaseMappings, mapById, startHour, endHour, endMinute);

                if (!Directory.Exists(dir + "//SYNC//INTERPOLATION"))
                    Directory.CreateDirectory(dir + "//SYNC//INTERPOLATION");

                String sGrOutputFile = dir + "//SYNC//INTERPOLATION//INTERPOLATION" +"_" + Utilities.szVersion + ".CSV";
                if(!File.Exists( sGrOutputFile))
                {
                    TextWriter sw= new StreamWriter(sGrOutputFile);
                    sw.WriteLine("SUBJECTID,SHORTID,DATE,INTERPOLATEDTENTHSECS,TOTALTENTHSSECS");
                    sw.Close();
                }
                 


                classRoomDay.getTenthsFromUbi(dir, sGrOutputFile,false);
               
            }
        }
        public void processAlice(Boolean justCleanAlice)
        {

            /*4.1 For each Collection Day process daily files*/
 
            Boolean generalAliceFileCleaned = false;
            foreach (DateTime day in classRoomDays)
            {
                ClassroomDay classRoomDay = new ClassroomDay(day);
                classRoomDay.setMappings(dir, mapPrefix, personBaseMappings, mapById, startHour, endHour, endMinute);
                 
                
                String szOnsetOutputFile = dir + "//SYNC//DAYONSETS_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
                Dictionary<String, Tuple<String, DateTime>> lenaStartTimes = classRoomDay.readLenaItsAndGetOnsets(dir, szOnsetOutputFile, startHour, endHour, endMinute);//takes only mapping start-end
                //classRoomDay.lenaOnsets = new Dictionary<string, List<LenaOnset>>();
                classRoomDay.readAliceAndGetOnsets(dir, startHour, endHour, endMinute, lenaStartTimes,ref generalAliceFileCleaned,  justCleanAlice);
                //GR


                if (!justCleanAlice)
                {
                    String sGrOutputFile = dir + "//SYNC//GR//DAYGR_TYPE_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
                    //classRoomDay.makeGofRFilesAndTimeDict(dir, sGrOutputFile, this.diagnosisList, false);
                    classRoomDay.getTenthsFromUbi(dir, sGrOutputFile);

                    classRoomDay.setTenthOfSecLENA();// ALICE();

                    Dictionary<String, Pair> pairs = classRoomDay.countInteractions(lenaStartTimes, this.grMin, this.grMax, this.angle, "", ""); //; //count interactions but no need to write a file

                    //*PAIRACTIVITY REPORT*/
                    String szPairActOutputFile = dir + "//SYNC//ALICE_PAIRACTIVITY//ALICE_PAIRACTIVITY_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
                    classRoomDay.writePairActivityData(pairs, className, szPairActOutputFile, this.diagnosisList, this.languagesList);
                    filesToMerge["ALICE_PAIRACTIVITIES"].Add(szPairActOutputFile);
                }

            }
        }
        public void process(Boolean all,Boolean tenSecs)
        {
            makeDayReportLists();

            TextWriter sw = new StreamWriter("testtimes.csv",false);
            sw.WriteLine("ID,DATE,SECONDS,FROM,FROMMS,TO,TOMS");
            sw.Close();

            /*4.1 For each Collection Day process daily files*/
            foreach (DateTime day in classRoomDays)
            {
                ClassroomDay classRoomDay = new ClassroomDay(day);
                //classRoomDay.setMappings(dir + "//" + Utilities.getDateDashStr(day) + "//MAPPINGS//MAPPING_" + className + ".CSV", personBaseMappings, mapById, startHour, endHour, endMinute);
                classRoomDay.setMappings(dir, mapPrefix, personBaseMappings, mapById, startHour, endHour, endMinute);
                //if (className == "StarFish_2223")
                //    classRoomDay.toFilter = true;
                //ONSETS
                String szOnsetOutputFile = dir + "//SYNC//ONSETS//DAYONSETS_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
                Dictionary<String, Tuple<String, DateTime>> lenaStartTimes = classRoomDay.readLenaItsAndGetOnsets(dir, szOnsetOutputFile, startHour, endHour, endMinute);//takes only mapping start-end
                filesToMerge["ONSETS"].Add(szOnsetOutputFile);
                 //
                //GR
                String sGrOutputFile = dir + "//SYNC//GR//DAYGR_TYPE_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
                classRoomDay.makeGofRFilesAndTimeDict(dir, sGrOutputFile, this.diagnosisList);

                String szTenthOutputFile = dir + "//SYNC//COTALK//DAYCOTALK_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";


                if (all || tenSecs)
                {

                  //  classRoomDay.setTenthOfSecALICE(dir, className, lenaStartTimes);

                    classRoomDay.setTenthOfSecLENA();
                    szTenthOutputFile = dir + "//SYNC//COTALK//DAYCOTALK_" + Utilities.getDateStrMMDDYY(day) + "_V" + Utilities.szVersion + ".CSV";
                    classRoomDay.writeTenthOfSec(szTenthOutputFile);

                     
                }
                if ( all)
                {
                   
                    //Date	Subject	Partner	SubjectShortID	PartnerShortID	SubjectDiagnosis	PartnerDiagnosis	SubjectGender	PartnerGender	SubjectLanguage	PartnerLanguage	Adult	SubjectStatus	PartnerStatus	SubjectType	PartnerType	Input1_pvc_or_sac	Input2_pvc_or_stc	Input3_dur_pvd_or_uttl	PairBlockTalking	PairTalkingDuration	Subject-Talking-Duration-From_Start	Partner-Talking-Duration-From-Start	Subject-Talking-Duration-Evenly-Spread	Partner-Talking-Duration-Evenly-Spread	SubjectTurnCount	PartnerTurnCount	SubjectVocCount	PartnerVocCount	SubjectAdultCount	PartnerAdultCount	SubjectNoise	PartnerNoise	SubjectOLN	PartnerOLN	SubjectCry	PartnerCry	SubjectJoinedCry	PartnerJoinedCry	JoinedCry	PairProximityDuration	PairOrientation-ProximityDuration	SharedTimeinClassroom	SubjectTime	PartnerTime	TotalRecordingTime	WUBITotalVD	TotalVD	PartnerWUBITotalVD	PartnerTotalVD	WUBITotalVC	TotalVC	PartnerWUBITotalVC	PartnerTotalVC	WUBITotalTC	TotalTC	PartnerWUBITotalTC	PartnerTotalTC	WUBITotalAC	TotalAC	PartnerWUBITotalAC	PartnerTotalAC	WUBITotalNO	TotalNO	PartnerWUBITotalNO	PartnerTotalNO	WUBITotalOLN	TotalOLN	PartnerWUBITotalOLN	PartnerTotalOLN	WUBITotalCRY	TotalCRY	PartnerWUBITotalCRY	PartnerTotalCRY	WUBITotalAV_DB	TotalAV_DB	PartnerWUBITotalAV_DB	PartnerTotalAV_DB	WUBITotalAV_PEAK_DB	TotalAV_PEAK_DB	PartnerWUBITotalAV_PEAK_DB	PartnerTotalAV_PEAK_DB	Lead_Date	Lead_SubjectStatus	Lead_PartnerStatus	Lead_Input1_pvc_or_sac	Lead_Input2_pvc_or_stc	Lead_Input3_dur_pvd_or_uttl	Lead_PairBlockTalking	Lead_PairTalkingDuration	Lead_Subject-Talking-Duration-From_Start	Lead_Partner-Talking-Duration-From-Start	Lead_Subject-Talking-Duration-Evenly-Spread	Lead_Partner-Talking-Duration-Evenly-Spread	Lead_SubjectTurnCount	Lead_PartnerTurnCount	Lead_SubjectVocCount	Lead_PartnerVocCount	Lead_SubjectAdultCount	Lead_PartnerAdultCount	Lead_SubjectNoise	Lead_PartnerNoise	Lead_SubjectOLN	Lead_PartnerOLN	Lead_SubjectCry	Lead_PartnerCry	Lead_SubjectJoinedCry	Lead_PartnerJoinedCry	Lead_JoinedCry	Lead_PairProximityDuration	Lead_PairOrientation-ProximityDuration	Lead_SharedTimeinClassroom	Lead_SubjectTime	Lead_PartnerTime	Lead_TotalRecordingTime	Lead_WUBITotalVD	Lead_TotalVD	Lead_PartnerWUBITotalVD	Lead_PartnerTotalVD	Lead_WUBITotalVC	Lead_TotalVC	Lead_PartnerWUBITotalVC	Lead_PartnerTotalVC	Lead_WUBITotalTC	Lead_TotalTC	Lead_PartnerWUBITotalTC	Lead_PartnerTotalTC	Lead_WUBITotalAC	Lead_TotalAC	Lead_PartnerWUBITotalAC	Lead_PartnerTotalAC	Lead_WUBITotalNO	Lead_TotalNO	Lead_PartnerWUBITotalNO	Lead_PartnerTotalNO	Lead_WUBITotalOLN	Lead_TotalOLN	Lead_PartnerWUBITotalOLN	Lead_PartnerTotalOLN	Lead_WUBITotalCRY	Lead_TotalCRY	Lead_PartnerWUBITotalCRY	Lead_PartnerTotalCRY	Lead_WUBITotalAV_DB	Lead_TotalAV_DB	Lead_PartnerWUBITotalAV_DB	Lead_PartnerTotalAV_DB	Lead_WUBITotalAV_PEAK_DB	Lead_TotalAV_PEAK_DB	Lead_PartnerWUBITotalAV_PEAK_DB	Lead_PartnerTotalAV_PEAK_DB	Lead_CLASSROOM
                    //*INTERACTIONS*/
                    String szAngleOutputFile = dir + "//SYNC//PAIRANGLES//DAILY_ANGLES" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
                    String szAppOutputFile = dir + "//SYNC//APPROACH//DAILY_APP_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
                    Dictionary<String, Pair> pairs = classRoomDay.countInteractions(this.grMin, this.grMax,this.angle, szAngleOutputFile, szAppOutputFile); //; //count interactions but no need to write a file

                    //*PAIRACTIVITY REPORT*/
                    String szPairActOutputFile = dir + "//SYNC//PAIRACTIVITY//PAIRACTIVITY_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
                    classRoomDay.writePairActivityData(pairs, className, szPairActOutputFile, this.diagnosisList, this.languagesList);
                    //classRoomDay.writePairActivityDatawAlice(pairs, className, szPairActOutputFile, this.diagnosisList, this.languagesList);
                 //   classRoomDay.writePairActivityData(pairs, className, szPairActOutputFile.Replace(".","_ACT."), this.diagnosisList, this.languagesList, activityTypes);
                    
                    filesToMerge["PAIRACTIVITIES"].Add(szPairActOutputFile);

                    /*AAPROACH*/
                    //swa.WriteLine("Person 1, Person2, Interaction Time, Interaction Millisecond,d1,d2,approachMeters,x10,y10,x20,y20,x11,y11,x21,y21, WithinGR, WithinGRAnd" + angle + "deg, Angle1, Angle2,Type1, Type2, Gender1, Gender2, Diagnosis1, Diagnosis2,LongPerson 1, LongPerson2,  ");


                    //*SOCIALONSETS  REPORT*/
                    String szSocialOnsetputFile = dir + "//SYNC//SOCIALONSETS//DAYSOCIALONSETS_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
                    classRoomDay.writeSocialOnsetData( className, szSocialOnsetputFile, this.diagnosisList, this.languagesList);
                    

                    filesToMerge["SOCIALONSETS"].Add(szSocialOnsetputFile);
                }

            }

        }
          
        public void mergeDayFiles()
        {
            foreach (List<String> files in filesToMerge.Values)
            {
                TextWriter sw = null;
                String szNewFileName = "";

                try
                {
                    Boolean includeHeader = true;
                    
                    foreach (String szfile in files)
                    {
                        if (szNewFileName == "")
                        {
                            String szShortName = Path.GetFileName(szfile);
                            szNewFileName = szShortName.Substring(0, szShortName.IndexOf("_"));
                            szShortName = szShortName.Substring(szShortName.IndexOf("_") + 1);
                            szNewFileName += szShortName.Substring(szShortName.IndexOf("_"));
                            sw = new StreamWriter(szfile.Replace(Path.GetFileName(szfile), szNewFileName), true);
                        }

                        using (StreamReader sr = new StreamReader(szfile))
                        {
                            if ((!includeHeader) && (!sr.EndOfStream))
                            {
                                sr.ReadLine();
                            }

                            while ((!sr.EndOfStream))// && lineCount < 10000)
                            {
                                String commaLine = sr.ReadLine();
                                sw.WriteLine(commaLine);
                            }
                        }
                        includeHeader = false;

                    }
                    if (szNewFileName != "")
                    {
                        sw.Close();
                    }
                }
                catch(Exception e )
                {
                    if (szNewFileName != "")
                    {
                        sw.Close();
                    }
                }
            }

        }
    }
        
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace UL_Processor_V2023
{
    class Classroom
    {
        public Boolean ubiCleanup = false;
        public Boolean doRecInfo = true;
        public Boolean processData = true;
        public Boolean reDenoise = false;
        public String dir = "";
        public String className = "";
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
        

        public List<String> diagnosisList = new List<string>();
        public List<String> languagesList = new List<string>();
        int numCols = 0;
        public void getPairActLeadsFromFiles()
        {
            TextWriter sw = new StreamWriter(dir + "//SYNC//PAIRACTIVITY//PAIRACTIVITY_" + Utilities.szVersion + "ALL.CSV");
            int numOfDays = classRoomDays.Count;
            Dictionary<String, String> prevPairLines = new Dictionary<string, string>();
            Dictionary<String, String> pairLines = new Dictionary<string, string>();
           
            foreach (DateTime dayDate in classRoomDays)
            {
                pairLines = new Dictionary<string, string>();
                String[] szFiles = Directory.GetFiles(dir + "//SYNC//PAIRACTIVITY//");
                String fileDayPart = Utilities.getDateStr(dayDate, "", 2);
                String headerLine = "";

                foreach (String szFile in szFiles)
                {

                    if (szFile.Contains(fileDayPart) && szFile.Contains(Utilities.szVersion + "."))
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
                                sw.WriteLine(headerLine + "," + headerLine.Replace(",", ",Lead_"));
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
            String mappingBaseFileName = dir + "//MAPPING_" + className + "_BASE.CSV";
            List<int> dList = new List<int>();
            List<int> lList = new List<int>();
            if (File.Exists(mappingBaseFileName))
                using (StreamReader sr = new StreamReader(mappingBaseFileName))
                {
                    if (!sr.EndOfStream)
                    {//
                        String commaLine = sr.ReadLine();
                        String[] line = commaLine.Split(',');
                        int cp = 0;
                        foreach (String szCol in line)
                        {
                            if (szCol.ToUpper().Trim().Contains("DIAGNOSIS"))
                            {
                                diagnosisList.Add(szCol.Trim());
                                dList.Add(cp);
                            }
                            if (szCol.ToUpper().Trim().Contains("LANGUAGE"))
                            {
                                languagesList.Add(szCol.Trim());
                                lList.Add(cp);
                            }
                            cp++;

                        }

                    }

                    while ((!sr.EndOfStream))// && lineCount < 10000)
                    {
                        String commaLine = sr.ReadLine();
                        String[] line = commaLine.Split(',');
                        if (line.Length > 16 && line[1] != "")
                        {
                            Person person = new Person(commaLine, mapById, dList,lList);//longid

                            if (!personBaseMappings.ContainsKey(person.mapId))
                            {
                                personBaseMappings.Add(person.mapId, person);
                            }

                        }
                    }
                }
        }
        public void setDirs()
        {
            dir = dir + className;
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

        }
        public void makeDayReportLists()
        {
            filesToMerge.Add("ONSETS", new List<string>());
            filesToMerge.Add("ACTIVITIES", new List<string>());
            filesToMerge.Add("PAIRACTIVITIES", new List<string>());
            filesToMerge.Add("MINACTIVITIES", new List<string>());
            filesToMerge.Add("SOCIALONSETS", new List<string>());

        }
        public void clean()
        {
            foreach (DateTime day in classRoomDays)
            {
                ClassroomDay classRoomDay = new ClassroomDay(day);
                classRoomDay.setMappings(dir + "//" + Utilities.getDateDashStr(day) + "//MAPPINGS//MAPPING_" + className + ".CSV", personBaseMappings, mapById, startHour, endHour, endMinute);
 
                //CLEAN UBI
                classRoomDay.createCleanUbiFile(dir, startHour, endHour);
            }

       }
        public void denoise()
        {
            foreach (DateTime day in classRoomDays)
            {
                ClassroomDay classRoomDay = new ClassroomDay(day);
                classRoomDay.setMappings(dir + "//" + Utilities.getDateDashStr(day) + "//MAPPINGS//MAPPING_" + className + ".CSV", personBaseMappings, mapById, startHour, endHour, endMinute);

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
                classRoomDay.createDenoisedFile(dir, className);//, startHour, endHour);
            }

        }

        public void mergeAndCleanExistingDenoised()
        {///
            foreach (DateTime day in classRoomDays)
            {
                ClassroomDay classRoomDay = new ClassroomDay(day);
                classRoomDay.setMappings(dir + "//" + Utilities.getDateDashStr(day) + "//MAPPINGS//MAPPING_" + className + ".CSV", personBaseMappings, mapById, startHour, endHour, endMinute);

                //CLEAN UBI
                classRoomDay.mergeAndCleanExistingDenoised(dir, startHour, endHour);
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
                classRoomDay.setMappings(dir + "//" + Utilities.getDateDashStr(day) + "//MAPPINGS//MAPPING_" + className + ".CSV", personBaseMappings, mapById, startHour, endHour, endMinute);
                 
                //ONSETS
                String szOnsetOutputFile = dir + "//SYNC//ONSETS//DAYONSETS_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
                Dictionary<String, Tuple<String, DateTime>> lenaStartTimes = classRoomDay.readLenaItsAndGetOnsets(dir, szOnsetOutputFile, startHour, endHour, endMinute);//takes only mapping start-end
                filesToMerge["ONSETS"].Add(szOnsetOutputFile);
                 
                //GR
                String sGrOutputFile = dir + "//SYNC//GR//DAYGR_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
                classRoomDay.makeGofRFilesAndTimeDict(dir, sGrOutputFile);

                String szTenthOutputFile = dir + "//SYNC//COTALK//DAYCOTALK_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";


                if (all || tenSecs)
                {

                    classRoomDay.setTenthOfSecALICE(dir + "//BEEPONSETS_" + className + ".csv", lenaStartTimes);

                    classRoomDay.setTenthOfSecLENA();
                    szTenthOutputFile = dir + "//SYNC//COTALK//DAYCOTALK_" + Utilities.getDateStrMMDDYY(day) + "_" + Utilities.szVersion + ".CSV";
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

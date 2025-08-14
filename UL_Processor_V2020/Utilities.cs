using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using static IronPython.Modules._ast;
using System.Runtime.Remoting.Messaging;
using IronPython.Compiler;
using IronPython.Runtime;

namespace UL_Processor_V2023
{
    class Utilities
    {
        public static String szVersion = "";

        public static Boolean specialFilterOut(String szDate)
        {
            return specialFilterOut(Convert.ToDateTime(szDate));
        }
        public static void resetDiagnosisAndLanguages(Classroom cr)
        {
            resetDiagnosisAndLanguages( cr,  "SYNC");
        }
        public static void resetDiagnosisAndLanguages(Classroom cr, String syncDir)
        {
            resetDiagnosisAndLanguages(cr, syncDir, "GR");
            resetDiagnosisAndLanguages(cr, syncDir, "PAIRACTIVITY");
             
        }


        public static void resetDiagnosisAndLanguages(Classroom cr,String syncDir, String reportFolder)
        {


            if ( Directory.Exists(cr.dir + "//"+ syncDir))
            {
                String szDir = cr.dir + "//" + syncDir + "//" + reportFolder;
                if (Directory.Exists(szDir))
                {
                    //Date	Subject	Partner	SubjectShortID	PartnerShortID	SubjectDiagnosis Base_School	PartnerDiagnosis Base_School	SubjectSecondaryDiagnosis_Base_School	PartnerSecondaryDiagnosis_Base_School	SubjectDiagnosis_Parent	PartnerDiagnosis_Parent	SubjectSecondaryDiagnosis_Parent	PartnerSecondaryDiagnosis_Parent	SubjectUpdated Diagnosis after SFARI ADOS	PartnerUpdated Diagnosis after SFARI ADOS	SubjectHome Language_School	PartnerHome Language_School	SubjectHolistic Language_School	PartnerHolistic Language_School	SubjectHome Language_Parent	PartnerHome Language_Parent	SubjectHolistic Language_Parent	PartnerHolistic Language_Parent	SubjectPLS Testing Language	PartnerPLS Testing Language	SubjectGender	PartnerGender	Adult	SubjectStatus	PartnerStatus	SubjectType	PartnerType	Input1_pvc_or_sac	Input2_pvc_or_stc	Input3_dur_pvd_or_uttl	PairBlockTalking	PairTalkingDuration	Subject-Talking-Duration-Evenly-Spread	Partner-Talking-Duration-Evenly-Spread	SubjectTurnCount	PartnerTurnCount	SubjectVocCount	PartnerVocCount	SubjectAdultCount	PartnerAdultCount	SubjectNoise	PartnerNoise	SubjectOLN	PartnerOLN	SubjectCry	PartnerCry	SubjectJoinedCry	PartnerJoinedCry	JoinedCry	PairProximityDuration	PairOrientation-ProximityDuration	SharedTimeinClassroom	SubjectTime	PartnerTime	TotalRecordingTime	WUBITotalVD	TotalVD	PartnerWUBITotalVD	PartnerTotalVD	WUBITotalVC	TotalVC	PartnerWUBITotalVC	PartnerTotalVC	WUBITotalTC	TotalTC	PartnerWUBITotalTC	PartnerTotalTC	WUBITotalAC	TotalAC	PartnerWUBITotalAC	PartnerTotalAC	WUBITotalNO	TotalNO	PartnerWUBITotalNO	PartnerTotalNO	WUBITotalOLN	TotalOLN	PartnerWUBITotalOLN	PartnerTotalOLN	WUBITotalCRY	TotalCRY	PartnerWUBITotalCRY	PartnerTotalCRY	WUBITotalAV_DB	TotalAV_DB	PartnerWUBITotalAV_DB	PartnerTotalAV_DB	WUBITotalAV_PEAK_DB	TotalAV_PEAK_DB	PartnerWUBITotalAV_PEAK_DB	PartnerTotalAV_PEAK_DB	CLASSROOM

                    if (Directory.Exists(szDir + "_V2"))
                    {
                        Directory.Delete(szDir + "_V2",true);
                    }
                        Directory.CreateDirectory(szDir + "_V2"); 
                    
                    //foreach (String szReport in Directory.GetFiles(szDir))
                    {
                        //File.Move(szReport, szDir + "_OLD//"+Path.GetFileName(szReport));
                    }

                    foreach ( String szReport in Directory.GetFiles(szDir))//))
                    {
                        //String newReport = szReport.Replace(".", "_OLD.");
                        //File.Move(szReport, newReport);
                        if (szReport.ToUpper().EndsWith(".CSV"))
                        using (StreamReader sr = new StreamReader(szReport))
                        {
                            TextWriter sw = new StreamWriter(szDir + "_V2" + "//"+ Path.GetFileName(szReport));
                            String goodLine = "";
                            String newDiagnosis = "";
                            foreach (String d in cr.diagnosisList)
                            {
                                newDiagnosis += reportFolder != "GR" ? ("Subject" + d + ",Partner" + d + ","):(d+",");
                            }
                            String newLanguages = "";
                            foreach (String l in cr.languagesList)
                            {
                                newLanguages += reportFolder != "GR" ? ("Subject" + l + ",Partner" + l + ",") : (l + ",");
                            }

                            newDiagnosis = newDiagnosis == "SubjectDiagnosis,PartnerDiagnosis,SubjectLanguage,PartnerLanguage," ? "SubjectDiagnosis,PartnerDiagnosis," : newDiagnosis;
                            newLanguages = newLanguages == "" ? "SubjectLanguage,PartnerLanguage," : newLanguages;

                            List<int> badCols = new List<int>();
                            if (!sr.EndOfStream)
                            {
                                String szLine = sr.ReadLine();
                                String[] lineCols = szLine.Split(',');
                                int col = 0;
                                 
                                foreach(String title in lineCols)
                                {
                                    if(title.ToUpper().Contains("DIAGNOSIS") ||
                                        title.ToUpper().Contains("LANGUAGE"))
                                    {
                                        
                                        if(badCols.Count==0)
                                        {
                                            goodLine += (newDiagnosis + newLanguages);
                                        }
                                        
                                        
                                        badCols.Add(col);

                                    }
                                    else
                                    {
                                        goodLine += (title+",");

                                    }
                                    col++;
                                }

                                //Location	LRIC_BUBBLES_2324_1L	03:00.2	1.038706917	4.839401017	0.25
                                if (badCols.Count == 0 && reportFolder == "GR")
                                {
                                    goodLine="LOCATION,SUBJECTID,TIME,X,Y,Z,"+ (newDiagnosis + newLanguages);
                                    sw.WriteLine(goodLine);
                                    readAndWriteNewLine(szLine, ref sw, badCols, cr, reportFolder);
                                }
                                else
                                    sw.WriteLine(goodLine);
                            }

                             
                            /*Time,lx,ly,lz,rx,ry,rz,o,dis2d,cx,cy,cz,o_kf,lx_kf,ly_kf,rx_kf,ry_kf,dis2d_kf,cx_kf,cy_kf
                             chn_vocal   chf_vocal adult_vocal chn_vocal_average_dB chf_vocal_average_dB    adult_vocal_average_dB chn_vocal_peak_dB   chf_vocal_peak_dB adult_vocal_peak_dB

                            2022 - 01-28 08:58:00.200,1.3800916038677513,4.36540153126917,0.4596901265437715,0.9715147678818095,4.272437575392713,0.544450214826891,2.917870339559073,0.41901948402966077,1.1758031858747804,4.318919553330941,0.5020701706853312,2.917870339559072,1.3800916038677513,4.36540153126917,0.9715147678818095,4.272437575392712,0.41901948402966077,1.1758031858747804,4.318919553330941*/
                            //Location	LRIC_BUBBLES_2324_1L	03:00.2	1.038706917	4.839401017	0.25

                            while (!sr.EndOfStream)
                            {
                                String szLine = sr.ReadLine();
                                readAndWriteNewLine(szLine, ref sw, badCols, cr, reportFolder);
                            }

                            sw.Close();
                        }


                 }


                }
                 

            }
         /*       Directory.CreateDirectory(cr.dir + "//SYNC");
            if (!Directory.Exists(cr.dir + "//SYNC//ONSETS"))
                Directory.CreateDirectory(cr.dir + "//SYNC//ONSETS");
            if (!Directory.Exists(cr.dir + "//SYNC//SOCIALONSETS"))
                Directory.CreateDirectory(cr.dir + "//SYNC//SOCIALONSETS");
            if (!Directory.Exists(cr.dir + "//SYNC//GR"))
                Directory.CreateDirectory(cr.dir + "//SYNC//GR");
            if (!Directory.Exists(cr.dir + "//SYNC//COTALK"))
                Directory.CreateDirectory(cr.dir + "//SYNC//COTALK");
            if (!Directory.Exists(cr.dir + "//SYNC//PAIRACTIVITY"))
                Directory.CreateDirectory(cr.dir + "//SYNC//PAIRACTIVITY");

            if (!Directory.Exists(cr.dir + "//SYNC//PAIRANGLES"))
                Directory.CreateDirectory(cr.dir + "//SYNC//PAIRANGLES");

            if (!Directory.Exists(cr.dir + "//SYNC//APPROACH"))
                Directory.CreateDirectory(cr.dir + "//SYNC//APPROACH");

            */

        }
        public static void readAndWriteNewLine(String szLine,ref TextWriter sw, List<int> badCols, Classroom cr, String reportFolder)
        {
             
            String[] lineCols = szLine.Split(',');
            String goodLine = String.Join(",", lineCols, 0, badCols.Count>0?badCols[0]:lineCols.Length);
            String newPairDiagnosis = "";
            String newSubjectDiagnosis = "";
            int pos = 0;
            String szSubject = reportFolder!="GR"?lineCols[1]: lineCols[1].Substring(0, lineCols[1].Length-1);
            String szPartner = reportFolder != "GR" ? lineCols[2] : lineCols[1].Substring(0, lineCols[1].Length - 1);  
            Person subject = cr.personBaseMappings[szSubject];
            Person partner = cr.personBaseMappings[szSubject];

            foreach (String d in subject.diagnosisList)
            {
                newPairDiagnosis += (d + "," + (partner.diagnosisList.Count > pos ? partner.diagnosisList[pos] : "") + ",");
                newSubjectDiagnosis += (d + "," );
                pos++;
            }
            String newPairLanguages = "";
            String newSubjectLanguages = "";
            pos = 0;
            foreach (String l in subject.languagesList)
            {
                newPairLanguages += (l + "," + (partner.languagesList.Count > pos ? partner.languagesList[pos] : "") + ",");
                newSubjectLanguages += (l + "," );
                pos++;
            }
            goodLine += ("," + (reportFolder != "GR"? newPairDiagnosis + newPairLanguages: newSubjectDiagnosis + newSubjectLanguages));
            
            if(badCols.Count>0)
            for (int i = badCols[0] + 1; i < lineCols.Length; i++)
            {
                if (!badCols.Contains(i))
                {
                    goodLine += (lineCols[i] + ",");
                }

            }
            sw.WriteLine(goodLine);
        }
        public static Boolean specialFilterOut(DateTime d)
        {

            Boolean filterOut = false;
            try
            {
                //if (szDate.Trim() != "")
                {
                    //DateTime d = Convert.ToDateTime(szDate);
                    if ((d.Year == 2022 && d.Month == 12 && d.Day == 7 && ((d.Hour == 10 && d.Minute >= 50) || ((d.Hour == 11 && d.Minute <= 25)))) ||
                        (d.Year == 2022 && d.Month == 12 && d.Day == 5 && (d.Hour == 11 && d.Minute >= 13 && d.Minute <= 40)) ||
                        (d.Year == 2022 && d.Month == 11 && d.Day == 16 && (d.Hour == 11 && d.Minute >= 11 && d.Minute <= 28)) ||
                        (d.Year == 2022 && d.Month == 11 && d.Day == 14 && (d.Hour == 12 && d.Minute >= 22 && d.Minute <= 48)) ||
                        (d.Year == 2022 && d.Month == 10 && d.Day == 21 && (d.Hour == 11 && d.Minute >= 0 && d.Minute <= 24)) ||

                        (d.Year == 2022 && d.Month == 10 && d.Day == 19 && ((d.Hour == 10 && d.Minute >= 58) || ((d.Hour == 11 && d.Minute <= 43)))) ||
                        (d.Year == 2023 && d.Month == 6 && d.Day == 15 && ((d.Hour == 10 && d.Minute >= 50) || ((d.Hour == 11 && d.Minute <= 22)))) ||
                        (d.Year == 2023 && d.Month == 4 && d.Day == 19 && (d.Hour == 11 && d.Minute >= 8 && d.Minute <= 28)) ||
                        (d.Year == 2023 && d.Month == 4 && d.Day == 17 && (d.Hour == 12 && d.Minute >= 37 && d.Minute <= 45)) ||

                        (d.Year == 2023 && d.Month == 3 && d.Day == 15 && ((d.Hour == 10 && d.Minute >= 49) || ((d.Hour == 11 && d.Minute <= 16)))) ||
                        (d.Year == 2023 && d.Month == 3 && d.Day == 13 && (d.Hour == 12 && d.Minute >= 38 && d.Minute <= 50)) ||
                        (d.Year == 2023 && d.Month == 2 && d.Day == 1 && ((d.Hour == 10 && d.Minute >= 38) || ((d.Hour == 11 && d.Minute <= 19)))) ||
                        (d.Year == 2023 && d.Month == 1 && d.Day == 30 && ((d.Hour == 8 && d.Minute >= 59) || ((d.Hour == 9 && d.Minute <= 26))))


                        )
                    {
                        filterOut = true;
                    }
                    else
                    {
                        filterOut = false;
                    }
                }
            }
            catch { }
            return filterOut;
        }
        public static String getDayMappingFileName(String dir, DateTime day, String className)
        {
            String mappingDayDir = dir + "//" + Utilities.getDateDashStr(day) + "//MAPPINGS//";
            String mappingDayFileNameNoPathBase = "MAPPING_" + className + ".CSV";
            String mappingDayFileNameNoPath = mappingDayFileNameNoPathBase;

            if (!File.Exists(mappingDayDir+ mappingDayFileNameNoPath))
            {
                mappingDayFileNameNoPath = mappingDayFileNameNoPathBase.Replace("OUTSIDE", "").Replace("BASE", "");
                mappingDayFileNameNoPath = mappingDayFileNameNoPath.Substring(0, mappingDayFileNameNoPath.IndexOf(".")) + "_"+ Utilities.getDateStr(day, "", 0) + ".csv";


                if (!File.Exists(mappingDayDir + mappingDayFileNameNoPath))
                {

                    mappingDayFileNameNoPath = mappingDayFileNameNoPathBase.Substring(0, mappingDayFileNameNoPathBase.IndexOf(".")) + "_" + Utilities.getDateStrMMDDYY(day, "_") + ".csv";

                    if (!File.Exists(mappingDayDir + mappingDayFileNameNoPath))
                    {

                        mappingDayFileNameNoPath = mappingDayFileNameNoPathBase.Substring(0, mappingDayFileNameNoPathBase.IndexOf(".")) + "_" + Utilities.getDateStr(day, "", 0) + ".csv";
                        if (!File.Exists(mappingDayDir + mappingDayFileNameNoPath))
                        {
                            mappingDayFileNameNoPath = mappingDayFileNameNoPathBase.Substring(0, mappingDayFileNameNoPathBase.IndexOf(".")) + "_" + Utilities.getDateStrMMDDYY(day, "") + ".csv";

                        }
                    }

                }

            }


           return mappingDayDir+ mappingDayFileNameNoPath;
        }

        /********************/////////////// DATE STUFF ////////////////********************
         
         
        public static DateTime getDate(String szDate)
        {
            return Convert.ToDateTime(szDate);
        }
        public static String getDateDashStr(DateTime d)
        {
            return getDateStr(d, "-",0);
        }
        public static String getDateStr(DateTime d, String szSep, int y)
        {
            return (d.Month <= 9 ? "0" + d.Month : d.Month.ToString()) + szSep +
                (d.Day <= 9 ? "0" + d.Day : d.Day.ToString()) + szSep +
                d.Year.ToString().Substring(y);
        }
        public static String getDateStrYYMMDD(DateTime d, String szSep, int y)
        {
            return d.Year.ToString().Substring(y) + szSep +
                (d.Month <= 9 ? "0" + d.Month : d.Month.ToString()) + szSep +
                (d.Day <= 9 ? "0" + d.Day : d.Day.ToString()) ;
        }
        public static String getDateNoZeroStr(DateTime d, String szSep)
        {
            return d.Month.ToString()  + szSep +
                d.Day.ToString() + szSep +
                d.Year.ToString();
        }
        public static String getDateStrMMDDYY(DateTime d)
        {
            return getDateStrMMDDYY( d,"");
        }
        public static String getDateStrMMDDYY(DateTime d, String sep)
        {
            return (d.Month <= 9 ? "0" + d.Month : d.Month.ToString()) +sep+
                (d.Day <= 9 ? "0" + d.Day : d.Day.ToString()) +sep+
                d.Year.ToString().Substring(2, 2);
        }
        public static String getTimeStr(DateTime t)
        {
            return t.Hour + ":" + (t.Minute < 10 ? "0" + t.Minute : t.Minute.ToString()) + ":" +
                (t.Second < 10 ? "0" + t.Second : t.Second.ToString()) + "." +
                (t.Millisecond < 10 ? "00" + t.Millisecond : t.Millisecond < 100 ? "0" + t.Millisecond : t.Millisecond.ToString());
        }
        public static DateTime geFullTime(DateTime first)
        {
            int ms = first.Millisecond;
            return new DateTime(first.Year, first.Month, first.Day, first.Hour, first.Minute, first.Second, ms);

        }
        public static bool isSameDay(DateTime d1, DateTime d2)
        {
            return d1.Year == d2.Year && d1.Month == d2.Month && d1.Day == d2.Day;
        }


        /********************/////////////// DATE STUFF ////////////////********************

        /********************/////////////// STUFF ////////////////********************
        
        public static Dictionary<String,Pair> getSzPairKey(Dictionary<String, PersonDayInfo> personDayMappings)
        {
            Dictionary<String, Pair> szPairs = new Dictionary<string, Pair>();
            int skip = 1;
            int pos = 1;
            foreach (String subject in personDayMappings.Keys)
            {
                PersonDayInfo subjectDayInfo = personDayMappings[subject];
                String szNumS = subjectDayInfo.mapId;
                szNumS = (szNumS.LastIndexOf("_") >= 0 ? szNumS.Substring(szNumS.LastIndexOf("_") + 1) : szNumS);
                szNumS= Regex.Match(szNumS, @"\d+").Value;

                foreach (String partner in personDayMappings.Keys)
                {
                    if (skip == 0)
                    {
                        PersonDayInfo partnerDayInfo = personDayMappings[partner];
                        String szNumP = partnerDayInfo.mapId;
                        szNumP = (szNumP.LastIndexOf("_") >= 0 ? szNumP.Substring(szNumP.LastIndexOf("_") + 1) : szNumP);
                        szNumP = Regex.Match(szNumP, @"\d+").Value;

                        if(Convert.ToInt16(szNumS)<= Convert.ToInt16(szNumP))
                        {
                            String szPairKey = subjectDayInfo.mapId + "|" + partnerDayInfo.mapId;
                            if(!szPairs.ContainsKey(szPairKey))
                            {
                                szPairs.Add(szPairKey, new Pair(szPairKey, subjectDayInfo.mapId, partnerDayInfo.mapId));
                            }
                        }
                        else
                        {
                            String szPairKey = partnerDayInfo.mapId + "|" + subjectDayInfo.mapId;
                            if (!szPairs.ContainsKey(szPairKey))
                            {
                                szPairs.Add(szPairKey, new Pair(szPairKey, partnerDayInfo.mapId, subjectDayInfo.mapId));
                            }
                        }


                    }
                    else
                    skip--;
                }
                pos++;
                skip = pos;
            }


            return szPairs;

        }
        public static String getLenaIdFromFileName(String szItsFileName)
        {
            String lenaId = szItsFileName;// file.Substring(file.IndexOf("\\") + 1);
            lenaId = lenaId.Substring( 16 , 6);
            if (lenaId.Substring(0, 2) == "00")
                lenaId = lenaId.Substring(2);
            else if (lenaId.Substring(0, 1) == "0")
                lenaId = lenaId.Substring(1);

            return lenaId;
        }
         
        public static void setVersion(double minGr, double maxGr)
        {
            szVersion = "GR" + minGr.ToString().Replace(".", "_") + maxGr.ToString().Replace(".", "_") + "_" + 
                "DEN_"+
                getDateStrMMDDYY(DateTime.Now) + "_V2" + new Random().Next();
        }
        public static void setVersion()
        {
            szVersion =  getDateStrMMDDYY(DateTime.Now) + "_" + new Random().Next();
        }
        public static String getPersonType(String type, String shortId)
        {
            return type != "" ? type.ToUpper().Trim() : 
                (shortId.IndexOf("L") == 0 || shortId.ToUpper().IndexOf("LAB")>=0 ? "LAB" : 
                shortId.IndexOf("T") == 0 || shortId.ToUpper().IndexOf("TEACHER")>=0 ? "TEACHER" : "");
        }
        public static String getNumberIdFromPerson(Person p)
        {

            return (p.subjectType == "LAB" ? "L" : p.subjectType == "TEACHER" ? "T" : "") + Regex.Match(p.shortId, @"\d+").Value; 
        }
        public static String getNumberIdFromChild(String p)
        {
            p = (p.LastIndexOf("_") >= 0 ? p.Substring(p.LastIndexOf("_") + 1) : p);
            return Regex.Match(p, @"\d+").Value;
        }
        public static String getNumberIdFromTeacher(String p)
        {
            p = (p.LastIndexOf("_") >= 0 ? p.Substring(p.LastIndexOf("_") + 1) : p);

            return Regex.Match(p, @"\d+").Value + "T" ;
        }
        //PERSON INFO STUFF//
        public static PersonDayInfo getPerson(Dictionary<String, PersonDayInfo> personDayMappings, String personId)
        {
            foreach(String szPersonId in personDayMappings.Keys)
            {
                if (szPersonId.Trim().ToUpper() == personId.Trim().ToUpper())
                    return personDayMappings[szPersonId];
            }
            return new PersonDayInfo();
        }
        public static String getChildIdFromSzNumber(Dictionary<String, PersonDayInfo> personDayMappings, String szNum)
        {
            foreach (String szPersonId in personDayMappings.Keys)
            {
                //getNumberIdFromChild(String p)
                if (szNum == getNumberIdFromChild(szPersonId))
                    return szPersonId;
            }

            return szNum;
        }
        public static String getTeacherIdFromSzNumber(Dictionary<String, PersonDayInfo> personDayMappings, String szNum)
        {
            foreach (String szPersonId in personDayMappings.Keys)
            {
                //getNumberIdFromChild(String p)
                if (szNum == getNumberIdFromTeacher(szPersonId) || 
                    szNum == szPersonId || 
                    (szPersonId.LastIndexOf("_") >= 0 && szNum == szPersonId.Substring(szPersonId.LastIndexOf("_")+1)))
                    return szPersonId;
            }

            return szNum;
        }
        public static double getCenter(double x, double x2)
        {
            double l = x2 - x!=0?Math.Abs(x2 - x) / 2:0;
            return x < x2 ? x + l : x2 + l;
        }
         
        public static double calcSquaredDist(PersonSuperInfo a, PersonSuperInfo b)
        {
            Double x1 = a.x;
            Double y1 = a.y;
            Double x2 = b.x;
            Double y2 = b.y;
            return Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2);
        }
        public static Tuple<double, double> withinOrientationData(PersonSuperInfo a, PersonSuperInfo b)
        {
            Tuple<double, double> r = new Tuple<double, double>(180, 180);
            if (a.xl > 0 && a.yl > 0 && b.xl > 0 && b.yl > 0)
            {
                double a_center_x = getCenter(a.xr, a.xl);
                double a_center_y = getCenter(a.yr, a.yl);
                double b_center_x = getCenter(b.xr, b.xl);
                double b_center_y = getCenter(b.yr, b.yl);

                double d_ab_x = b_center_x - a_center_x;
                double d_ab_y = b_center_y - a_center_y;// getCenter(b.ry, b.ly) - getCenter(a.ry, a.ly);
                normalize(ref d_ab_x, ref d_ab_y);
                double d_ba_x = -d_ab_y;
                double d_ba_y = d_ab_x;

                double da_x = a.xl - a.xr!=0? (a.xl - a.xr) / 2:a.xr;
                double da_y = a.yl - a.yr != 0 ? (a.yl - a.yr) / 2 : a.yr;
                double db_x = b.xl - b.xr != 0 ? (b.xl - b.xr) / 2 : b.xr;
                double db_y = b.yl - b.yr != 0 ? (b.yl - b.yr) / 2 : b.yr;

                normalize(ref da_x, ref da_y);
                normalize(ref db_x, ref db_y);

                double dx_a = (d_ab_x * da_x) + (d_ab_y * da_y);
                double dy_a = (d_ba_x * da_x) + (d_ba_y * da_y);
                double o_a = Math.Atan2(-dx_a, dy_a) * (180 / Math.PI);

                double dx_b = (d_ab_x * db_x) + (d_ab_y * db_y);
                double dy_b = (d_ba_x * db_x) + (d_ba_y * db_y);
                double o_b = Math.Atan2(dx_b, -dy_b) * (180 / Math.PI);
                r = new Tuple<double, double>((o_a), (o_b));
            }
            return r;
        }
        public static void normalize(ref double x, ref double y)
        {
            double r = Math.Sqrt((x * x) + (y * y));
            x = x / r;
            y = y / r;
        }

        public static void mergeDayFiles(String dir, String filter, String szNewFileName)
        {
            String[] szFiles = Directory.GetFiles(dir);
            TextWriter sw = new StreamWriter(dir+ "//"+   szNewFileName);
            Boolean includeHeader = true;
            foreach (String szFile in szFiles)
            {

                if (szFile.Contains(filter))
                {
                    using (StreamReader sr = new StreamReader(szFile))
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
                        includeHeader = false;
                    }
                     
                }
            }
            sw.Close();
                     
             
        }
        public static Tuple<double, double> getRelativeAngles(double a_xl, double a_xr, double a_yl, double a_yr, double b_xl, double b_xr, double b_yl, double b_yr)
        {
            Tuple<double, double> r = new Tuple<double, double>(180, 180);
            if (a_xl > 0 && a_yl > 0 && b_xl > 0 && b_yl > 0)
            {
                double a_center_x = getCenter(a_xr, a_xl);
                double a_center_y = getCenter(a_yr, a_yl);
                double b_center_x = getCenter(b_xr, b_xl);
                double b_center_y = getCenter(b_yr, b_yl);

                double d_ab_x = b_center_x - a_center_x;
                double d_ab_y = b_center_y - a_center_y;// getCenter(b_ry, b_ly) - getCenter(a_ry, a_ly);
                normalize(ref d_ab_x, ref d_ab_y);
                double d_ba_x = -d_ab_y;
                double d_ba_y = d_ab_x;

                double da_x = a_xl - a_xr != 0 ? (a_xl - a_xr) / 2 : a_xr;
                double da_y = a_yl - a_yr != 0 ? (a_yl - a_yr) / 2 : a_yr;
                double db_x = b_xl - b_xr != 0 ? (b_xl - b_xr) / 2 : b_xr;
                double db_y = b_yl - b_yr != 0 ? (b_yl - b_yr) / 2 : b_yr;

                normalize(ref da_x, ref da_y);
                normalize(ref db_x, ref db_y);

                double dx_a = (d_ab_x * da_x) + (d_ab_y * da_y);
                double dy_a = (d_ba_x * da_x) + (d_ba_y * da_y);
                double o_a = Math.Atan2(-dx_a, dy_a) * (180 / Math.PI);

                double dx_b = (d_ab_x * db_x) + (d_ab_y * db_y);
                double dy_b = (d_ba_x * db_x) + (d_ba_y * db_y);
                double o_b = Math.Atan2(dx_b, -dy_b) * (180 / Math.PI);
                r = new Tuple<double, double>((o_a), (o_b));
            }
            return r;
        }
         
    }
}

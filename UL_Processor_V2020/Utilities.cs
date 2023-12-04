using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
namespace UL_Processor_V2023
{
    class Utilities
    {
        public static String szVersion = "";
        
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
        public static String getDateNoZeroStr(DateTime d, String szSep)
        {
            return d.Month.ToString()  + szSep +
                d.Day.ToString() + szSep +
                d.Year.ToString();
        }
        public static String getDateStrMMDDYY(DateTime d)
        {
            return (d.Month <= 9 ? "0" + d.Month : d.Month.ToString()) +  
                (d.Day <= 9 ? "0" + d.Day : d.Day.ToString()) + 
                d.Year.ToString().Substring(2,2);
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

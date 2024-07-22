using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IronPython.Modules._ast;
using UL_Processor_V2023;
using IronPython.Runtime;
using System.Web.UI.WebControls;

namespace UL_Processor_V2020
{
    public class QAKalman
    {
        
        public void qa(String inputFile, String outputFile, int startHour, int endHour, DateTime classDay, ref Dictionary<String, PersonDayInfo> personDayMappings)
        {

            bool fileExisted = File.Exists(outputFile);
            TextWriter sw = new StreamWriter(outputFile,true);// countDays > 0);
            if(!fileExisted)
                sw.WriteLine("SUBJECT,LEFT_TAG,LPREV_TIME,LTIME,RIGHT_TAG,RPREV_TIME,RTIME,DAY,DIFFL,DIFFPRL,DIFFR,DIFFPLR");


            Dictionary<String, Dictionary<String, List<String>>> info = new Dictionary<string, Dictionary<string, List<string>>>();
            DateTime t = new DateTime();
            DateTime tmax=new DateTime();
            DateTime tmin =new DateTime(1999,1,1);


            using (StreamReader sr = new StreamReader(inputFile))
            {
                while (!sr.EndOfStream)
                {
                    String szLine = sr.ReadLine();
                    String[] line = szLine.Split(',');
                    if (line.Length >= 5)
                    {
                        String tag = line[1].Trim();
                        DateTime lineTime = Convert.ToDateTime(line[2]);
                        Double xPos = Convert.ToDouble(line[3]);
                        Double yPos = Convert.ToDouble(line[4]);
                        if (Utilities.isSameDay(lineTime, classDay) &&
                            lineTime.Hour >= startHour &&
                            lineTime.Hour <= endHour)
                        {
                            UbiLocation ubiLoc = new UbiLocation();
                            ubiLoc.tag = tag;

                            findTagPerson(ref ubiLoc, lineTime, ref personDayMappings);
                             
                            if (ubiLoc.id != "" &&
                                lineTime >= personDayMappings[ubiLoc.id].startDate &&
                                lineTime <= personDayMappings[ubiLoc.id].endDate)
                            {
                                

                                
                                
                                if(!info.ContainsKey(ubiLoc.id))
                                {
                                    info.Add(ubiLoc.id,new Dictionary<string, List<string>>());
                                    info[ubiLoc.id].Add("L", new List<string>());
                                    info[ubiLoc.id].Add("R", new List<string>());
                                     
                                }
                                info[ubiLoc.id][ubiLoc.type].Add(szLine);



                            }
                        }
                    }
                }


                foreach(String s in info.Keys)
                {
                    try
                    {
                        int idx_l = 0;
                        int idx_r = 0;
                        String[] cl = info[s]["L"][0].Split(',');
                        String[] cr = info[s]["R"][0].Split(',');

                        tmin = Convert.ToDateTime(cl[2]) > Convert.ToDateTime(cr[2]) ? Convert.ToDateTime(cr[2]) : Convert.ToDateTime(cl[2]);


                        cl = info[s]["L"][info[s]["L"].Count-1].Split(',');
                        cr = info[s]["R"][info[s]["R"].Count - 1].Split(',');

                        tmax = Convert.ToDateTime(cl[2]) > Convert.ToDateTime(cr[2]) ? Convert.ToDateTime(cr[2]) : Convert.ToDateTime(cl[2]);
                        t = tmin;
                        int ms = t.Millisecond / 100 * 100 + 100;
                        if (t.Millisecond % 100 == 0)
                        {
                            ms -= 100;
                        }
                        if (ms == 1000)
                        {
                            if (t.Second < 59)
                            {
                                t = new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second + 1, 0);
                            }
                            else if (t.Minute < 59)
                            {
                                t = new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute + 1, 0, 0);
                            }
                            else
                            {
                                t = new DateTime(t.Year, t.Month, t.Day, t.Hour + 1, 0, 0, 0);
                            }
                        }
                        else
                        {
                            t = new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second, ms);
                        }

                        int idx_lp = 0;
                        int idx_rp = 0;
                        while (t < tmax)
                        {
                             
                        try
                        {
                            while (Convert.ToDateTime(info[s]["L"][idx_l].Split(',')[2]) < t)
                                {
                                    idx_l += 1; 
                                    if (idx_l == 13535 && idx_r == 11743)
                                    {
                                        bool stop = true;
                                    }
                                }
                                 

                            while (Convert.ToDateTime(info[s]["R"][idx_r].Split(',')[2]) < t)
                                {
                                    idx_r += 1;
                                    if (idx_l == 13535 && idx_r == 11743)
                                    {
                                        bool stop = true;
                                    }
                                }
                             

                                String[] dl = info[s]["L"][idx_l].Split(',');
                            String[] dr = info[s]["R"][idx_r].Split(',');
                            DateTime tl = Convert.ToDateTime(dl[2]);
                            DateTime tr = Convert.ToDateTime(dr[2]);


                            //if abs((data_l["Time"][idx_l] - data_r["Time"][idx_r]).total_seconds()) > GAP_TOLERANCE
                            ////and (abs((data_l["Time"][idx_l] - data_l["Time"][idx_l-1]).total_seconds()) < GAP_TOLERANCE 
                            ///or abs((data_r["Time"][idx_r] - data_r["Time"][idx_r-1]).total_seconds()) < GAP_TOLERANCE 
                            ///or abs((data_r["Time"][idx_r] - data_l["Time"][idx_l-1]).total_seconds()) < GAP_TOLERANCE 
                            ///or abs((data_l["Time"][idx_l] - data_r["Time"][idx_r-1]).total_seconds()) < GAP_TOLERANCE):
                            if (Math.Abs((tl - tr).TotalSeconds) >= 60 && (idx_lp != idx_l || idx_rp != idx_r) &&
                                (
                                    (idx_l > 0 && Math.Abs((tl - Convert.ToDateTime(info[s]["L"][idx_l-1].Split(',')[2])).TotalSeconds) < 60)||
                                    (idx_l > 0 && Math.Abs((tr - Convert.ToDateTime(info[s]["L"][idx_l-1].Split(',')[2])).TotalSeconds) < 60) ||
                                    (idx_r > 0 && Math.Abs((tr - Convert.ToDateTime(info[s]["R"][idx_r-1].Split(',')[2])).TotalSeconds) < 60) ||
                                    (idx_l > 0 && idx_r>0 && Math.Abs((tl - Convert.ToDateTime(info[s]["R"][idx_r-1].Split(',')[2])).TotalSeconds) < 60)
                                )
                                )
                            {
                                 
                                sw.WriteLine(s + "," +
                                    info[s]["L"][idx_l].Split(',')[1] + "," +
                                    (idx_l > 1 ? info[s]["L"][idx_l - 1].Split(',')[2] : "") + "," +
                                    info[s]["L"][idx_l].Split(',')[2] + "," +
                                    info[s]["R"][idx_r].Split(',')[1] + "," +
                                     (idx_r > 1 ? info[s]["R"][idx_r - 1].Split(',')[2] : "") + "," +
                                    info[s]["R"][idx_r].Split(',')[2] + ","+tl.Month+"/"+tl.Day+"/"+tl.Year+","+
                                    (idx_l > 0 ? Math.Abs((tl - Convert.ToDateTime(info[s]["L"][idx_l - 1].Split(',')[2])).TotalSeconds) :0) +","+
                                    (idx_l > 0 ? Math.Abs((tr - Convert.ToDateTime(info[s]["L"][idx_l - 1].Split(',')[2])).TotalSeconds) : 0) + "," +
                                    (idx_r > 0 ? Math.Abs((tr - Convert.ToDateTime(info[s]["R"][idx_r - 1].Split(',')[2])).TotalSeconds) : 0) + "," +
                                    (idx_l > 0 && idx_r > 0 ? Math.Abs((tl - Convert.ToDateTime(info[s]["R"][idx_r - 1].Split(',')[2])).TotalSeconds):0)
                                    );
                                }


                                t = t.AddMilliseconds(100);
                            }
                            catch (Exception e)
                            {

                            }
                            
                                idx_rp = idx_r;
                                idx_lp = idx_l;

                        }


                    }
                    catch(Exception e)
                    {

                    }
 
                } 
                 

                sw.Close();
            }





        }
        public void findTagPerson(ref UbiLocation ubiLocation, DateTime dt, ref Dictionary<String, PersonDayInfo> personDayMappings)
        {
            foreach (String key in personDayMappings.Keys)
            {
                PersonDayInfo pdi = personDayMappings[key];

                //00:11:CE:00:00:00:D4:C3	00:11:CE:00:00:00:D5:F4


                if (pdi.present && pdi.status == "PRESENT" &&
                        dt >= pdi.startDate && dt <= pdi.endDate)
                {
                    if (ubiLocation.tag == pdi.leftUbi)
                    {
                        ubiLocation.id = key;
                        ubiLocation.type = "L";
                        break;
                    }
                    else if (ubiLocation.tag == pdi.rightUbi)
                    {
                        ubiLocation.id = key;
                        ubiLocation.type = "R";
                        break;
                    }


                }

            }

        }

    }
    
}

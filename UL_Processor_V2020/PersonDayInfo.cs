using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UL_Processor_V2020;

namespace UL_Processor_V2023
{
    public class PersonDayInfo
    {
        public String mapId = "";
        public String status = "ABSENT";
        public String lenaId = "";
        public String sonyId = "";
        public String leftUbi = "";
        public String rightUbi = "";
        public Boolean present = false;
        public DateTime startDate = new DateTime(2000, 1, 1);
        public DateTime endDate = new DateTime(2000, 2, 1);

        public LenaVars totalLenaVars = new LenaVars();
        public LenaVars WUBILenaVars = new LenaVars();

        public AliceVars totalAliceVars = new AliceVars();
        public AliceVars WUBIAliceVars = new AliceVars();

        public Dictionary<String, LenaVars> totalLenaVarsAct = new Dictionary<string, LenaVars>();
        public DateTime maxTime = new DateTime(1999,1,1);
       // public Dictionary<String, LenaVars> WUBILenaVarsAct = new Dictionary<string, LenaVars>();




        public PersonDayInfo()
        {
        }
        public PersonDayInfo(String commaLine, String id, DateTime sd, DateTime ed)
        {
            String[] line = commaLine.Split(',');
            try
            {
                mapId = id;
                lenaId = line[9].Trim();
                leftUbi = line[5].Trim();
                rightUbi = line[7].Trim();
                present = line[19].ToUpper() == "PRESENT";
                status = line[19].ToUpper();
                //DEBUG AND THEN DELETE
                try
                {
                    startDate = Convert.ToDateTime(line[11].Trim());
                    startDate = new DateTime(sd.Year, sd.Month, sd.Day, startDate.Hour, startDate.Minute, startDate.Second);
                }
                catch (Exception e)
                {
                    startDate = sd;
                }
                try
                {
                    endDate = Convert.ToDateTime(line[12].Trim());
                    endDate = new DateTime(ed.Year, ed.Month, ed.Day, endDate.Hour, endDate.Minute, endDate.Second);
                    if (endDate.Hour >= 1)
                        endDate=endDate.AddHours(12);
                }
                catch (Exception e)
                {
                    endDate = ed;
                }
            }
            catch (Exception e)
            {

            }
        }
        public PersonDayInfo(String commaLine, String id, DateTime sd, DateTime ed, Dictionary<String, int> columnIndex)
        {
            String[] line = commaLine.Split(',');
            try
            {
                mapId = id;
                lenaId = line[columnIndex["LENA"]].Trim();
                sonyId = columnIndex["SONY"]>=0?line[columnIndex["SONY"]].Trim():"";
                leftUbi = line[columnIndex["LEFT"]].Trim().Length>6?line[columnIndex["LEFT"]].Trim(): "00:11:CE:00:00:00:" + line[columnIndex["LEFT"]].Trim();
                rightUbi = line[columnIndex["RIGHT"]].Trim().Length > 6 ? line[columnIndex["RIGHT"]].Trim() : "00:11:CE:00:00:00:" + line[columnIndex["RIGHT"]].Trim();
                present = line[columnIndex["STATUS"]].ToUpper() == "PRESENT";
                status = line[columnIndex["STATUS"]].ToUpper();

                if (present)
                {
                    try
                    {
                        String[] startTime = line[columnIndex["START"]].Trim().Split(':');
                        startDate = new DateTime(sd.Year, sd.Month, sd.Day, Convert.ToInt16(startTime[0]), Convert.ToInt16(startTime[1]), 0);
                    }
                    catch (Exception e)
                    {
                        startDate = sd;
                        Console.WriteLine(e.Message);
                    }
                    try
                    {
                        String[] endTime = line[columnIndex["END"]].Trim().Split(':');
                        endDate = new DateTime(ed.Year, ed.Month, ed.Day, Convert.ToInt16(endTime[0]), Convert.ToInt16(endTime[1]), endDate.Second);
                        if (endDate.Hour >= 1)
                            endDate = endDate.AddHours(12);
                    }
                    catch (Exception e)
                    {
                        endDate = ed;
                        Console.WriteLine(e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

            }
        }
    }
}

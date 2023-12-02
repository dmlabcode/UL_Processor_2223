using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


using System.Diagnostics;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace UL_Processor_V2023
{
    class Program
    {
        static String szVersion = "";
       
        static void Main(string[] arguments)
        {

            //String[] szClassroomsToProcess = { "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:StarFish_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:03/15/2023 HACKT1:NO ONSETS:YES TEN:YES VEL:NO ANGLES:YES SUMALL:YES ITS:YES GR:YES DBS:YES APPROACH:YES SOCIALONSETS:YES" };
            String szClassroomSettings = " REDENOISE:NO PROCESS:YES";
            
            String[] szClassroomsToProcess = {
                  "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:StarFish_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
                  //"06/15/2023" +
                  "10/19/2022,10/21/2022,11/14/2022,11/16/2022,12/05/2022,12/07/2022,01/30/2023,02/01/2023,03/13/2023,03/15/2023,04/17/2023,04/19/2023,06/15/2023" +
                  szClassroomSettings};
            
             
            /******** A)FOR EACH CLASSROOM:********/
            foreach (String szClassroomArgs in szClassroomsToProcess)
            {
                Boolean toProcess = true;// false;// true;// false;

                /*1- Create Classroom Object, read and set Parameters*/
                Classroom classRoom = new Classroom();

                classRoom.ubiCleanup = true;//  true;// true;// false;// true;// false;
                classRoom.reDenoise = false;//true;// false;// true;// false;
              
                String[] args = szClassroomArgs.Split(' ');
                
                foreach (String arg in args) 
                {
                    String[] setting = arg.Split(':');
                     
                    if (setting.Length > 1)
                    {
                        switch (setting[0].Trim())
                        {
                            case "DORECINFO":
                                classRoom.ubiCleanup = setting[1].Trim().ToUpper() == "YES";
                                break;
                            case "EXISTINGVERSION":
                                classRoom.processData = false;
                                Utilities.szVersion = setting[1].Trim();
                                break;
                            case "UBICLEANUP":
                                classRoom.ubiCleanup = setting[1].Trim().ToUpper() == "YES";
                                break;
                            case "ADDGP":
                                //  classRoom.addGp = setting[1].Trim().ToUpper() == "YES";
                                break;
                            case "REDENOISE":
                                classRoom.reDenoise = setting[1].Trim().ToUpper() == "YES";
                                break;
                            case "PROCESS":
                                toProcess = setting[1].Trim().ToUpper() == "YES";
                                break;
                            case "DIR":
                                classRoom.dir = setting[1].Trim() + ":" + setting[2].Trim();
                                break;
                            case "ANGLE":
                                classRoom.angle = Convert.ToDouble(setting[1].Trim());
                                break;
                            case "CLASSNAME":
                                classRoom.className = setting[1].Trim();
                                break;
                            case "GRMIN":
                                classRoom.grMin = Convert.ToDouble(setting[1].Trim());
                                break;
                            case "GRMAX":
                                classRoom.grMax = Convert.ToDouble(setting[1].Trim());
                                break;
                            case "HRMIN":
                                classRoom.startHour = Convert.ToInt16(setting[1].Trim());
                                break;
                            case "HRMAX":
                                classRoom.endHour = Convert.ToInt16(setting[1].Trim());
                                break;
                            case "MINMAX":
                                classRoom.endMinute = Convert.ToInt16(setting[1].Trim());
                                break;
                            case "DAYS":
                                foreach (String szDate in setting[1].Trim().Split(','))
                                {
                                    classRoom.classRoomDays.Add(Utilities.getDate(szDate));
                                }
                                break;
                            
                        }
                    }
                }



                /*2- Set Version Name extension for file naming: GR+minGrwith_insteadOfDots+maxGrwith_insteadOfDots+TodaysMMDDYY+RANDOMNUMBER
                     Set Classroom Object mapId to link mapping files and data
                     Create directories for distinct reports*/


                if(Utilities.szVersion.Trim() == "")
                    Utilities.setVersion(classRoom.grMin, classRoom.grMax);//run day and GR version for file naming
                
                classRoom.mapById = "LONGID";
                classRoom.setDirs();


                /*3- Set Classroom’s Base Mappings */
                classRoom.setBaseMappings();
              
                /*4 Clean ubi */
                if (classRoom.ubiCleanup)
                    classRoom.clean();
                 

                classRoom.denoise();  

                /* 5 Process */
                if (toProcess)
                    classRoom.process(true, true);

                // classRoom.processGofRfiles();
                // classRoom.processFromGofRfiles("", true);
                //classRoom.processOnsetsGrAndActLogs(); TO DELETE


                classRoom.mergeDayFiles();



                //Utilities.szVersion = "10_26_2020_478216537";// "10_21_2020_2098687227";// "10_20_2020_419130690";// "10_20_2020_986296434";// "10_19_2020_1345568271";//10_19_2020_1345568271  10_19_2020_1700354507
                classRoom.getPairActLeadsFromFiles();

            }

            Console.ReadLine();
        }
    }
}

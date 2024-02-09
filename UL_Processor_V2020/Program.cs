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


            /*  String[] szClassroomsToProcess = {
                  "DIR:D://CLASSROOMS_2122// CLASSNAME:LEAP_AM_2122 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
                  "01/28/22"+
                  //,04/26/2023,06/20/2023,06/22/2023,07/25/2023,07/27/2023" +
                  szClassroomSettings};
            
             */
            /* 2223
           * String[] szClassroomsToProcess = {
                "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:StarFish_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
                //"04/17/2023,04/19/2023" +
                "10/19/2022,10/21/2022,11/14/2022,11/16/2022,12/05/2022,12/07/2022,01/30/2023,02/01/2023,03/13/2023,03/15/2023,04/17/2023,04/19/2023,06/15/2023" +
                szClassroomSettings};

          String[] szClassroomsToProcess = {
                 "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:LadyBugs_2223 GRMIN:0 GRMAX:1.5 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
                 "09/12/2022,10/12/2022,10/31/2022,01/18/2023,01/20/2023,02/13/2023,02/15/2023"+
                 ",04/26/2023,06/20/2023,06/22/2023,07/25/2023,07/27/2023" +
                 szClassroomSettings};

              String[] szClassroomsToProcess = {
                 "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:LadyBugs_2223 GRMIN:0 GRMAX:1.5 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
                 "09/12/2022,10/12/2022,10/31/2022,01/18/2023,01/20/2023,02/13/2023,02/15/2023"+
                 ",04/26/2023,06/20/2023,06/22/2023,07/25/2023,07/27/2023" +
                 szClassroomSettings}; 

            
            String[] szClassroomsToProcess = {
                   "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:LEAP_AM_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:10 MINMAX:50 DAYS:" +
                   "10/07/2022"+
                   ",11/29/2022,12/12/2022,12/14/2022,"+
                   "01/24/2023,02/08/2023"+
                   ","+
                   "02/10/2023"+
                   ","+
                   "03/08/2023"+
                   ",03/10/2023"+
                   ",04/04/2023,04/06/2023,05/16/2023"+//,10/07/2022,11/29/2022,12/12/2022,12/14/2022" +
                   szClassroomSettings};

            String[] szClassroomsToProcess = {
                   "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:ES_ROOM8_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
                   "10/25/2022,11/15/2022,12/15/2022,12/19/2022"+
                   ",01/25/2023,01/27/2023,03/30/2023,03/31/2023,04/24/2023,04/25/2023,05/30/2023,05/31/2023"
                   szClassroomSettings};

             String[] szClassroomsToProcess = {
                   "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Bubbles_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
                   "01/26/2024"+
                   szClassroomSettings};
            String[] szClassroomsToProcess = {
                   "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:ES_ROOM8_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
                   "10/25/2022,11/15/2022"+
                   szClassroomSettings};

          */
            String szClassroomSettings = " REDENOISE:NO PROCESS:YES JUSTPLS:YES";


            String[] szClassroomsToProcess = {
                   "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:Turtles_2223"+
                   szClassroomSettings};



            /******** A)FOR EACH CLASSROOM:********/
            foreach (String szClassroomArgs in szClassroomsToProcess)
            {
                Boolean toProcess = true;// false;// true;// false;

                /*1- Create Classroom Object, read and set Parameters*/
                Classroom classRoom = new Classroom();

                classRoom.ubiCleanup = true;// true;//  true;// true;// false;// true;// false;
                classRoom.reDenoise = false;//true;// false;// true;// false;
              
                String[] args = szClassroomArgs.Split(' ');
                
                foreach (String arg in args) 
                {
                    String[] setting = arg.Split(':');
                     
                    if (setting.Length > 1)
                    {
                        switch (setting[0].Trim())
                        {
                            case "JUSTPLS":
                                classRoom.justPLS = setting[1].Trim().ToUpper() == "YES";
                                break;
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
                            case "ALICE":
                                classRoom.includeAlice = setting[1].Trim().ToUpper() == "YES";
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


                if (Utilities.szVersion.Trim() == "")
                    Utilities.setVersion(classRoom.grMin, classRoom.grMax);//run day and GR version for file naming

                classRoom.mapById = "LONGID";
                classRoom.setDirs();


                /*3- Set Classroom’s Base Mappings */
                classRoom.setBaseMappings();

                if (classRoom.justPLS) 
                {
                    classRoom.processPLSs();
                }
                else 
                {
                    classRoom.createReportDirs();
                    /*4 Clean ubi */
                    classRoom.ubiCleanup = classRoom.reDenoise ? true : classRoom.ubiCleanup;
                    if (classRoom.ubiCleanup)
                        classRoom.cleanUbiFiles();


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

            }

            Console.ReadLine();
        }
    }
}

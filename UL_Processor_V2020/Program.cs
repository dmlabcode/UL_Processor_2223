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
using IronPython.Runtime.Operations;
using UL_Processor_V2020;

namespace UL_Processor_V2023
{
    class Program
    {
        static String szVersion = "";
        static void processUAWith(String[] szClassroomsToProcess)
        {
            
            processUA(szClassroomsToProcess, false);
            
           
        }
        static void processUAWith(String[] szClassroomsToProcess, Boolean justCleanAlice)
        {

            processUA(szClassroomsToProcess, justCleanAlice);


        }
        static void Main(string[] arguments)
        {
            String szClassroomSettings = " MAP_PREFIX:APPLETREE_2526 REDENOISE:NO PROCESS:YES KALMAN:YES JUSTPLS:NO LABS:YES SEWIO:NO";
            String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2526// CLASSNAME:AppleTree_2526 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
      "09/05/2025,10/24/2025,12-05-2025"+
       szClassroomSettings};

            processUL(szClassroomsToProcess);




            /*
             * 
             *  String szClassroomSettings = " MAP_PREFIX:APPLETREE_2526 REDENOISE:NO PROCESS:YES KALMAN:YES JUSTPLS:NO LABS:YES SEWIO:NO"; 
          String[]    szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2526// CLASSNAME:AppleTree_2526 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
      "09/05/2025,10/24/2025"+//,12-05-2025"+
       szClassroomSettings};
             String szClassroomSettings = " MAP_PREFIX:BUBBLES_2526 REDENOISE:NO PROCESS:YES KALMAN:YES JUSTPLS:NO LABS:YES SEWIO:NO";

            String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2526// CLASSNAME:Bubbles_2526 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
      "09/10/2025,10/22/2025,12-08-2025"+
       szClassroomSettings};


            processUL(szClassroomsToProcess);*/


             

            Console.ReadLine();
           


        }
        
        static void getInterpolation(string[] szClassroomsToProcess)
        { 
            /******** A)FOR EACH CLASSROOM:********/
            foreach (String szClassroomArgs in szClassroomsToProcess)
            {
                /*1- Create Classroom Object, read and set Parameters*/
                Classroom classRoom = new Classroom();
                String[] args = szClassroomArgs.Split(' ');
                foreach (String arg in args)
                {
                    String[] setting = arg.Split(':');

                    if (setting.Length > 1)
                    {
                        switch (setting[0].Trim())
                        {
                            case "CLASSNAME":
                                classRoom.className = setting[1].Trim();
                                break;
                            case "FOLDER":
                                classRoom.classFolder = setting[1].Trim();
                                break;
                            case "MAP_PREFIX":
                                classRoom.mapPrefix = setting[1].Trim();
                                break;
                            case "DIR":
                                classRoom.dir = setting[1].Trim() + ":" + setting[2].Trim();
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

                classRoom.setDirs();
                classRoom.mapById = "LONGID";

                /*3- Set Classroom’s Base Mappings */
                classRoom.setBaseMappings();
                InterpolationInfo ii = new InterpolationInfo();
                ii.getInfo(classRoom, "C:\\IBSS\\CLASSROOMS_2324\\LEAP_AM\\SYNC\\GR");


            }

            Console.ReadLine();
        }
        static void syncWhisper(string[] szClassroomsToProcess, Boolean isToneDetection)
        { 
            /******** A)FOR EACH CLASSROOM:********/
            foreach (String szClassroomArgs in szClassroomsToProcess)
            {
                /*1- Create Classroom Object, read and set Parameters*/
                Classroom classRoom = new Classroom();
                String[] args = szClassroomArgs.Split(' ');
                foreach (String arg in args)
                {
                    String[] setting = arg.Split(':');

                    if (setting.Length > 1)
                    {
                        switch (setting[0].Trim())
                        {
                            case "CLASSNAME":
                                classRoom.className = setting[1].Trim();
                                break;
                            case "FOLDER":
                                classRoom.classFolder = setting[1].Trim();
                                break;
                            case "MAP_PREFIX":
                                classRoom.mapPrefix = setting[1].Trim();
                                break;
                            case "DIR":
                                classRoom.dir = setting[1].Trim() + ":" + setting[2].Trim();
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
                classRoom.setDirs();
                classRoom.mapById = "LONGID";

                /*3- Set Classroom’s Base Mappings */
                classRoom.setBaseMappings();
                WhisperSync ws = new WhisperSync();
                if(!isToneDetection)
                    ws.syncWhisper2223(classRoom);
                else
                    ws.syncWhisperTone(classRoom);


            }

            Console.ReadLine();
        }
    
    static void processUA(string[] szClassroomsToProcess, Boolean justCleanAlice)
        {
            /******** A)FOR EACH CLASSROOM:********/
            foreach (String szClassroomArgs in szClassroomsToProcess)
            {
                 
                /*1- Create Classroom Object, read and set Parameters*/
                Classroom classRoom = new Classroom();
                 
                String[] args = szClassroomArgs.Split(' ');
                 
                foreach (String arg in args) 
                {
                    String[] setting = arg.Split(':');
                     
                    if (setting.Length > 1)
                    {
                        switch (setting[0].Trim())
                        {
                            case "DIR":
                                classRoom.dir = setting[1].Trim() + ":" + setting[2].Trim();
                                break;
                            case "ANGLE":
                                classRoom.angle = Convert.ToDouble(setting[1].Trim());
                                break;
                            case "CLASSNAME":
                                classRoom.className = setting[1].Trim();
                                break;
                            case "FOLDER":
                                classRoom.classFolder = setting[1].Trim();
                                break;
                            case "MAP_PREFIX":
                                classRoom.mapPrefix = setting[1].Trim();
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
                if (!Directory.Exists(classRoom.dir + "//SYNC//ALICE_PAIRACTIVITY"))
                Directory.CreateDirectory(classRoom.dir + "//SYNC//ALICE_PAIRACTIVITY");
                classRoom.filesToMerge.Add("ALICE_PAIRACTIVITIES", new List<string>());
                if (!Directory.Exists(classRoom.dir + "//SYNC//GR"))
                    Directory.CreateDirectory(classRoom.dir + "//SYNC//GR");
                /*4 Clean ubi */
                classRoom.cleanUbiFiles();

                classRoom.processAlice(justCleanAlice);

               classRoom.mergeDayFiles();



                //Utilities.szVersion = "10_26_2020_478216537";// "10_21_2020_2098687227";// "10_20_2020_419130690";// "10_20_2020_986296434";// "10_19_2020_1345568271";//10_19_2020_1345568271  10_19_2020_1700354507
                classRoom.getPairActLeadsFromDir("ALICE_PAIRACTIVITY", "ALICE_PAIRACTIVITY");
                //classRoom.getPairActLeadsFromFiles();
            }

            //Console.ReadLine();
        }


        static void processUL(string[] szClassroomsToProcess)
        {
            /******** A)FOR EACH CLASSROOM:********/
            foreach (String szClassroomArgs in szClassroomsToProcess)
            {
                Boolean toProcess = true;// false;// true;// false;

                /*1- Create Classroom Object, read and set Parameters*/
                Classroom classRoom = new Classroom();

                classRoom.ubiCleanup = true;// true;//  true;// true;// false;// true;// false;
                classRoom.reDenoise = false;//true;// false;// true;// false;

                String[] args = szClassroomArgs.Split(' ');

                String szCustom = "1";

                foreach (String arg in args)
                {
                    String[] setting = arg.Split(':');

                    if (setting.Length > 1)
                    {
                        switch (setting[0].Trim())
                        {
                            case "KALMAN":
                                classRoom.kalman = setting[1].Trim().ToUpper() == "YES";
                                break;
                            case "LABS":
                                classRoom.includeLabs = setting[1].Trim().ToUpper() == "YES";
                                break;
                            case "SEWIO":
                                classRoom.isSewio = setting[1].Trim().ToUpper() == "YES";
                                break;
                            case "JUSTPLS":
                                classRoom.justPLS = setting[1].Trim().ToUpper() == "YES";
                                break;
                            case "CUSTOM":
                                szCustom = setting[1].Trim().ToUpper();
                                break;
                            // case "DORECINFO":
                            //     classRoom.ubiCleanup = setting[1].Trim().ToUpper() == "YES";
                            //     break;
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
                            case "FOLDER":
                                classRoom.classFolder = setting[1].Trim();
                                break;
                            case "MAP_PREFIX":
                                classRoom.mapPrefix = setting[1].Trim();
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
                //Utilities.szVersion = "072224_V22072780461";// "10_21_2020_2098687227";// "10_20_2020_419130690";// "10_20_2020_986296434";// "10_19_2020_1345568271";//10_19_2020_1345568271  10_19_2020_1700354507
                //classRoom.getPairActLeadsFromFiles("Synched_Data_GR0_2to2_ANGLE45");

                //Utilities.resetDiagnosisAndLanguages(classRoom, "SYNC");// "Synched_Data_GR0_22_DEN_MAXZ1_25");


                if (!classRoom.justPLS && szCustom == "1")
                {

                    classRoom.createReportDirs();
                    /*4 Clean ubi */
                    classRoom.ubiCleanup = classRoom.reDenoise ? true : classRoom.ubiCleanup;
                    if (classRoom.ubiCleanup)
                        classRoom.cleanUbiFiles();


                    if (classRoom.kalman)
                        classRoom.denoise();

                    /* 5 Process */
                    //classRoom.processUbi(true);//DELETE DEBUG
                    if (toProcess)
                    {
                        if (classRoom.kalman)
                            classRoom.process(true, true);
                        else
                            classRoom.processUbi(true);
                    }




                    classRoom.mergeDayFiles();



                    //Utilities.szVersion = "10_26_2020_478216537";// "10_21_2020_2098687227";// "10_20_2020_419130690";// "10_20_2020_986296434";// "10_19_2020_1345568271";//10_19_2020_1345568271  10_19_2020_1700354507
                    classRoom.getPairActLeadsFromFiles();
                }
                else if (classRoom.justPLS)
                {

                    classRoom.processPLSs();
                }
                else if (szCustom == "3")
                {
                    classRoom.processOutsideLenas();
                }

            }

            Console.ReadLine();
        }


    }
}
 
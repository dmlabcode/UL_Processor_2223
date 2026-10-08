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
using static IronPython.Modules._ast;
using static System.Net.Mime.MediaTypeNames;

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

            //Tuple<double,double> d= Utilities.getRelativeAngles( 1,  3,  0,  2,  8,  6,  2,  0);




            string cmd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.Replace("\\bin\\Debug", ""), "whisper_vtc1_alignment_v3.py");// "vtcwhisper.py");
            
            //python whisper_vtc1_alignment.py Path/To/Whisper/Output.csv Path/To/VTC1/file.rttm --alt-labels
            //python match_speakers.py whisper.csv vtc.csv output.csv
            //string cmdPython = cmd.Replace("vtcwhisper.py", "\\Python310\\python.exe");
            string cmdPython = cmd.Replace("whisper_vtc1_alignment_v3.py", "\\Python310\\python.exe");
            string args = "H:\\07-16-2026\\Whisper_Data\\ " +
                "H:\\07-16-2026\\ALICE_Data\\diarization_output.rttm " +
                "H:\\07-16-2026\\Whisper_Data_with_ALICE";// +
                //"H:\\2526_TEMP\\05-04-2026\\MAPPINGS\\MAPPING_StarFish_2526.csv ";
            // H:\\2526_TEMP\\05-04-2026\\SUBJECT_2\\SLP\\test.csv ";
            //H:\\2526_LRIC_TEMP\\05-14-2026\\108_01_AST_2min_large-v2_wtime.csv H:\\2526_LRIC_TEMP\\05-14-2026\\diarization_output_108_01_wtime.csv H:\\2526_LRIC_TEMP\\05-14-2026\\test.csv ";

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "C:\\VS\\UL_PROCESSOR_2223\\UL_Processor_V2020\\Python310\\python.exe";
            start.Arguments = string.Format("{0} {1}", cmd, args);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            //

         /*   using (Process process = Process.Start(start))
            {
                 
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.Write(result);

                    process.WaitForExit();
                    Console.Write(result);
                }
            }
         */
            //python MLU_calculation.py Path/To/Aligned/Transcript/Speaker/file.csv --output Path/For/MLU/output.csv
            cmd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.Replace("\\bin\\Debug", ""), "MLU_calculation.py");// "vtcwhisper.py");
            cmdPython = cmd.Replace("MLU_calculation.py", "\\Python310\\python.exe");
            args = "H:\\07-16-2026\\036_LRIC_APPLETREE_2526_8_071626_AST_2min_large-v2_ALICE.csv --output H:\\07-16-2026\\MLU_DS_STARFISH_2526_8_071626.csv";// H:\\2526_TEMP\\05-04-2026\\SUBJECT_2\\SLP\\test.csv ";
                                                                                                                                                               //H:\\2526_LRIC_TEMP\\05-14-2026\\108_01_AST_2min_large-v2_wtime.csv H:\\2526_LRIC_TEMP\\05-14-2026\\diarization_output_108_01_wtime.csv H:\\2526_LRIC_TEMP\\05-14-2026\\test.csv ";

            start = new ProcessStartInfo();
            start.FileName = "C:\\VS\\UL_PROCESSOR_2223\\UL_Processor_V2020\\Python310\\python.exe";
            start.Arguments = string.Format("{0} {1}", cmd, args);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            //
            /*
            using (Process process = Process.Start(start))
            {

                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.Write(result);

                    process.WaitForExit();
                    Console.Write(result);
                }
            }
         
            
            */

            args = "H:\\07-16-2026\\038_LRIC_APPLETREE_2526_10_071626_AST_2min_large-v2_ALICE.csv --output H:\\07-16-2026\\MLU_DS_STARFISH_2526_10_071626.csv";// H:\\2526_TEMP\\05-04-2026\\SUBJECT_2\\SLP\\test.csv ";
                                                                                                                                                                                   //H:\\2526_LRIC_TEMP\\05-14-2026\\108_01_AST_2min_large-v2_wtime.csv H:\\2526_LRIC_TEMP\\05-14-2026\\diarization_output_108_01_wtime.csv H:\\2526_LRIC_TEMP\\05-14-2026\\test.csv ";

            start = new ProcessStartInfo();
            start.FileName = "C:\\VS\\UL_PROCESSOR_2223\\UL_Processor_V2020\\Python310\\python.exe";
            start.Arguments = string.Format("{0} {1}", cmd, args);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            //

            /* using (Process process = Process.Start(start))
             {

                 using (StreamReader reader = process.StandardOutput)
                 {
                     string result = reader.ReadToEnd();
                     Console.Write(result);

                     process.WaitForExit();
                     Console.Write(result);
                 }
             }
            */


            String szClassroomSettings = " MAP_PREFIX:Pandas_2526 REDENOISE:NO PROCESS:YES KALMAN:YES JUSTPLS:NO LABS:YES SEWIO:NO";


            String[] szClassroomsToProcess = {
                   "DIR:C://IBSS//CLASSROOMS_2526// CLASSNAME:Pandas_2526 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
                    "10/14/2025,11/14/2025,12/15/2025,02/05/2026,03/17/2026,04/23/2026"+
                                 szClassroomSettings};


            

            processUL(szClassroomsToProcess);


            /*


            String szClassroomSettings = " MAP_PREFIX:StarFish_2526 REDENOISE:NO PROCESS:YES KALMAN:YES JUSTPLS:NO LABS:YES SEWIO:NO";


            String[] szClassroomsToProcess = {
    "DIR:C://IBSS//CLASSROOMS_2526// CLASSNAME:StarFish_2526 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:14 MINMAX:50 DAYS:" +
    "05/04/2026,05/12/2026,05/19/2026"+
    szClassroomSettings};*/






            /*
             * 
             *  
             *   
             *   
             *   
             *     String szClassroomSettings = " MAP_PREFIX:Pandas_2526 REDENOISE:NO PROCESS:YES KALMAN:YES JUSTPLS:NO LABS:YES SEWIO:NO";


            String[] szClassroomsToProcess = {
                   "DIR:C://IBSS//CLASSROOMS_2526// CLASSNAME:Pandas_2526 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
                    "10/14/2025,11/14/2025,12/15/2025"+
                                 szClassroomSettings};



            processUL(szClassroomsToProcess);


          String[]  szClassroomSettings = " MAP_PREFIX:BusyBees_2526 REDENOISE:NO PROCESS:YES KALMAN:YES JUSTPLS:NO LABS:YES SEWIO:NO";


            String[] szClassroomsToProcess = {
                   "DIR:C://IBSS//CLASSROOMS_2526// CLASSNAME:BusyBees_2526 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
                    "10/07/2025,11/04/2025,12/12/2025"+//10/07/2025"+//,11/04/2025,12/12/2025"+
                                 szClassroomSettings};



            processUL(szClassroomsToProcess); 
             
            
            String szClassroomSettings = " MAP_PREFIX:Room8 REDENOISE:NO PROCESS:YES KALMAN:YES JUSTPLS:NO LABS:YES SEWIO:NO";


             String[] szClassroomsToProcess = {
                   "DIR:C://IBSS//CLASSROOMS_2526// CLASSNAME:Room8 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
                    "09/24/2025,10/29/2025,12/10/2025,01/21/2026,03/05/2026,04/10/2026"+//+
                                 szClassroomSettings};

            
            String szClassroomSettings = " MAP_PREFIX:Room8_OUTSIDE REDENOISE:NO PROCESS:YES KALMAN:YES JUSTPLS:NO LABS:YES SEWIO:NO";


            String[] szClassroomsToProcess = {
                   "DIR:C://IBSS//CLASSROOMS_2526// CLASSNAME:Room8_OUTSIDE GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
                    "09/24/2025,10/29/2025,12/10/2025"+ szClassroomSettings};

            
            
          String szClassroomSettings = " MAP_PREFIX:APPLETREE_2526 REDENOISE:NO PROCESS:YES KALMAN:YES JUSTPLS:NO LABS:YES SEWIO:NO";
            String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2526// CLASSNAME:AppleTree_2526 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
      "09/05/2025,10/24/2025,12-05-2025,01-15-2026"+
       szClassroomSettings};
            processUL(szClassroomsToProcess);



             String szClassroomSettings = " MAP_PREFIX:BUBBLES_2526 REDENOISE:NO PROCESS:YES KALMAN:YES JUSTPLS:NO LABS:YES SEWIO:NO";

            String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2526// CLASSNAME:Bubbles_2526 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
      "09/10/2025,10/22/2025,12-08-2025,01-14-2026"+
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
                    ws.syncWhisper2223(classRoom, "SF2223BEEPS.csv", "BEEPSANDTIMESV2");//
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
                    //
                    //classRoom.processUbi(true);//DELETE DEBUG
                    if (toProcess)
                    {
                        if (classRoom.kalman)
                            classRoom.process(true, true);// (false, false);// (true, true);//DEBUG CHANGE
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
 
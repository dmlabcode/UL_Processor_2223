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

namespace UL_Processor_V2023
{
    class Program
    {
        static String szVersion = "";
        static void Main(string[] arguments)
        {
            String processType = "WHISPER";// "UL";

            String szClassroomSettings = " REDENOISE:NO PROCESS:YES JUSTPLS:NO LABS:YES";// YES";
            
            String[] szClassroomsToProcess = {
            "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:StarFish_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
            "01/30/2023" +
            //"10/19/2022,10/21/2022,11/14/2022,11/16/2022,12/05/2022,12/07/2022,01/30/2023,02/01/2023,03/13/2023,03/15/2023,04/17/2023,04/19/2023,06/15/2023" +
            szClassroomSettings};

            if (processType=="UL")
            {
                processUL(szClassroomsToProcess);
            }
            else if(processType=="WHISPER")
            {
                syncWhisper(szClassroomsToProcess);
            }
            
        }
        static void syncWhisper(string[] szClassroomsToProcess)
        { /*String[] szClassroomsToProcess = {
                   "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Room4_2324_OUTSIDE GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
                    "04/17/2024,05/08/2024"+ szClassroomSettings};*/
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
                ws.syncWhisper2223(classRoom);
                
               
            }

            Console.ReadLine();
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


                if (!classRoom.justPLS && szCustom=="1")
                {
                
                    classRoom.createReportDirs();
                    /*4 Clean ubi */
                    classRoom.ubiCleanup = classRoom.reDenoise ? true : classRoom.ubiCleanup;
                    if (classRoom.ubiCleanup)
                        classRoom.cleanUbiFiles();


                    //DELETE DEBUG 
                    //classRoom.kalman = false;
                    if (classRoom.kalman)
                    classRoom.denoise();

                    /* 5 Process */
                  
                    if (toProcess)
                    {
                        if(classRoom.kalman)
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
                else if(szCustom=="3")
                {
                    classRoom.processOutsideLenas();
                }

            }

            Console.ReadLine();
        }
    }
}

//String[] szClassroomsToProcess = { "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:StarFish_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:03/15/2023 HACKT1:NO ONSETS:YES TEN:YES VEL:NO ANGLES:YES SUMALL:YES ITS:YES GR:YES DBS:YES APPROACH:YES SOCIALONSETS:YES" };


/*  String[] szClassroomsToProcess = {
      "DIR:D://CLASSROOMS_2122// CLASSNAME:LEAP_AM_2122 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
      "10/25/2021,11/19/2021,12/03/2021,01/28/22,02/25/2022,03/29/2022,05/24/2022"+
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
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Bubbles_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "01/26/2024"+
       szClassroomSettings};

  String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:LEAP_AM_2223"+
       szClassroomSettings};

 String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:LittleFish_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       //"09-19-2022,09-21-2022,10-03-2022,10-04-2022,11-07-2022,01-09-2023,01-11-2023,02-24-2023,03-27-2023,03-29-2023,04-10-2023,04-12-2023,06-29-2023,07-20-2023"+
       "09-19-2022,09-21-2022,10-03-2022,10-04-2022,11-07-2022,01-09-2023,01-11-2023,02-24-2023,03-27-2023,03-29-2023,04-10-2023,04-12-2023,06-29-2023,07-20-2023"+
        szClassroomSettings};


 String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:ES_ROOM4_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "09/19/2022,09/21/2022,10/18/2022,10/20/2022,12/08/2022,12/09/2022,02/01/2023,02/09/2023,03/16/2023,03/17/2023,04/27/2023,04/28/2023,05/23/2023,05/25/2023"+
       szClassroomSettings};

 String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:LittleFish_2223 GRMIN:0.2 GRMAX:1.5 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       //"09-19-2022,09-21-2022,10-03-2022,10-04-2022,11-07-2022,01-09-2023,01-11-2023,02-24-2023,03-27-2023,03-29-2023,04-10-2023,04-12-2023,06-29-2023,07-20-2023"+
       "10-04-2022,02-24-2023,03-27-2023,03-29-2023,04-10-2023,04-12-2023"+
        szClassroomSettings};

String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:Room4_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "09/21/2022,10/18/2022,10/20/2022,12/08/2022,12/09/2022,02/01/2023,02/09/2023,03/16/2023,03/17/2023,04/27/2023,04/28/2023,05/25/2023"+
       szClassroomSettings};




String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:Avengers_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "09/20/2022,09/22/2022,11/01/2022,12/13/2022,01/12/2023,01/13/2023,02/14/2023,02/16/202g3,03/13/2023,03/14/2023,04/04/2023,04/06/2023,05/16/2023,05/18/2023"+
       szClassroomSettings};





String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2122// CLASSNAME:Room8_2122 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
       "12/13/2021,02/16/2022,03/16/2022"+//12/13/2021,01/24/2022,02/16/2022,03/16/2022"+
       szClassroomSettings};


String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Room4_2324_OUTSIDE GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
       "04/17/2024,05/08/2024"+//,10/07/2022,11/29/2022,12/12/2022,12/14/2022" +
       szClassroomSettings};


String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Room4_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
        "09/14/2023,10/12/2023,11/01/2023,12/08/2023,01/17/2024,02/14/2024,03/05/2024,04/17/2024,05/08/2024"+//,10/07/2022,11/29/2022,12/12/2022,12/14/2022" +
       // "10/12/2023"+
       szClassroomSettings};

String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:StarFish_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
       "10/19/2022,10/21/2022,11/14/2022,11/16/2022,12/05/2022,12/07/2022,01/30/2023,02/01/2023,03/13/2023,03/15/2023,04/17/2023,04/19/2023,06/15/2023"+
       szClassroomSettings};




*/
//6.815269687740454	4.164972799088159	0.25	6.856734733	3.9619128297947186
//7.360307737530511	4.655070106770157	0.5439706514243943	7.410467134588359	4.383312169016765
//46:59.9	10.305739870258385	2.1546635580997844	0.378853761	10.327892386068461	2.3489900677222906

/* double radians = -1.53418194;// 1.3693646451763588;
 double degrees = -87.90215016741;// 79.54239464896433276;// 78.458814783229485101;
 double thisDegrees = Convert.ToDouble(Convert.ToDouble(radians) * (180 / Math.PI));
 double pixl = 6.815269687740454;
 double piyl = 4.164972799088159;   
 double pixr = 6.856734733;
 double piyr = 3.9619128297947186;
 double piori_chaoming = Math.Atan2(piyr - piyl, pixr - pixl) / Math.PI * 180 + 90;

 piori_chaoming = piori_chaoming > 360 ? piori_chaoming - 360 : piori_chaoming;




 String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:Room4_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "09/21/2022,10/18/2022,10/20/2022,12/08/2022,12/09/2022,02/01/2023,02/09/2023,03/16/2023,03/17/2023,04/27/2023,04/28/2023,05/25/2023"+
       szClassroomSettings};
String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:StarFish_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
        "12/07/2023"+//,10/07/2022,11/29/2022,12/12/2022,12/14/2022" +
       szClassroomSettings};
  String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:Room4_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "09/21/2022,10/18/2022,10/20/2022,12/08/2022,12/09/2022,02/01/2023,02/09/2023,03/16/2023,03/17/2023,04/27/2023,04/28/2023,05/25/2023"+
       szClassroomSettings};
//ADD 5/18 01-26-2023
  String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:Room4_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "09/21/2022,10/18/2022,10/20/2022,12/08/2022,12/09/2022,02/01/2023,02/09/2023,03/16/2023,03/17/2023,04/27/2023,04/28/2023,05/25/2023"+
       szClassroomSettings};







/**********************************2223*****************************************


String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:StarFish_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
       "10/19/2022,10/21/2022,11/14/2022,11/16/2022,12/05/2022,12/07/2022,01/30/2023,02/01/2023,03/13/2023,03/15/2023,04/17/2023,04/19/2023,06/15/2023"+
       szClassroomSettings};
"09-13-2023,09-15-2023,10-03-2023,10-05-2023,11-01-2023,11-01-2023"+
"11-03-2023,12-04-2023,12-07-2023,01-09-2024,01-11-2024,02-23-2024,02-26-2024,"+
"03-01-2024,03-07-2024,04-22-2024,04-25-2024,05-02-2024,05-06-2024,06-13-2024"




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
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:LEAP_PM_2223 GRMIN:0.2 GRMAX:2 HRMIN:11 HRMAX:13 MINMAX:50 DAYS:" +
       "10/06/2022,10/07/2022,12/12/2022"+
       ","+
       "01/26/2023,02/08/2023,02/10/2023,03/10/2023,04/04/2023,04/06/2023"+
       szClassroomSettings};




String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:ES_ROOM8_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "10/25/2022,11/15/2022,12/15/2022,12/19/2022"+
       ",01/25/2023,01/27/2023,03/30/2023,03/31/2023,04/24/2023,04/25/2023,05/30/2023"+//,05/31/2023"+
       szClassroomSettings};


 String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:Avengers_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "09/20/2022,09/22/2022,11/01/2022,12/13/2022,01/12/2023,01/13/2023,02/14/2023,02/16/2023,03/13/2023,03/14/2023,04/04/2023,04/06/2023,05/16/2023,05/18/2023"+
       szClassroomSettings};

 String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:Turtles_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
       "10/17/2022,10/19/2022,11/17/2022,12/06/2022,02/28/2023,"+
       "03/02/2023,03/08/2023,03/10/2023,04/11/2023,04/14/2023,05/09/2023,05/10/2023"+//,06/01/2023,06/02/2023"+
       szClassroomSettings};

 String[] szClassroomsToProcess = {
     "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:LadyBugs_2223 GRMIN:0 GRMAX:1.5 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
     "09/12/2022,10/12/2022,10/31/2022,01/18/2023,01/20/2023,02/13/2023,02/15/2023"+
     ",04/26/2023,06/20/2023,06/22/2023,07/25/2023,07/27/2023" +
     szClassroomSettings};

 String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2223// CLASSNAME:Room4_2223 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "09/21/2022,10/18/2022,10/20/2022,12/08/2022,12/09/2022,02/01/2023,02/09/2023,03/16/2023,03/17/2023,04/27/2023,04/28/2023,05/25/2023"+
       szClassroomSettings}; 



/**************************************2324***********************
String szClassroomSettings = " REDENOISE:NO PROCESS:YES JUSTPLS:NO LABS:YES CUSTOM:3";// YES";
String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:AppleTree_Outside_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "11-29-2023,01-30-2024,02-27-2024,02-29-2024,03-18-2024"+
       //"09/28/2023,10/26/2023,11/30/2023,12/12/2023,01/25/2024,02/09/2024,02/23/2024,03/13/2024"+
       szClassroomSettings};


   String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Room8_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
        "09/26/2023,10/10/2023,11/02/2023,12/06/2023,01/24/2024,02/07/2024,03/20/2024,04/03/2024,05/15/2024"+//,10/07/2022,11/29/2022,12/12/2022,12/14/2022" +
       szClassroomSettings};

String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:LadyBugs GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
        "09/20/2023,09/22/2023,10/11/2023,10/13/2023,11/14/2023,11/16/2023,12/11/2023,12/14/2023,01/22/2024,01/25/2024,02/05/2024,02/08/2024,03/12/2024,03/14/2024,04/09/2024,04/11/2024,05/14/2024,05/16/2024,06/04/2024,06/06/2024,07/18/2024,07/24/2024"+//,10/07/2022,11/29/2022,12/12/2022,12/14/2022" +
       szClassroomSettings};

String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Room4_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
        "09/14/2023,10/12/2023,11/01/2023,12/08/2023,01/17/2024,02/14/2024,03/05/2024,04/17/2024,05/08/2024"+//,10/07/2022,11/29/2022,12/12/2022,12/14/2022" +
       // "10/12/2023"+
       szClassroomSettings};

String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:LEAP_AM GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:10 MINMAX:50 DAYS:" +
       "09/29/2023,10/24/2023,11/08/2023,12/15/2023,01/31/2024,02/13/2024,03/19/2024,04/02/2024,05/13/2024"+
       szClassroomSettings};

 String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:LEAP_PM GRMIN:0.2 GRMAX:2 HRMIN:11 HRMAX:13 MINMAX:50 DAYS:" +
       "09/29/2023,10/24/2023,11/08/2023,12/15/2023,01/31/2024,02/13/2024,03/19/2024,04/02/2024,05/13/2024"+
       szClassroomSettings};



  String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:AppleTree_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "09/28/2023,10/26/2023,11/30/2023,12/12/2023,01/25/2024,02/23/2024,03/13/2024,04/15/2024,05/17/2024,06/25/2024,07/09/2024"+
       szClassroomSettings};



String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Bubbles_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "04/05/2024,04/09/2024,05/28/2024,06/11/2024,07/16/2024"+// "01/26/2024,02/16/2024,04/05/2024,04/09/2024,05/28/2024,06/11/2024,07/16/2024"+
       szClassroomSettings};



  String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Room4_2324_OUTSIDE GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
        "04/17/2024,05/08/2024"+

       String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Bubbles_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "01/26/2024,02/16/2024,04/05/2024,04/09/2024,05/28/2024,06/11/2024,07/16/2024"+
       szClassroomSettings};

  String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Room8_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
        "09/26/2023,10/10/2023,11/02/2023,12/06/2023,01/24/2024,02/07/2024,03/20/2024,04/03/2024,05/15/2024"+//,10/07/2022,11/29/2022,12/12/2022,12/14/2022" +
       szClassroomSettings};




String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Turtles GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
       "09/07/2023,10/06/2023,11/28/2023,12/18/2023,01/10/2024,02/20/2024,03/11/2024,03/11/2024,04/16/2024,05/20/2024"+//,06/01/2023,06/02/2023"+
       szClassroomSettings};

String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Room8_2324_OUTSIDE GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:12 MINMAX:50 DAYS:" +
        "03/20/2024,04/03/2024,05/15/2024"+//,10/07/2022,11/29/2022,12/12/2022,12/14/2022" +
       szClassroomSettings};



  String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:AppleTree_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:11 MINMAX:50 DAYS:" +
       "09/28/2023,10/26/2023,11/30/2023,12/12/2023,01/25/2024,02/23/2024,03/13/2024,04/15/2024,05/17/2024,06/25/2024,07/09/2024"+
       szClassroomSettings};


String[] szClassroomsToProcess = {
       "DIR:C://IBSS//CLASSROOMS_2324// CLASSNAME:Avengers_2324 GRMIN:0.2 GRMAX:2 HRMIN:8 HRMAX:13 MINMAX:50 DAYS:" +
       "08/31/2023,09/19/2023,10/18/2023,11/17/2023,12/05/2023,01/29/2024,02/22/2024,03/07/2024,04/23/2024,05/22/2024"+//"08/31/2023,09/19/2023,10/18/2023,11/17/2023,12/05/2023,01/29/2024,02/22/2024,03/07/2024,04/23/2024,05/22/2024"+
       szClassroomSettings};
*/

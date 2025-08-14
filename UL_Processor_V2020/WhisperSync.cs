using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UL_Processor_V2023;
using static IronPython.Modules._ast;
using System.IO;
using System.Xml;
using IronPython.Compiler.Ast;
using static IronPython.Modules.PythonDateTime;
using System.Net.NetworkInformation;
using System.Web.UI;

namespace UL_Processor_V2023
{
    internal class subjectAudio
    {
        public DateTime lenaStart;
        public DateTime sonyStart;


    }
    internal class WhisperSync
    {
        public void syncWhisperTone(Classroom cr)
        {
            foreach (DateTime day in cr.classRoomDays)
            {
                ClassroomDay classRoomDay = new ClassroomDay(day);
                classRoomDay.setMappings(cr.dir, cr.mapPrefix, cr.personBaseMappings, cr.mapById, cr.startHour, cr.endHour, cr.endMinute);


                Dictionary<String, String> subjectSonyMap = new Dictionary<String, String>();
                String beepFile = cr.dir + "//TONEONSETS.csv";
                if (File.Exists(beepFile))
                {
                    using (StreamReader sr = new StreamReader(beepFile))
                    {
                       if (!sr.EndOfStream)
                       sr.ReadLine();

                //classRoomDay.setMappings(cr.dir, cr.className, cr.personBaseMappings, cr.mapById, cr.startHour, cr.endHour, cr.endMinute); 
                
                TextWriter sw = new StreamWriter(cr.dir + "//BEEPSANDTIMESV2" + Utilities.getDateStrMMDDYY(day) + ".csv");

                //Dictionary<String, subjectAudio> subjectAudioInfo = new Dictionary<string, subjectAudio>();
                        while (!sr.EndOfStream)
                        {
                            String szLine = sr.ReadLine();
                            String[] line = szLine.Split(',');
                            //-----  	beepFile	frequency	file_name	flag_start_time	flag_end_time	length	previousSeconds	absolute_start_time	min_flag_candidate_req	max_gap_time
                            //0 / mnt / hdd1 / IBSS / Debbie_School / StarFish / StarFish_2223 / 01 - 30 - 2023 / LENA_DATA / AUDIO  3000    20230201_014254_026848.wav  847.156 847.768 0.612   0   847.156 0.04    0.4

                            if (line.Length > 5 && line[0].Contains(Utilities.getDateDashStr(day)) && line[5].Trim()!="")
                            {
                                //DateTime datetimeLena = Convert.ToDateTime(line[3].Trim());
                                //DateTime datetimeSony = Convert.ToDateTime(line[5].Trim());
                                //String subjectId = line[4].Trim();
                                String mapSonyId = line[2].Trim();  
                                PersonDayInfo pdi = classRoomDay.getPersonInfoBySony(mapSonyId);
                                //subjectAudio subjectAudio = new subjectAudio();
                                //subjectAudio.lenaStart = datetimeLena;
                                //subjectAudio.sonyStart = datetimeSony;
                                if (!subjectSonyMap.ContainsKey(pdi.mapId))
                                {
                                    //subjectAudioInfo.Add(pdi.mapId,subjectAudio);
                                    subjectSonyMap.Add( pdi.mapId, line[5].Trim());
                                }
                            }
                        }
                    }

                    getUbiInteractions(cr, classRoomDay, subjectSonyMap);

                }

                  


            }


        }
        public void syncWhisper2223(Classroom cr)
        {
            foreach (DateTime day in cr.classRoomDays)
            {
                TextWriter sw = new StreamWriter(cr.dir + "//BEEPSANDTIMESV2" + Utilities.getDateStrMMDDYY(day) + ".csv");

                double minLenaOnset = -1;
                String minLenaOnsetFile = "";
                DateTime minLenaStartTime = DateTime.MinValue;
                List<String> newBeepLines = new List<String>();
                Dictionary<String,String> subjectSonyMap = new Dictionary<String,String>();
                ClassroomDay classRoomDay = new ClassroomDay(day);
                classRoomDay.setMappings(cr.dir, cr.className, cr.personBaseMappings, cr.mapById, cr.startHour, cr.endHour, cr.endMinute);

                String beepFile = cr.dir + "//SF2223BEEPS.csv";
                if (File.Exists(beepFile))
                {
                    getMinOnset(beepFile, cr, classRoomDay, day, ref minLenaOnset, ref minLenaOnsetFile, ref minLenaStartTime, ref newBeepLines, ref sw);
                    setAudioStartTimes(cr,classRoomDay, day, minLenaOnset, minLenaOnsetFile, ref newBeepLines, ref sw, ref subjectSonyMap);
                    sw.Close();
                    getUbiInteractions(cr,classRoomDay, subjectSonyMap);

                    
                }

                 
            }
        

        }

        public void getUbiInteractions(Classroom cr, ClassroomDay classRoomDay, Dictionary<String, String> subjectSonyMap)
        {
            classRoomDay.makeTimeDict(cr.dir);
            if (Directory.Exists(cr.dir + "//" + Utilities.getDateDashStr(classRoomDay.classDay) + "//Whisper_Data"))
            {
                if (!Directory.Exists(cr.dir + "//" + Utilities.getDateDashStr(classRoomDay.classDay) + "//WhisperV2_Ubi_Data"))
                {
                    Directory.CreateDirectory(cr.dir + "//" + Utilities.getDateDashStr(classRoomDay.classDay) + "//WhisperV2_Ubi_Data");
                }

                foreach (String file in Directory.GetFiles(cr.dir + "//" + Utilities.getDateDashStr(classRoomDay.classDay) + "//Whisper_Data"))
                {
                    
                    String mapSonyId = file.Substring(file.IndexOf("\\") + 2, 2);
                    PersonDayInfo pdi = classRoomDay.getPersonInfoBySony(mapSonyId);
                    TextWriter sw = new StreamWriter(file.Replace("AST", pdi.mapId).Replace("//Whisper_Data", "//WhisperV2_Ubi_Data"));
                    if (subjectSonyMap.ContainsKey(pdi.mapId))
                    {
                        DateTime subjectSonyStartTime = Utilities.getDate(subjectSonyMap[pdi.mapId]) ;
                        subjectSonyStartTime = new DateTime(classRoomDay.classDay.Year, classRoomDay.classDay.Month, classRoomDay.classDay.Day, subjectSonyStartTime.Hour, subjectSonyStartTime.Minute, subjectSonyStartTime.Second, subjectSonyStartTime.Millisecond);

                        using (StreamReader sr = new StreamReader(file))
                        {
                            if (!sr.EndOfStream)
                            {
                                sw.Write(sr.ReadLine() + ",KIDSOCIALCONTACT,ADULTSOCIALCONTACT,TEACHERSOCIALCONTACT,STARTTIMESTAMP,ENDTIMESTAMP,");
                                foreach(String s in cr.personBaseMappings.Keys)
                                    sw.Write(s + ",");
                                
                                sw.WriteLine("");

                            }

                            /*
                             * 	start_sec	end_sec	lang	language_t3	probability_t3	sentence
0	0	10.3	en	['en', 'it', 'la']	{'en': 0.8521786332130432, 'it': 0.06686361134052277, 'la': 0.04212208092212677}	 A4320AM, this is a Linear T3 being used by Subjet T3 and Sony A75.
1	10.3	13.3	en	['en', 'es', 'fi']	{'en': 0.9219698309898376, 'es': 0.021056130528450012, 'fi': 0.012256273068487644}	 Starfish Debi, January 30th, 2023.
*/
                            while (!sr.EndOfStream)
                            {
                                String szLine = sr.ReadLine();
                                String[] line = szLine.Split(',');
                                
                                if (line.Length > 6 )
                                {
                                    double startSec = Convert.ToDouble(line[1]);
                                    double endSec = Convert.ToDouble(line[2]);

                                    DateTime startTime=subjectSonyStartTime.AddSeconds(startSec);
                                    DateTime endTime = subjectSonyStartTime.AddSeconds(endSec);

                                    DateTime t=startTime;
                                    Boolean inSocialContactWithKid = false;
                                    Boolean inSocialContactWithAdult = false;
                                    Boolean inSocialContactWithTeacher = false;

                                    List<String> inSocialContact = new List<string>();

                                    while (t<endTime)
                                    {
                                        if(classRoomDay.ubiTenths.ContainsKey(t) && classRoomDay.ubiTenths[t].ContainsKey(pdi.mapId))
                                        {
                                            foreach(String subject in classRoomDay.ubiTenths[t].Keys)
                                            {
                                                if(subject!=pdi.mapId)
                                                {
                                                    double dist = Utilities.calcSquaredDist(classRoomDay.ubiTenths[t][subject], classRoomDay.ubiTenths[t][pdi.mapId]);
                                                    Boolean withinGofR = (dist <= (cr.grMax * cr.grMax)) && (dist >= (cr.grMin * cr.grMin));
                                                     


                                                    Tuple<double, double> angles = Utilities.withinOrientationData(classRoomDay.ubiTenths[t][subject], classRoomDay.ubiTenths[t][pdi.mapId]);
                                                    Boolean orientedCloseness = withinGofR && ((Math.Abs(angles.Item1) <= 45 && Math.Abs(angles.Item2) <= 45));
                                                    if(withinGofR)
                                                    {
                                                        if (orientedCloseness)
                                                        {
                                                            if (!inSocialContact.Contains(subject))
                                                            {
                                                                inSocialContact.Add(subject);
                                                            }
                                                            
                                                            if (cr.personBaseMappings[subject].subjectType == "CHILD")
                                                            {
                                                                inSocialContactWithKid = true;
                                                                 
                                                            }
                                                            else
                                                            {
                                                                inSocialContactWithAdult = true;
                                                                if (cr.personBaseMappings[subject].subjectType == "TEACHER")
                                                                {
                                                                    inSocialContactWithTeacher = true;
                                                                     
                                                                }
                                                            }

                                                        }
                                                    }
                                                   // if (inSocialContactWithKid && inSocialContactWithAdult && inSocialContactWithTeacher)
                                                    {
                                                     //   break;
                                                    }
                                                }
                                                 
                                            }
                                        }
                                        if (inSocialContactWithKid && inSocialContactWithAdult && inSocialContactWithTeacher)
                                        {
                                            t = endTime;
                                        }
                                        else
                                        {
                                            t = t.AddMilliseconds(100);
                                        }

                                    }
                                    sw.Write(szLine + "," + inSocialContactWithKid + "," + inSocialContactWithAdult + "," + inSocialContactWithTeacher+","+
                                        startTime.Hour+":"+startTime.Minute+":"+startTime.Second+"."+startTime.Millisecond + "," +
                                        endTime.Hour + ":" + endTime.Minute + ":" + endTime.Second + "." + endTime.Millisecond+",");

                                    foreach (String s in cr.personBaseMappings.Keys)
                                    {
                                        if(s!= pdi.mapId  && classRoomDay.personBaseMappings.ContainsKey(s) &&   classRoomDay.personDayMappings.ContainsKey(s) && classRoomDay.personDayMappings[s].present)
                                        {
                                            sw.Write((inSocialContact.Contains(s) ? "TRUE" : "FALSE") + ",");
                                        }
                                        else
                                        {
                                            sw.Write("NA,");
                                        }
                                    }

                                    sw.WriteLine("");
                                }
                            }
                        }
                     }
                    sw.Close();

                }
            }

        }
        public void setAudioStartTimes(Classroom cr, ClassroomDay classRoomDay, DateTime day, double minLenaOnset, String minLenaOnsetFile, ref List<String> newBeepLines, ref TextWriter sw, ref Dictionary<String,String> subjectSonyMap)
        {
            DateTime minLenaStartTime = new DateTime();
            if (File.Exists(cr.dir + "//" + Utilities.getDateDashStr(day) + "//LENA_Data//ITS//" + minLenaOnsetFile.Replace(".wav", ".its")))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(cr.dir + "//" + Utilities.getDateDashStr(day) + "//LENA_Data//ITS//" + minLenaOnsetFile.Replace(".wav", ".its"));
                XmlNodeList rec = doc.ChildNodes[0].SelectNodes("ProcessingUnit/Recording");
                foreach (XmlNode recording in rec)
                {

                    double recStartSecs = Convert.ToDouble(recording.Attributes["startTime"].Value.Substring(2, recording.Attributes["startTime"].Value.Length - 3));
                    DateTime recStartTime = DateTime.Parse(recording.Attributes["startClockTime"].Value);
                    var est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    recStartTime = TimeZoneInfo.ConvertTime(recStartTime, est);

                    if (Utilities.isSameDay(recStartTime, day) &&
                            //recStartTime.Hour >= startHour &&
                            (recStartTime.Hour < cr.endHour ||
                                (recStartTime.Hour == cr.endHour &&
                                    recStartTime.Minute <= cr.endMinute
                                )
                            )
                        )
                    {
                        minLenaStartTime = recStartTime;
                        break;
                    }
                }


                foreach (String szline in newBeepLines)
                {
                    String[] line = szline.Split( ',' );
                    double currentOnset = Convert.ToDouble(line[8]);
                    DateTime currentStartTime = minLenaStartTime;
                    if (currentOnset != minLenaOnset)
                    {
                        currentStartTime = minLenaStartTime.AddSeconds(currentOnset - minLenaOnset);
                    }
                    currentStartTime= new DateTime(classRoomDay.classDay.Year, classRoomDay.classDay.Month, classRoomDay.classDay.Day, currentStartTime.Hour, currentStartTime.Minute, currentStartTime.Second,currentStartTime.Millisecond);   
                    sw.WriteLine(string.Join(",", line) + "," + currentStartTime.ToLongTimeString() );

                    
                    if (line[12]!="" &&(!subjectSonyMap.ContainsKey(line[12])))
                    {
                        subjectSonyMap.Add(line[12],currentStartTime.ToLongTimeString()); 
                    }
                }
            } 
        }


        public void getMinOnset(String beepFile, Classroom cr, ClassroomDay classRoomDay, DateTime day, ref double minLenaOnset, ref String minLenaOnsetFile,ref DateTime minLenaStartTime,ref List<String> newBeepLines, ref TextWriter sw)
        {

             
            using (StreamReader sr = new StreamReader(beepFile))
            {
                if (!sr.EndOfStream)
                    sw.WriteLine(sr.ReadLine() + ",AUDIOTYPE,SUBJECTID,RECSTARTTIME");
                while (!sr.EndOfStream)
                {
                    String szLine = sr.ReadLine();
                    String[] line = szLine.Split(',');
                    //-----  	beepFile	frequency	file_name	flag_start_time	flag_end_time	length	previousSeconds	absolute_start_time	min_flag_candidate_req	max_gap_time
                    //0 / mnt / hdd1 / IBSS / Debbie_School / StarFish / StarFish_2223 / 01 - 30 - 2023 / LENA_DATA / AUDIO  3000    20230201_014254_026848.wav  847.156 847.768 0.612   0   847.156 0.04    0.4

                    if (line.Length > 9 && line[1].Contains(Utilities.getDateDashStr(day)))
                    {
                        double onsetSecs = Convert.ToDouble(line[8]);
                        String audioType = line[3].Trim().Length > 11 ? "LENA" : "SONY";
                        if (audioType == "LENA"  && File.Exists(cr.dir + "//" + Utilities.getDateDashStr(day) + "//LENA_Data//ITS//" + line[3].Trim().Replace(".wav", ".its")))
                        {
                            if (minLenaOnset == -1 || minLenaOnset > onsetSecs)
                            {
                                minLenaOnset = onsetSecs;
                                minLenaOnsetFile = line[3].Trim();
                            }
                        }
                        if(audioType=="SONY")
                        {
                            bool stopHere = true;
                        }
                        String szSonyId = audioType == "SONY" ? line[3].Replace(".wav", "") : "";
                        String mapSonyId = audioType == "SONY" ? szSonyId.Substring(1, 2) : "";
                        PersonDayInfo pdi = classRoomDay.getPersonInfoBySony(mapSonyId);

                        newBeepLines.Add(szLine+","+ audioType+","+pdi.mapId);
                    }
                }
            }


        }
    }
}



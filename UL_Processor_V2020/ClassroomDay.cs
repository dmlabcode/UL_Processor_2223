using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

using System.Diagnostics;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

using Newtonsoft.Json;
using static IronPython.Modules._ast;
using static IronPython.Modules.PythonDateTime;
using UL_Processor_V2020;
using static IronPython.Runtime.Profiler;
using static IronPython.SQLite.PythonSQLite;
using IronPython.Runtime.Operations;
using IronPython.Compiler;
using static IronPython.Modules.ArrayModule;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Web.UI;
using IronPython.Runtime;

namespace UL_Processor_V2023
{
    
    public class ClassroomDay
    {
        public DateTime classDay;
        public Dictionary<String, Person> personBaseMappings = new Dictionary<string, Person>();
        public Dictionary<String, PersonDayInfo> personDayMappings = new Dictionary<string, PersonDayInfo>();
        public Dictionary<String, List<LenaOnset>> lenaOnsets = new Dictionary<string, List<LenaOnset>>();
        public Dictionary<DateTime, Dictionary<String, PersonSuperInfo>> ubiTenths = new Dictionary<DateTime, Dictionary<string, PersonSuperInfo>>();
         
        public double recSecs = 0;
        public Boolean mappingsSet = false;

        public Boolean toFilter = false;
        public double maxZ = 1.25;
        public double maxGapSecs = 60;
       
        public ClassroomDay(DateTime day)
        {
            classDay = day;
        }
       
        public void writeSocialOnsetData(String className, String szOutputFile, List<String> diagnosisList, List<String> languagesList)
        { 
            TextWriter sw = new StreamWriter(szOutputFile);// countDays > 0);
            /*sw.WriteLine("File,Date,Subject,LenaID,SubjectType,conversationid," +
                "voctype,recstart,startsec,endsec,starttime,endtime,duration," +
                "seg_duration,wordcount,avg_db,avg_peak,turn_taking"
                //",logActivities,children,teachers");//,children,teachers"
                );*/



            sw.WriteLine("File,Date,Subject,LenaID,SubjectType,Conversation_Id,voctype,recstart,startsec,endsec,starttime,endtime,duration,seg_duration,wordcount,avg_db,avg_peak,turn_taking,in_social_contact_talk, outside_kid_social_contact_and_subject_talking, in_social_contact_and_subject_talking ");

            

            foreach (String bid in lenaOnsets.Keys)
            {
                foreach (LenaOnset lo in lenaOnsets[bid])
                {
                     sw.WriteLine( 
                       lo.itsFile+ "," +
                                                                                            classDay + "," +
                                                                                            lo.id + "," +
                                                                                            lo.lenaId+"," +
                                                                                            lo.subjectType + "," +
                                                                                            lo.conversationid+"," +
                                                                                            lo.type + "," +
                                                                                            Utilities.getTimeStr(lo.recStartTime) + "," +
                                                                                            lo.startSec + "," +
                                                                                            lo.endSec + "," +
                                                                                            Utilities.getTimeStr(lo.startTime) + "," +
                                                                                            Utilities.getTimeStr(lo.endTime) + "," +
                                                                                            String.Format("{0:0.00}", lo.durSecs) + "," +
                                                                                            String.Format("{0:0.00}", lo.segmentDurSecs) + "," +
                                                                                            String.Format("{0:0.00}", lo.count) +","+
                                                                                            String.Format("{0:0.00}", lo.avgDb) + "," +
                                                                                            String.Format("{0:0.00}", lo.peakDb) + "," +
                                                                                            String.Format("{0:0.00}", lo.tc)+","+
                                                                                            (lo.inSocialContactAnyTalking?"YES":"NO")+","+
                                                                                            (lo.outsideKidsSocialContactSubjectTalking ? "YES" : "NO") + "," +
                                                                                            (lo.inSocialContactSubjectTalking ? "YES" : "NO"));


                }

            }
            sw.Close();
        }
        public void writePairActivityData(Dictionary<String, Pair> pairs, String className, String szOutputFile, List<String> diagnosisList, List<String> languagesList)
        {
            TextWriter sw = new StreamWriter(szOutputFile,false);


            String szHeader =
"Date,Subject,Partner,SubjectShortID,PartnerShortID,SubjectDiagnosis,PartnerDiagnosis,SubjectLanguage,PartnerLanguage," +
"SubjectGender,PartnerGender,Adult,SubjectStatus,PartnerStatus,SubjectType,PartnerType," +
"Input1_pvc_or_sac,Input2_pvc_or_stc," +
"Input3_dur_pvd_or_uttl,";


            szHeader += "PairBlockTalking,PairTalkingDuration," +
//taken out Subject-Talking-Duration-From_Start,"+Partner-Talking-Duration-From-Start,
"Subject-Talking-Duration-Evenly-Spread,Partner-Talking-Duration-Evenly-Spread," +
"A_Subject-Talking-Duration-Evenly-Spread,A_Partner-Talking-Duration-Evenly-Spread," +
"AL_Subject-Talking-Duration-Evenly-Spread,AL_Partner-Talking-Duration-Evenly-Spread," +
"SubjectTurnCount,PartnerTurnCount,SubjectVocCount,PartnerVocCount," +
"A_SubjectVocCount,A_PartnerVocCount,"+
"AL_SubjectVocCount,AL_PartnerVocCount,"+
"SubjectAdultCount,PartnerAdultCount,SubjectNoise," +
"PartnerNoise,SubjectOLN,PartnerOLN,SubjectCry,PartnerCry,SubjectJoinedCry,PartnerJoinedCry,JoinedCry,";
             
        
            szHeader += "PairProximityDuration," +
"PairOrientation-ProximityDuration,SharedTimeinClassroom,SubjectTime,PartnerTime,TotalRecordingTime,"+
"WUBITotalVD,TotalVD,PartnerWUBITotalVD,PartnerTotalVD,"+
"A_WUBITotalVD,A_TotalVD,A_PartnerWUBITotalVD,A_PartnerTotalVD," +
"AL_WUBITotalVD,AL_TotalVD,AL_PartnerWUBITotalVD,AL_PartnerTotalVD," +
"WUBITotalVC,TotalVC,PartnerWUBITotalVC,PartnerTotalVC," +
"A_WUBITotalVC,AL_TotalVC,A_PartnerWUBITotalVC,A_PartnerTotalVC," +
"AL_WUBITotalVC,AL_TotalVC,AL_PartnerWUBITotalVC,AL_PartnerTotalVC," +
"WUBITotalTC,TotalTC,PartnerWUBITotalTC," +
"PartnerTotalTC,WUBITotalAC,TotalAC,PartnerWUBITotalAC,PartnerTotalAC,WUBITotalNO,TotalNO,PartnerWUBITotalNO,PartnerTotalNO,"+
"WUBITotalOLN,TotalOLN,PartnerWUBITotalOLN,PartnerTotalOLN,WUBITotalCRY,TotalCRY,PartnerWUBITotalCRY,PartnerTotalCRY,"+
"WUBITotalAV_DB,TotalAV_DB,PartnerWUBITotalAV_DB,PartnerTotalAV_DB,WUBITotalAV_PEAK_DB,TotalAV_PEAK_DB,PartnerWUBITotalAV_PEAK_DB,PartnerTotalAV_PEAK_DB,CLASSROOM";
            //82//

            String newDiagnosis = "";
            foreach(String d in diagnosisList)
            {
                newDiagnosis += ("Subject"+d + ",Partner" + d + ",");
            }
            String newLanguages = "";
            foreach (String l in languagesList)
            {
                newLanguages += ("Subject" + l + ",Partner" + l + ",");
            }

            newDiagnosis= newDiagnosis=="SubjectDiagnosis,PartnerDiagnosis,SubjectLanguage,PartnerLanguage,"? "SubjectDiagnosis,PartnerDiagnosis," : newDiagnosis;
            newLanguages = newLanguages == "" ? "SubjectLanguage,PartnerLanguage," : newLanguages;
            szHeader=szHeader.Replace("SubjectDiagnosis,PartnerDiagnosis,", newDiagnosis);
            szHeader=szHeader.Replace("SubjectLanguage,PartnerLanguage,", newLanguages);

            sw.WriteLine(szHeader);

            foreach (String szPair in pairs.Keys)
            {
                String szSubject = szPair.Split('|')[0];
                String szPartner = szPair.Split('|')[1];
                Person subject = personBaseMappings[szSubject];
                Person partner= personBaseMappings[szPartner];
                Pair pair = pairs[szPair];
                PersonDayInfo sdi = Utilities.getPerson(personDayMappings, szSubject);
                PersonDayInfo pdi = Utilities.getPerson(personDayMappings, szPartner);
                
                LenaVars subjectLenaVarsInContact = pair.subjectLenaVarsInContact;
                LenaVars partnerLenaVarsInContact = pair.partnerLenaVarsInContact;
 
                LenaVars subjectLenaVarsInWUBI = personDayMappings[szSubject].WUBILenaVars;
                LenaVars partnerLenaVarsInWUBI = personDayMappings[szPartner].WUBILenaVars;
                LenaVars subjectLenaVarsInTotal = personDayMappings[szSubject].totalLenaVars;
                LenaVars partnerLenaVarsInTotal = personDayMappings[szPartner].totalLenaVars;


                String newPairDiagnosis = "";
                String newPairDiagnosisP = "";
                int pos = 0;


                foreach (String d in subject.diagnosisList)
                {
                    newPairDiagnosis += (d + "," + (partner.diagnosisList.Count>pos? partner.diagnosisList[pos]:"") + ",");
                    newPairDiagnosisP += ((partner.diagnosisList.Count > pos ? partner.diagnosisList[pos] : "") + "," + d + ",");
                    pos++;
                }
                String newPairLanguages = "";
                String newPairLanguagesP = "";
                pos = 0;
                foreach (String l in subject.languagesList)
                {
                    newPairLanguages +=  ( l + "," + (partner.languagesList.Count > pos ? partner.languagesList[pos] : "") + ",");
                    newPairLanguagesP += ((partner.languagesList.Count > pos ? partner.languagesList[pos] : "") + "," + l+ ",");
                    pos++;
                }

                String szSw = this.classDay.Month + "/" + this.classDay.Day + "/" + this.classDay.Year + "," +
                subject.longId + "," +
                partner.longId + "," +
                subject.shortId + "," +
                partner.shortId + "," +
                newPairDiagnosis +
                newPairLanguages +
                subject.gender + "," +
                partner.gender + "," +
                (!(subject.subjectType.ToUpper().Trim() == "CHILD" && partner.subjectType.ToUpper().Trim() == "CHILD")) + "," +
                sdi.status + "," +
                pdi.status + "," +
                subject.subjectType + "," +
                partner.subjectType + "," +
                (sdi.status.ToUpper() == "PRESENT" && pdi.status.ToUpper() == "PRESENT" ? (partner.subjectType.ToUpper() == "CHILD" ? partnerLenaVarsInContact.totalChildUttCount.ToString() : subjectLenaVarsInContact.totalAdultWordCount.ToString()) : "NA") + "," +
                (sdi.status.ToUpper() == "PRESENT" && pdi.status.ToUpper() == "PRESENT" ? (partner.subjectType.ToUpper() == "CHILD" ? partnerLenaVarsInContact.totalChildUttCount.ToString() : subjectLenaVarsInContact.totalTurnCounts.ToString()) : "NA") + "," +
                (sdi.status.ToUpper() == "PRESENT" && pdi.status.ToUpper() == "PRESENT" ? (partner.subjectType.ToUpper() == "CHILD" ? partnerLenaVarsInContact.totalChildUttDuration.ToString() : subjectLenaVarsInContact.totalAdultWordCount.ToString()) : "NA") + ",";


                szSw += pair.pairBlockTalking + "," +
                pair.pairProxOriDuration + "," +//partnerLenaVarsInContact.totalChildUttDuration + "," +
                                                 //"NA," +
                                                 //"NA," +
                subjectLenaVarsInContact.totalChildUttDuration + "," +
                partnerLenaVarsInContact.totalChildUttDuration + "," +

                subjectLenaVarsInContact.totalKchiDur + "," +
                partnerLenaVarsInContact.totalKchiDur + "," +
                subjectLenaVarsInContact.totalKchiDurWLENA + "," +
                partnerLenaVarsInContact.totalKchiDurWLENA + "," +

                subjectLenaVarsInContact.totalTurnCounts + "," +
                partnerLenaVarsInContact.totalTurnCounts + "," +
                subjectLenaVarsInContact.totalChildUttCount + "," +
                partnerLenaVarsInContact.totalChildUttCount + "," +


                subjectLenaVarsInContact.totalKchiCount + "," +
                partnerLenaVarsInContact.totalKchiCount + "," +
                subjectLenaVarsInContact.totalKchiCountWLENA + "," +
                partnerLenaVarsInContact.totalKchiCountWLENA + "," +

                subjectLenaVarsInContact.totalAdultWordCount + "," +
                partnerLenaVarsInContact.totalAdultWordCount + "," +
                subjectLenaVarsInContact.totalNoise + "," +
                partnerLenaVarsInContact.totalNoise + "," +
              subjectLenaVarsInContact.totalOLN + "," +
                partnerLenaVarsInContact.totalOLN + "," +
                subjectLenaVarsInContact.totalChildCryDuration + "," +
                partnerLenaVarsInContact.totalChildCryDuration + "," +


                pair.partnerJoinedCry + "," +
                pair.subjectJoinedCry + "," +
                pair.joinedCry + ",";


                szSw +=pair.pairProxDuration + "," +
                pair.pairProxOriDuration + "," +
                pair.sharedTimeInSecs + "," +
                pair.subjectTotalTimeInSecs + "," +
                pair.partnerTotalTimeInSecs + "," +
                recSecs + "," +
                subjectLenaVarsInWUBI.totalChildUttDuration + "," +
                subjectLenaVarsInTotal.totalChildUttDuration + "," +
                partnerLenaVarsInWUBI.totalChildUttDuration + "," +
                partnerLenaVarsInTotal.totalChildUttDuration + "," +

                subjectLenaVarsInWUBI.totalKchiDur + "," +
                subjectLenaVarsInTotal.totalKchiDur + "," +
                partnerLenaVarsInWUBI.totalKchiDur + "," +
                partnerLenaVarsInTotal.totalKchiDur + "," +
                subjectLenaVarsInWUBI.totalKchiDurWLENA + "," +
                subjectLenaVarsInTotal.totalKchiDurWLENA + "," +
                partnerLenaVarsInWUBI.totalKchiDurWLENA + "," +
                partnerLenaVarsInTotal.totalKchiDurWLENA + "," +

                subjectLenaVarsInWUBI.totalChildUttCount + "," +
                subjectLenaVarsInTotal.totalChildUttCount + "," +
                partnerLenaVarsInWUBI.totalChildUttCount + "," +
                partnerLenaVarsInTotal.totalChildUttCount + "," +

                subjectLenaVarsInWUBI.totalKchiCount + "," +
                subjectLenaVarsInTotal.totalKchiCount + "," +
                partnerLenaVarsInWUBI.totalKchiCount + "," +
                partnerLenaVarsInTotal.totalKchiCount + "," +
                subjectLenaVarsInWUBI.totalKchiCountWLENA + "," +
                subjectLenaVarsInTotal.totalKchiCountWLENA + "," +
                partnerLenaVarsInWUBI.totalKchiCountWLENA + "," +
                partnerLenaVarsInTotal.totalKchiCountWLENA + "," +


                subjectLenaVarsInWUBI.totalTurnCounts + "," +
                subjectLenaVarsInTotal.totalTurnCounts + "," +
                partnerLenaVarsInWUBI.totalTurnCounts + "," +
                partnerLenaVarsInTotal.totalTurnCounts + "," +

                subjectLenaVarsInWUBI.totalAdultWordCount + "," +
                subjectLenaVarsInTotal.totalAdultWordCount + "," +
                partnerLenaVarsInWUBI.totalAdultWordCount + "," +
                partnerLenaVarsInTotal.totalAdultWordCount + "," +

                subjectLenaVarsInWUBI.totalNoise + "," +
                subjectLenaVarsInTotal.totalNoise + "," +
                partnerLenaVarsInWUBI.totalNoise + "," +
                partnerLenaVarsInTotal.totalNoise + "," +

                subjectLenaVarsInWUBI.totalOLN + "," +
                subjectLenaVarsInTotal.totalOLN + "," +
                partnerLenaVarsInWUBI.totalOLN + "," +
                partnerLenaVarsInTotal.totalOLN + "," +

                subjectLenaVarsInWUBI.totalChildCryDuration + "," +
                subjectLenaVarsInTotal.totalChildCryDuration + "," +
                partnerLenaVarsInWUBI.totalChildCryDuration + "," +
                partnerLenaVarsInTotal.totalChildCryDuration + "," +


                (subjectLenaVarsInWUBI.avgDb != 0 && subjectLenaVarsInWUBI.totalSegments != 0 ? subjectLenaVarsInWUBI.avgDb / subjectLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
                (subjectLenaVarsInTotal.avgDb != 0 && subjectLenaVarsInTotal.totalSegments != 0 ? subjectLenaVarsInTotal.avgDb / subjectLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +
                (partnerLenaVarsInWUBI.avgDb != 0 && partnerLenaVarsInWUBI.totalSegments != 0 ? partnerLenaVarsInWUBI.avgDb / partnerLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
                (partnerLenaVarsInTotal.avgDb != 0 && partnerLenaVarsInTotal.totalSegments != 0 ? partnerLenaVarsInTotal.avgDb / partnerLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +

                (subjectLenaVarsInWUBI.maxDb != 0 && subjectLenaVarsInWUBI.totalSegments != 0 ? subjectLenaVarsInWUBI.maxDb / subjectLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
                (subjectLenaVarsInTotal.maxDb != 0 && subjectLenaVarsInTotal.totalSegments != 0 ? subjectLenaVarsInTotal.maxDb / subjectLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +
                (partnerLenaVarsInWUBI.maxDb != 0 && partnerLenaVarsInWUBI.totalSegments != 0 ? partnerLenaVarsInWUBI.maxDb / partnerLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
                (partnerLenaVarsInTotal.maxDb != 0 && partnerLenaVarsInTotal.totalSegments != 0 ? partnerLenaVarsInTotal.maxDb / partnerLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +
                className;

                sw.WriteLine(szSw);







                szSubject = szPair.Split('|')[1];
                szPartner = szPair.Split('|')[0];
                subject = personBaseMappings[szSubject];
                partner = personBaseMappings[szPartner];
                sdi = Utilities.getPerson(personDayMappings, szSubject);
                pdi = Utilities.getPerson(personDayMappings, szPartner);

                subjectLenaVarsInContact = pair.partnerLenaVarsInContact;
                partnerLenaVarsInContact = pair.subjectLenaVarsInContact;
                

                //szSubject
                subjectLenaVarsInWUBI = personDayMappings[szSubject].WUBILenaVars;
                partnerLenaVarsInWUBI = personDayMappings[szPartner].WUBILenaVars;
                subjectLenaVarsInTotal = personDayMappings[szSubject].totalLenaVars;
                partnerLenaVarsInTotal = personDayMappings[szPartner].totalLenaVars;


                szSw = this.classDay.Month + "/" + this.classDay.Day + "/" + this.classDay.Year + "," +
               subject.longId + "," +
               partner.longId + "," +
               subject.shortId + "," +
               partner.shortId + "," +
                newPairDiagnosisP +
                newPairLanguagesP +
               subject.gender + "," +
               partner.gender + "," +
               (!(subject.subjectType.ToUpper().Trim() == "CHILD" && partner.subjectType.ToUpper().Trim() == "CHILD")) + "," +
               sdi.status + "," +
               pdi.status + "," +
               subject.subjectType + "," +
               partner.subjectType + "," +
               (sdi.status.ToUpper() == "PRESENT" && pdi.status.ToUpper() == "PRESENT" ? (partner.subjectType.ToUpper() == "CHILD" ? partnerLenaVarsInContact.totalChildUttCount.ToString() : subjectLenaVarsInContact.totalAdultWordCount.ToString()) : "NA") + "," +
               (sdi.status.ToUpper() == "PRESENT" && pdi.status.ToUpper() == "PRESENT" ? (partner.subjectType.ToUpper() == "CHILD" ? partnerLenaVarsInContact.totalChildUttCount.ToString() : subjectLenaVarsInContact.totalTurnCounts.ToString()) : "NA") + "," +
               (sdi.status.ToUpper() == "PRESENT" && pdi.status.ToUpper() == "PRESENT" ? (partner.subjectType.ToUpper() == "CHILD" ? partnerLenaVarsInContact.totalChildUttDuration.ToString() : subjectLenaVarsInContact.totalAdultWordCount.ToString()) : "NA") + ",";


                szSw += pair.pairBlockTalking + "," +
                pair.pairProxOriDuration + "," +//partnerLenaVarsInContact.totalChildUttDuration + "," +
                                                //"NA," +
                                                //"NA," +
                subjectLenaVarsInContact.totalChildUttDuration + "," +
                partnerLenaVarsInContact.totalChildUttDuration + "," +

                subjectLenaVarsInContact.totalKchiDur + "," +
                partnerLenaVarsInContact.totalKchiDur + "," +
                subjectLenaVarsInContact.totalKchiDurWLENA + "," +
                partnerLenaVarsInContact.totalKchiDurWLENA + "," +

                subjectLenaVarsInContact.totalTurnCounts + "," +
                partnerLenaVarsInContact.totalTurnCounts + "," +
                subjectLenaVarsInContact.totalChildUttCount + "," +
                partnerLenaVarsInContact.totalChildUttCount + "," +

                subjectLenaVarsInContact.totalKchiCount + "," +
                partnerLenaVarsInContact.totalKchiCount + "," +
                subjectLenaVarsInContact.totalKchiCountWLENA + "," +
                partnerLenaVarsInContact.totalKchiCountWLENA + "," +


                subjectLenaVarsInContact.totalAdultWordCount + "," +
                partnerLenaVarsInContact.totalAdultWordCount + "," +
                subjectLenaVarsInContact.totalNoise + "," +
                partnerLenaVarsInContact.totalNoise + "," +
                subjectLenaVarsInContact.totalOLN + "," +
                partnerLenaVarsInContact.totalOLN + "," +
                subjectLenaVarsInContact.totalChildCryDuration + "," +
                partnerLenaVarsInContact.totalChildCryDuration + "," +


                pair.partnerJoinedCry + "," +
                pair.subjectJoinedCry + "," +
                pair.joinedCry + ",";
             
                szSw += pair.pairProxDuration + "," +
               pair.pairProxOriDuration + "," +
               pair.sharedTimeInSecs + "," +
               pair.partnerTotalTimeInSecs + "," +
               pair.subjectTotalTimeInSecs + "," +


               recSecs + "," +
               subjectLenaVarsInWUBI.totalChildUttDuration + "," +
               subjectLenaVarsInTotal.totalChildUttDuration + "," +
               partnerLenaVarsInWUBI.totalChildUttDuration + "," +
               partnerLenaVarsInTotal.totalChildUttDuration + "," +

               subjectLenaVarsInWUBI.totalKchiDur + "," +
               subjectLenaVarsInTotal.totalKchiDur + "," +
               partnerLenaVarsInWUBI.totalKchiDur + "," +
               partnerLenaVarsInTotal.totalKchiDur + "," +
               subjectLenaVarsInWUBI.totalKchiDurWLENA + "," +
               subjectLenaVarsInTotal.totalKchiDurWLENA + "," +
               partnerLenaVarsInWUBI.totalKchiDurWLENA + "," +
               partnerLenaVarsInTotal.totalKchiDurWLENA + "," +



               subjectLenaVarsInWUBI.totalChildUttCount + "," +
               subjectLenaVarsInTotal.totalChildUttCount + "," +
               partnerLenaVarsInWUBI.totalChildUttCount + "," +
               partnerLenaVarsInTotal.totalChildUttCount + "," +

               subjectLenaVarsInWUBI.totalKchiCount + "," +
               subjectLenaVarsInTotal.totalKchiCount + "," +
               partnerLenaVarsInWUBI.totalKchiCount + "," +
               partnerLenaVarsInTotal.totalKchiCount + "," +
               subjectLenaVarsInWUBI.totalKchiCountWLENA + "," +
               subjectLenaVarsInTotal.totalKchiCountWLENA + "," +
               partnerLenaVarsInWUBI.totalKchiCountWLENA + "," +
               partnerLenaVarsInTotal.totalKchiCountWLENA + "," +


               subjectLenaVarsInWUBI.totalTurnCounts + "," +
               subjectLenaVarsInTotal.totalTurnCounts + "," +
               partnerLenaVarsInWUBI.totalTurnCounts + "," +
               partnerLenaVarsInTotal.totalTurnCounts + "," +

               subjectLenaVarsInWUBI.totalAdultWordCount + "," +
               subjectLenaVarsInTotal.totalAdultWordCount + "," +
               partnerLenaVarsInWUBI.totalAdultWordCount + "," +
               partnerLenaVarsInTotal.totalAdultWordCount + "," +

               subjectLenaVarsInWUBI.totalNoise + "," +
               subjectLenaVarsInTotal.totalNoise + "," +
               partnerLenaVarsInWUBI.totalNoise + "," +
               partnerLenaVarsInTotal.totalNoise + "," +

               subjectLenaVarsInWUBI.totalOLN + "," +
               subjectLenaVarsInTotal.totalOLN + "," +
               partnerLenaVarsInWUBI.totalOLN + "," +
               partnerLenaVarsInTotal.totalOLN + "," +

               subjectLenaVarsInWUBI.totalChildCryDuration + "," +
               subjectLenaVarsInTotal.totalChildCryDuration + "," +
               partnerLenaVarsInWUBI.totalChildCryDuration + "," +
               partnerLenaVarsInTotal.totalChildCryDuration + "," +


               (subjectLenaVarsInWUBI.avgDb != 0 && subjectLenaVarsInWUBI.totalSegments != 0 ? subjectLenaVarsInWUBI.avgDb / subjectLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
               (subjectLenaVarsInTotal.avgDb != 0 && subjectLenaVarsInTotal.totalSegments != 0 ? subjectLenaVarsInTotal.avgDb / subjectLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +
               (partnerLenaVarsInWUBI.avgDb != 0 && partnerLenaVarsInWUBI.totalSegments != 0 ? partnerLenaVarsInWUBI.avgDb / partnerLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
               (partnerLenaVarsInTotal.avgDb != 0 && partnerLenaVarsInTotal.totalSegments != 0 ? partnerLenaVarsInTotal.avgDb / partnerLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +

               (subjectLenaVarsInWUBI.maxDb != 0 && subjectLenaVarsInWUBI.totalSegments != 0 ? subjectLenaVarsInWUBI.maxDb / subjectLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
               (subjectLenaVarsInTotal.maxDb != 0 && subjectLenaVarsInTotal.totalSegments != 0 ? subjectLenaVarsInTotal.maxDb / subjectLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +
               (partnerLenaVarsInWUBI.maxDb != 0 && partnerLenaVarsInWUBI.totalSegments != 0 ? partnerLenaVarsInWUBI.maxDb / partnerLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
               (partnerLenaVarsInTotal.maxDb != 0 && partnerLenaVarsInTotal.totalSegments != 0 ? partnerLenaVarsInTotal.maxDb / partnerLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +
               className;
               
                sw.WriteLine(szSw);


            }
            sw.Close();
        }
        public void writePairActivityDatawAlice(Dictionary<String, Pair> pairs, String className, String szOutputFile, List<String> diagnosisList, List<String> languagesList)
        {
            TextWriter sw = new StreamWriter(szOutputFile, false);


            String szHeader =
"Date,Subject,Partner,SubjectShortID,PartnerShortID,SubjectDiagnosis,PartnerDiagnosis,SubjectLanguage,PartnerLanguage," +
"SubjectGender,PartnerGender,Adult,SubjectStatus,PartnerStatus,SubjectType,PartnerType," +
"Input1_pvc_or_sac,Input2_pvc_or_stc," +
"Input3_dur_pvd_or_uttl,";


            szHeader += "PairBlockTalking,PairTalkingDuration," +
//taken out Subject-Talking-Duration-From_Start,"+Partner-Talking-Duration-From-Start,
"Subject-Talking-Duration-Evenly-Spread,Partner-Talking-Duration-Evenly-Spread," +
"SubjectTurnCount,PartnerTurnCount,SubjectVocCount,PartnerVocCount,SubjectAdultCount,PartnerAdultCount,SubjectNoise," +
"PartnerNoise,SubjectOLN,PartnerOLN,SubjectCry,PartnerCry,SubjectJoinedCry,PartnerJoinedCry,JoinedCry,";


            szHeader += "PairProximityDuration," +
"PairOrientation-ProximityDuration,SharedTimeinClassroom,SubjectTime,PartnerTime,TotalRecordingTime,WUBITotalVD,TotalVD," +
"PartnerWUBITotalVD,PartnerTotalVD,WUBITotalVC,TotalVC,PartnerWUBITotalVC,PartnerTotalVC,WUBITotalTC,TotalTC,PartnerWUBITotalTC," +
"PartnerTotalTC,WUBITotalAC,TotalAC,PartnerWUBITotalAC,PartnerTotalAC,WUBITotalNO,TotalNO,PartnerWUBITotalNO,PartnerTotalNO," +
"WUBITotalOLN,TotalOLN,PartnerWUBITotalOLN,PartnerTotalOLN,WUBITotalCRY,TotalCRY,PartnerWUBITotalCRY,PartnerTotalCRY," +
"WUBITotalAV_DB,TotalAV_DB,PartnerWUBITotalAV_DB,PartnerTotalAV_DB,WUBITotalAV_PEAK_DB,TotalAV_PEAK_DB,PartnerWUBITotalAV_PEAK_DB,PartnerTotalAV_PEAK_DB,CLASSROOM";

            szHeader +=",Subject_ALICE_KCHI,Partner_ALICE_KCHI,"+
                                        "Subject_ALICE_CHI,Partner_ALICE_CHI,"+
                                        "Subject_ALICE_FEM,Partner_ALICE_FEM," +
                                        "Subject_ALICE_MAL,Partner_ALICE_MAL," +
                                        "Subject_ALICE_SPEECH,Partner_ALICE_SPEECH," +
                                        "WUBITotal_Subject_ALICE_KCHI,Total_Subject_ALICE_KCHI,"+
                                        "WUBITotal_Partner_ALICE_KCHI,Total_Partner_ALICE_KCHI," +
                                        "WUBITotal_Subject_ALICE_CHI,Total_Subject_ALICE_CHI," +
                                        "WUBITotal_Partner_ALICE_CHI,Total_Partner_ALICE_CHI," +
                                        "WUBITotal_Subject_ALICE_FEM,Total_Subject_ALICE_FEM," +
                                        "WUBITotal_Partner_ALICE_FEM,Total_Partner_ALICE_FEM," +
                                        "WUBITotal_Subject_ALICE_MAL,Total_Subject_ALICE_MAL," +
                                        "WUBITotal_Partner_ALICE_MAL,Total_Partner_ALICE_MAL," +
                                        "WUBITotal_Subject_ALICE_SPEECH,Total_Subject_ALICE_SPEECH," +
                                        "WUBITotal_Partner_ALICE_SPEECH,Total_Partner_ALICE_SPEECH";
            //82
            szHeader =szHeader.Replace(" ", "");
            String newDiagnosis = "";
            foreach (String d in diagnosisList)
            {
                newDiagnosis += ("Subject" + d + ",Partner" + d + ",");
            }
            String newLanguages = "";
            foreach (String l in languagesList)
            {
                newLanguages += ("Subject" + l + ",Partner" + l + ",");
            }

            newDiagnosis = newDiagnosis == "SubjectDiagnosis,PartnerDiagnosis,SubjectLanguage,PartnerLanguage," ? "SubjectDiagnosis,PartnerDiagnosis," : newDiagnosis;
            newLanguages = newLanguages == "" ? "SubjectLanguage,PartnerLanguage," : newLanguages;
            szHeader = szHeader.Replace("SubjectDiagnosis,PartnerDiagnosis,", newDiagnosis);
            szHeader = szHeader.Replace("SubjectLanguage,PartnerLanguage,", newLanguages);

            sw.WriteLine(szHeader);

            foreach (String szPair in pairs.Keys)
            {
                String szSubject = szPair.Split('|')[0];
                String szPartner = szPair.Split('|')[1];
                Person subject = personBaseMappings[szSubject];
                Person partner = personBaseMappings[szPartner];
                Pair pair = pairs[szPair];
                PersonDayInfo sdi = Utilities.getPerson(personDayMappings, szSubject);
                PersonDayInfo pdi = Utilities.getPerson(personDayMappings, szPartner);

                LenaVars subjectLenaVarsInContact = pair.subjectLenaVarsInContact;
                LenaVars partnerLenaVarsInContact = pair.partnerLenaVarsInContact;

                LenaVars subjectLenaVarsInWUBI = personDayMappings[szSubject].WUBILenaVars;
                LenaVars partnerLenaVarsInWUBI = personDayMappings[szPartner].WUBILenaVars;
                LenaVars subjectLenaVarsInTotal = personDayMappings[szSubject].totalLenaVars;
                LenaVars partnerLenaVarsInTotal = personDayMappings[szPartner].totalLenaVars;

                AliceVars subjectAliceVarsInContact = pair.subjectAliceVarsInContact;
                AliceVars partnerAliceVarsInContact = pair.partnerAliceVarsInContact;

                AliceVars subjectAliceVarsInWUBI = personDayMappings[szSubject].WUBIAliceVars;
                AliceVars partnerAliceVarsInWUBI = personDayMappings[szPartner].WUBIAliceVars;
                AliceVars subjectAliceVarsInTotal = personDayMappings[szSubject].totalAliceVars;
                AliceVars partnerAliceVarsInTotal = personDayMappings[szPartner].totalAliceVars;


                String newPairDiagnosis = "";
                String newPairDiagnosisP = "";
                int pos = 0;
                foreach (String d in subject.diagnosisList)
                {
                    newPairDiagnosis += (d + "," + partner.diagnosisList[pos] + ",");
                    newPairDiagnosisP += (partner.diagnosisList[pos] + "," + d + ",");
                    pos++;
                }
                String newPairLanguages = "";
                String newPairLanguagesP = "";
                pos = 0;
                foreach (String l in subject.languagesList)
                {
                    newPairLanguages += (l + "," + partner.languagesList[pos] + ",");
                    newPairLanguagesP += (partner.languagesList[pos] + "," + l + ",");
                    pos++;
                }

                String szSw = this.classDay.Month + "/" + this.classDay.Day + "/" + this.classDay.Year + "," +
                subject.longId + "," +
                partner.longId + "," +
                subject.shortId + "," +
                partner.shortId + "," +
                newPairDiagnosis +
                newPairLanguages +
                subject.gender + "," +
                partner.gender + "," +
                (!(subject.subjectType.ToUpper().Trim() == "CHILD" && partner.subjectType.ToUpper().Trim() == "CHILD")) + "," +
                sdi.status + "," +
                pdi.status + "," +
                subject.subjectType + "," +
                partner.subjectType + "," +
                (sdi.status.ToUpper() == "PRESENT" && pdi.status.ToUpper() == "PRESENT" ? (partner.subjectType.ToUpper() == "CHILD" ? partnerLenaVarsInContact.totalChildUttCount.ToString() : subjectLenaVarsInContact.totalAdultWordCount.ToString()) : "NA") + "," +
                (sdi.status.ToUpper() == "PRESENT" && pdi.status.ToUpper() == "PRESENT" ? (partner.subjectType.ToUpper() == "CHILD" ? partnerLenaVarsInContact.totalChildUttCount.ToString() : subjectLenaVarsInContact.totalTurnCounts.ToString()) : "NA") + "," +
                (sdi.status.ToUpper() == "PRESENT" && pdi.status.ToUpper() == "PRESENT" ? (partner.subjectType.ToUpper() == "CHILD" ? partnerLenaVarsInContact.totalChildUttDuration.ToString() : subjectLenaVarsInContact.totalAdultWordCount.ToString()) : "NA") + ",";


                szSw += pair.pairBlockTalking + "," +
                 pair.pairProxOriDuration + "," +//partnerLenaVarsInContact.totalChildUttDuration + "," +
                                                 //"NA," +
                                                 //"NA," +
                subjectLenaVarsInContact.totalChildUttDuration + "," +
                partnerLenaVarsInContact.totalChildUttDuration + "," +
                subjectLenaVarsInContact.totalTurnCounts + "," +
                partnerLenaVarsInContact.totalTurnCounts + "," +
                subjectLenaVarsInContact.totalChildUttCount + "," +
                partnerLenaVarsInContact.totalChildUttCount + "," +
                subjectLenaVarsInContact.totalAdultWordCount + "," +
                partnerLenaVarsInContact.totalAdultWordCount + "," +
                subjectLenaVarsInContact.totalNoise + "," +
                partnerLenaVarsInContact.totalNoise + "," +
              subjectLenaVarsInContact.totalOLN + "," +
                partnerLenaVarsInContact.totalOLN + "," +
                subjectLenaVarsInContact.totalChildCryDuration + "," +
                partnerLenaVarsInContact.totalChildCryDuration + "," +


                pair.partnerJoinedCry + "," +
                pair.subjectJoinedCry + "," +
                pair.joinedCry + ",";


                szSw += pair.pairProxDuration + "," +
                pair.pairProxOriDuration + "," +
                pair.sharedTimeInSecs + "," +
                pair.subjectTotalTimeInSecs + "," +
                pair.partnerTotalTimeInSecs + "," +
                recSecs + "," +
                subjectLenaVarsInWUBI.totalChildUttDuration + "," +
                subjectLenaVarsInTotal.totalChildUttDuration + "," +
                partnerLenaVarsInWUBI.totalChildUttDuration + "," +
                partnerLenaVarsInTotal.totalChildUttDuration + "," +

                subjectLenaVarsInWUBI.totalChildUttCount + "," +
                subjectLenaVarsInTotal.totalChildUttCount + "," +
                partnerLenaVarsInWUBI.totalChildUttCount + "," +
                partnerLenaVarsInTotal.totalChildUttCount + "," +

                subjectLenaVarsInWUBI.totalTurnCounts + "," +
                subjectLenaVarsInTotal.totalTurnCounts + "," +
                partnerLenaVarsInWUBI.totalTurnCounts + "," +
                partnerLenaVarsInTotal.totalTurnCounts + "," +

                subjectLenaVarsInWUBI.totalAdultWordCount + "," +
                subjectLenaVarsInTotal.totalAdultWordCount + "," +
                partnerLenaVarsInWUBI.totalAdultWordCount + "," +
                partnerLenaVarsInTotal.totalAdultWordCount + "," +

                subjectLenaVarsInWUBI.totalNoise + "," +
                subjectLenaVarsInTotal.totalNoise + "," +
                partnerLenaVarsInWUBI.totalNoise + "," +
                partnerLenaVarsInTotal.totalNoise + "," +

                subjectLenaVarsInWUBI.totalOLN + "," +
                subjectLenaVarsInTotal.totalOLN + "," +
                partnerLenaVarsInWUBI.totalOLN + "," +
                partnerLenaVarsInTotal.totalOLN + "," +

                subjectLenaVarsInWUBI.totalChildCryDuration + "," +
                subjectLenaVarsInTotal.totalChildCryDuration + "," +
                partnerLenaVarsInWUBI.totalChildCryDuration + "," +
                partnerLenaVarsInTotal.totalChildCryDuration + "," +


                (subjectLenaVarsInWUBI.avgDb != 0 && subjectLenaVarsInWUBI.totalSegments != 0 ? subjectLenaVarsInWUBI.avgDb / subjectLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
                (subjectLenaVarsInTotal.avgDb != 0 && subjectLenaVarsInTotal.totalSegments != 0 ? subjectLenaVarsInTotal.avgDb / subjectLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +
                (partnerLenaVarsInWUBI.avgDb != 0 && partnerLenaVarsInWUBI.totalSegments != 0 ? partnerLenaVarsInWUBI.avgDb / partnerLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
                (partnerLenaVarsInTotal.avgDb != 0 && partnerLenaVarsInTotal.totalSegments != 0 ? partnerLenaVarsInTotal.avgDb / partnerLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +

                (subjectLenaVarsInWUBI.maxDb != 0 && subjectLenaVarsInWUBI.totalSegments != 0 ? subjectLenaVarsInWUBI.maxDb / subjectLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
                (subjectLenaVarsInTotal.maxDb != 0 && subjectLenaVarsInTotal.totalSegments != 0 ? subjectLenaVarsInTotal.maxDb / subjectLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +
                (partnerLenaVarsInWUBI.maxDb != 0 && partnerLenaVarsInWUBI.totalSegments != 0 ? partnerLenaVarsInWUBI.maxDb / partnerLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
                (partnerLenaVarsInTotal.maxDb != 0 && partnerLenaVarsInTotal.totalSegments != 0 ? partnerLenaVarsInTotal.maxDb / partnerLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +
                className;

                /*szHeader +=@",Subject_ALICE_KCHI,Partner_ALICE_KCHI,
                                        Subject_ALICE_CHI,Partner_ALICE_CHI,
                                        Subject_ALICE_FEM,Partner_ALICE_FEM,
                                        Subject_ALICE_MAL,Partner_ALICE_MAL,
                                        Subject_ALICE_SPEECH,Partner_ALICE_SPEECH,
                                        WUBITotal_Subject_ALICE_KCHI,Total_Subject_ALICE_KCHI,
                                        WUBITotal_Partner_ALICE_KCHI,Total_Partner_ALICE_KCHI,
                                        WUBITotal_Subject_ALICE_CHI,Total_Subject_ALICE_CHI,
                                        WUBITotal_Partner_ALICE_CHI,Total_Partner_ALICE_CHI,
                                        WUBITotal_Subject_ALICE_FEM,Total_Subject_ALICE_FEM,
                                        WUBITotal_Partner_ALICE_FEM,Total_Partner_ALICE_FEM,
                                        WUBITotal_Subject_ALICE_MAL,Total_Subject_ALICE_MAL,
                                        WUBITotal_Partner_ALICE_MAL,Total_Partner_ALICE_MAL,
                                        WUBITotal_Subject_ALICE_SPEECH,Total_Subject_ALICE_SPEECH,
                                        WUBITotal_Partner_ALICE_SPEECH,Total_Partner_ALICE_SPEECH";
            //82*/

                szSw += ","+subjectAliceVarsInContact.kchi+","+
                    partnerAliceVarsInContact.kchi + "," +
                    subjectAliceVarsInContact.chi + "," +
                    partnerAliceVarsInContact.chi + "," +
                    subjectAliceVarsInContact.fem + "," +
                    partnerAliceVarsInContact.fem + "," +
                    subjectAliceVarsInContact.mal + "," +
                    partnerAliceVarsInContact.mal + "," +
                    subjectAliceVarsInContact.speech + "," +
                    partnerAliceVarsInContact.speech + ","+

                    subjectAliceVarsInWUBI.kchi + "," +
                    subjectAliceVarsInTotal.kchi + "," +
                    partnerAliceVarsInWUBI.kchi + "," +
                    partnerAliceVarsInTotal.kchi + ","+

                    subjectAliceVarsInWUBI.chi + "," +
                    subjectAliceVarsInTotal.chi + "," +
                    partnerAliceVarsInWUBI.chi + "," +
                    partnerAliceVarsInTotal.chi + "," +

                    subjectAliceVarsInWUBI.fem + "," +
                    subjectAliceVarsInTotal.fem + "," +
                    partnerAliceVarsInWUBI.fem + "," +
                    partnerAliceVarsInTotal.fem + "," +

                    subjectAliceVarsInWUBI.mal + "," +
                    subjectAliceVarsInTotal.mal + "," +
                    partnerAliceVarsInWUBI.mal + "," +
                    partnerAliceVarsInTotal.mal + "," +

                    subjectAliceVarsInWUBI.speech + "," +
                    subjectAliceVarsInTotal.speech + "," +
                    partnerAliceVarsInWUBI.speech + "," +
                    partnerAliceVarsInTotal.speech;


                    sw.WriteLine(szSw);







                szSubject = szPair.Split('|')[1];
                szPartner = szPair.Split('|')[0];
                subject = personBaseMappings[szSubject];
                partner = personBaseMappings[szPartner];
                sdi = Utilities.getPerson(personDayMappings, szSubject);
                pdi = Utilities.getPerson(personDayMappings, szPartner);

                subjectLenaVarsInContact = pair.partnerLenaVarsInContact;
                partnerLenaVarsInContact = pair.subjectLenaVarsInContact;
                subjectAliceVarsInContact = pair.partnerAliceVarsInContact;
                partnerAliceVarsInContact = pair.subjectAliceVarsInContact;


                //szSubject
                subjectLenaVarsInWUBI = personDayMappings[szSubject].WUBILenaVars;
                partnerLenaVarsInWUBI = personDayMappings[szPartner].WUBILenaVars;
                subjectLenaVarsInTotal = personDayMappings[szSubject].totalLenaVars;
                partnerLenaVarsInTotal = personDayMappings[szPartner].totalLenaVars;

                subjectAliceVarsInWUBI = personDayMappings[szSubject].WUBIAliceVars;
                partnerAliceVarsInWUBI = personDayMappings[szPartner].WUBIAliceVars;
                subjectAliceVarsInTotal = personDayMappings[szSubject].totalAliceVars;
                partnerAliceVarsInTotal = personDayMappings[szPartner].totalAliceVars;


                szSw = this.classDay.Month + "/" + this.classDay.Day + "/" + this.classDay.Year + "," +
               subject.longId + "," +
               partner.longId + "," +
               subject.shortId + "," +
               partner.shortId + "," +
                newPairDiagnosisP +
                newPairLanguagesP +
               subject.gender + "," +
               partner.gender + "," +
               (!(subject.subjectType.ToUpper().Trim() == "CHILD" && partner.subjectType.ToUpper().Trim() == "CHILD")) + "," +
               sdi.status + "," +
               pdi.status + "," +
               subject.subjectType + "," +
               partner.subjectType + "," +
               (sdi.status.ToUpper() == "PRESENT" && pdi.status.ToUpper() == "PRESENT" ? (partner.subjectType.ToUpper() == "CHILD" ? partnerLenaVarsInContact.totalChildUttCount.ToString() : subjectLenaVarsInContact.totalAdultWordCount.ToString()) : "NA") + "," +
               (sdi.status.ToUpper() == "PRESENT" && pdi.status.ToUpper() == "PRESENT" ? (partner.subjectType.ToUpper() == "CHILD" ? partnerLenaVarsInContact.totalChildUttCount.ToString() : subjectLenaVarsInContact.totalTurnCounts.ToString()) : "NA") + "," +
               (sdi.status.ToUpper() == "PRESENT" && pdi.status.ToUpper() == "PRESENT" ? (partner.subjectType.ToUpper() == "CHILD" ? partnerLenaVarsInContact.totalChildUttDuration.ToString() : subjectLenaVarsInContact.totalAdultWordCount.ToString()) : "NA") + ",";


                szSw += pair.pairBlockTalking + "," +
                pair.pairProxOriDuration + "," +//partnerLenaVarsInContact.totalChildUttDuration + "," +
                                                //"NA," +
                                                //"NA," +
                subjectLenaVarsInContact.totalChildUttDuration + "," +
                partnerLenaVarsInContact.totalChildUttDuration + "," +
                subjectLenaVarsInContact.totalTurnCounts + "," +
                partnerLenaVarsInContact.totalTurnCounts + "," +
                subjectLenaVarsInContact.totalChildUttCount + "," +
                partnerLenaVarsInContact.totalChildUttCount + "," +
                subjectLenaVarsInContact.totalAdultWordCount + "," +
                partnerLenaVarsInContact.totalAdultWordCount + "," +
                subjectLenaVarsInContact.totalNoise + "," +
                partnerLenaVarsInContact.totalNoise + "," +
                subjectLenaVarsInContact.totalOLN + "," +
                partnerLenaVarsInContact.totalOLN + "," +
                subjectLenaVarsInContact.totalChildCryDuration + "," +
                partnerLenaVarsInContact.totalChildCryDuration + "," +


                pair.partnerJoinedCry + "," +
                pair.subjectJoinedCry + "," +
                pair.joinedCry + ",";

                szSw += pair.pairProxDuration + "," +
               pair.pairProxOriDuration + "," +
               pair.sharedTimeInSecs + "," +
               pair.partnerTotalTimeInSecs + "," +
               pair.subjectTotalTimeInSecs + "," +


               recSecs + "," +
               subjectLenaVarsInWUBI.totalChildUttDuration + "," +
               subjectLenaVarsInTotal.totalChildUttDuration + "," +
               partnerLenaVarsInWUBI.totalChildUttDuration + "," +
               partnerLenaVarsInTotal.totalChildUttDuration + "," +

               subjectLenaVarsInWUBI.totalChildUttCount + "," +
               subjectLenaVarsInTotal.totalChildUttCount + "," +
               partnerLenaVarsInWUBI.totalChildUttCount + "," +
               partnerLenaVarsInTotal.totalChildUttCount + "," +
               subjectLenaVarsInWUBI.totalTurnCounts + "," +
               subjectLenaVarsInTotal.totalTurnCounts + "," +
               partnerLenaVarsInWUBI.totalTurnCounts + "," +
               partnerLenaVarsInTotal.totalTurnCounts + "," +

               subjectLenaVarsInWUBI.totalAdultWordCount + "," +
               subjectLenaVarsInTotal.totalAdultWordCount + "," +
               partnerLenaVarsInWUBI.totalAdultWordCount + "," +
               partnerLenaVarsInTotal.totalAdultWordCount + "," +

               subjectLenaVarsInWUBI.totalNoise + "," +
               subjectLenaVarsInTotal.totalNoise + "," +
               partnerLenaVarsInWUBI.totalNoise + "," +
               partnerLenaVarsInTotal.totalNoise + "," +

               subjectLenaVarsInWUBI.totalOLN + "," +
               subjectLenaVarsInTotal.totalOLN + "," +
               partnerLenaVarsInWUBI.totalOLN + "," +
               partnerLenaVarsInTotal.totalOLN + "," +

               subjectLenaVarsInWUBI.totalChildCryDuration + "," +
               subjectLenaVarsInTotal.totalChildCryDuration + "," +
               partnerLenaVarsInWUBI.totalChildCryDuration + "," +
               partnerLenaVarsInTotal.totalChildCryDuration + "," +


               (subjectLenaVarsInWUBI.avgDb != 0 && subjectLenaVarsInWUBI.totalSegments != 0 ? subjectLenaVarsInWUBI.avgDb / subjectLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
               (subjectLenaVarsInTotal.avgDb != 0 && subjectLenaVarsInTotal.totalSegments != 0 ? subjectLenaVarsInTotal.avgDb / subjectLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +
               (partnerLenaVarsInWUBI.avgDb != 0 && partnerLenaVarsInWUBI.totalSegments != 0 ? partnerLenaVarsInWUBI.avgDb / partnerLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
               (partnerLenaVarsInTotal.avgDb != 0 && partnerLenaVarsInTotal.totalSegments != 0 ? partnerLenaVarsInTotal.avgDb / partnerLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +

               (subjectLenaVarsInWUBI.maxDb != 0 && subjectLenaVarsInWUBI.totalSegments != 0 ? subjectLenaVarsInWUBI.maxDb / subjectLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
               (subjectLenaVarsInTotal.maxDb != 0 && subjectLenaVarsInTotal.totalSegments != 0 ? subjectLenaVarsInTotal.maxDb / subjectLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +
               (partnerLenaVarsInWUBI.maxDb != 0 && partnerLenaVarsInWUBI.totalSegments != 0 ? partnerLenaVarsInWUBI.maxDb / partnerLenaVarsInWUBI.totalSegments : 0.00).ToString() + "," +
               (partnerLenaVarsInTotal.maxDb != 0 && partnerLenaVarsInTotal.totalSegments != 0 ? partnerLenaVarsInTotal.maxDb / partnerLenaVarsInTotal.totalSegments : 0.00).ToString() + "," +
               className;
                szSw += "," + subjectAliceVarsInContact.kchi + "," +
                  partnerAliceVarsInContact.kchi + "," +
                  subjectAliceVarsInContact.chi + "," +
                  partnerAliceVarsInContact.chi + "," +
                  subjectAliceVarsInContact.fem + "," +
                  partnerAliceVarsInContact.fem + "," +
                  subjectAliceVarsInContact.mal + "," +
                  partnerAliceVarsInContact.mal + "," +
                  subjectAliceVarsInContact.speech + "," +
                  partnerAliceVarsInContact.speech + "," +

                  subjectAliceVarsInWUBI.kchi + "," +
                  subjectAliceVarsInTotal.kchi + "," +
                  partnerAliceVarsInWUBI.kchi + "," +
                  partnerAliceVarsInTotal.kchi + "," +

                  subjectAliceVarsInWUBI.chi + "," +
                  subjectAliceVarsInTotal.chi + "," +
                  partnerAliceVarsInWUBI.chi + "," +
                  partnerAliceVarsInTotal.chi + "," +

                  subjectAliceVarsInWUBI.fem + "," +
                  subjectAliceVarsInTotal.fem + "," +
                  partnerAliceVarsInWUBI.fem + "," +
                  partnerAliceVarsInTotal.fem + "," +

                  subjectAliceVarsInWUBI.mal + "," +
                  subjectAliceVarsInTotal.mal + "," +
                  partnerAliceVarsInWUBI.mal + "," +
                  partnerAliceVarsInTotal.mal + "," +

                  subjectAliceVarsInWUBI.speech + "," +
                  subjectAliceVarsInTotal.speech + "," +
                  partnerAliceVarsInWUBI.speech + "," +
                  partnerAliceVarsInTotal.speech;
                sw.WriteLine(szSw);


            }
            sw.Close();
        }
        public Boolean hasAllInfo(PersonSuperInfo p1)
        {
             
            
            return ((!double.IsNaN(p1.x)) &&
                    (!double.IsNaN(p1.y)) &&
                    (!double.IsNaN(p1.xl)) &&
                    (!double.IsNaN(p1.yl)) &&
                    (!double.IsNaN(p1.xr)) &&
                    (!double.IsNaN(p1.yr)) &&
                    p1.x!=0 &&
                    p1.y!=0);
        }
        public Dictionary<String, Pair> countInteractions(double minGr, double maxGr, double angle, String szAngleOutputFile, String szAppOutputFile)
        {
            return countInteractions(new Dictionary<string, Tuple<string, DateTime>>(), minGr,  maxGr,  angle,  szAngleOutputFile,  szAppOutputFile);

        }
        public Dictionary<String, Pair> countInteractions(Dictionary<String,Tuple<String,DateTime>> lenaStartTimes, double minGr, double maxGr, double angle, String szAngleOutputFile, String szAppOutputFile)
        {//pairs are unique not repeated//
            Dictionary<String, Pair> pairs = Utilities.getSzPairKey(personDayMappings);
            Dictionary<String, int> onsetPos = new Dictionary<string, int>();
            Boolean doAngles = szAngleOutputFile != "";
            Boolean doApp = szAppOutputFile != "";
             
            TextWriter sw = doAngles ? new StreamWriter(szAngleOutputFile) : null;
            TextWriter swapp = doApp ? new StreamWriter(szAppOutputFile) : null;

            //DEBUG DELETE ELAN INTERACTIONS
            /* Time frame is 9:46:53 AM - 10:29:53
            Subject 2 (9:46:58-9:52:12)
            Subject 9 (9:52:12-9:57:35)
            Subject 7 (9:57:35-10:02:57)
            Subject 6 (10:02:57-10:08:09)
            Subject 10 (10:08:09-10:13:28)
            Subject 8 (10:13:28-10:18:46)
            Subject 5 (10:18:46-10:24:42)
            Subject 1 (10:24:42-10:29:53)*/

            /*TextWriter swi = new StreamWriter(szAngleOutputFile.Replace(".CSV","INTERACTIONS.CSV"));
            swi.WriteLine("Person 1, Person2, Interaction Time, Interaction Millisecond, Interaction, " + angle + "Interaction, WasTalking1, WasTalking2 ");
            Dictionary<String, Tuple<DateTime, DateTime>> filters = new Dictionary<string, Tuple<DateTime, DateTime>>();
            filters.Add("LRIC_APPLETREE_2324_2", new Tuple<DateTime, DateTime>(new DateTime(2023, 10, 26, 9, 46, 58), new DateTime(2023, 10, 26, 9, 52, 12)));
            filters.Add("LRIC_APPLETREE_2324_9", new Tuple<DateTime, DateTime>(new DateTime(2023, 10, 26, 9, 52, 12), new DateTime(2023, 10, 26, 9, 57, 35)));
            filters.Add("LRIC_APPLETREE_2324_7", new Tuple<DateTime, DateTime>(new DateTime(2023, 10, 26, 9, 57, 35), new DateTime(2023, 10, 26, 9, 02, 57)));
            filters.Add("LRIC_APPLETREE_2324_6", new Tuple<DateTime, DateTime>(new DateTime(2023, 10, 26, 10, 02, 57), new DateTime(2023, 10, 26, 9, 08, 09)));
            filters.Add("LRIC_APPLETREE_2324_10", new Tuple<DateTime, DateTime>(new DateTime(2023, 10, 26, 10, 08, 09), new DateTime(2023, 10, 26, 9, 13, 28)));
            filters.Add("LRIC_APPLETREE_2324_8", new Tuple<DateTime, DateTime>(new DateTime(2023, 10, 26, 10, 13, 28), new DateTime(2023, 10, 26, 9, 18, 46)));
            filters.Add("LRIC_APPLETREE_2324_5", new Tuple<DateTime, DateTime>(new DateTime(2023, 10, 26, 10, 18, 46), new DateTime(2023, 10, 26, 9, 24, 42)));
            filters.Add("LRIC_APPLETREE_2324_1", new Tuple<DateTime, DateTime>(new DateTime(2023, 10, 26, 10, 24, 42), new DateTime(2023, 10, 26, 9, 29, 53)));*/
            //DEBUG DELETE ELAN INTERACTIONS

            if (doAngles)
                sw.WriteLine("Person 1, Person2, Interaction Time, Interaction Millisecond, Interaction, " + angle + "Interaction, Angle1, Angle2, Leftx,Lefty,Rightx,Righty, Leftx2,Lefty2,Rightx2,Righty2,Type1, Type2, Gender1, Gender2, Diagnosis1, Diagnosis2, WasTalking1, WasTalking2 ");

            if (doApp)
                swapp.WriteLine("Person 1, Person2, Interaction Time, Interaction Millisecond,d1,d2,approachMeters,x10,y10,x20,y20,x11,y11,x21,y21, WithinGR, WithinGRAnd" + angle + "deg, Angle1, Angle2,Type1, Type2, Gender1, Gender2, Diagnosis1, Diagnosis2,LongPerson 1, LongPerson2,  ");


            DateTime trunkAt = new DateTime();// getTrunkTime();
            Dictionary<String, DateTime> sLenaStartTime = new Dictionary<string, DateTime>();
            Boolean isOldVersion = (classDay.Year < 2021 || (classDay.Year == 2021 && classDay.Month < 8));
            
            if(isOldVersion)
            {
                trunkAt = getTrunkTime();
                foreach (Tuple<String, DateTime> tup in lenaStartTimes.Values)
                {
                    sLenaStartTime.Add(tup.Item1, tup.Item2);
                }
            }
            

            foreach (DateTime t in ubiTenths.Keys)
            {

                foreach (String szPairKey in pairs.Keys)
                {
                    Pair pair = pairs[szPairKey];
                    Boolean validTime = true;
                    if (isOldVersion)
                    {
                       /* if (t.CompareTo(trunkAt) > 0)
                        {
                            validTime = false;
                        } 
                        else*/
                        if ((sLenaStartTime.ContainsKey(pair.szSubjectMapId) &&
                            t.CompareTo(sLenaStartTime[pair.szSubjectMapId]) <= 0 ) ||
                            (sLenaStartTime.ContainsKey(pair.szPartnerMapId) && 
                            t.CompareTo(sLenaStartTime[pair.szPartnerMapId]) <= 0))
                            {

                            //((sLenaStartTime.ContainsKey(pair.szSubjectMapId) && t.CompareTo(sLenaStartTime[pair.szSubjectMapId]) <= 0 ) || (sLenaStartTime.ContainsKey(pair.szPartnerMapId) && t.CompareTo(sLenaStartTime[pair.szPartnerMapId]) <= 0))
                            validTime = false;
                            }


                    }
                     

                    if (ubiTenths[t].ContainsKey(pair.szSubjectMapId))
                    {
                        pair.subjectTotalTimeInSecs += .1;

                        //if (ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiCount>0.000 && ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildUttCount>0.0000)
                        {
                            //pair.subjectLenaVarsInWUBI
                        }
                    }
                    if (ubiTenths[t].ContainsKey(pair.szPartnerMapId))
                    {
                        pair.partnerTotalTimeInSecs += .1;
                    }

                    if (validTime)
                    {


                        if (ubiTenths[t].ContainsKey(pair.szSubjectMapId) &&
                        ubiTenths[t].ContainsKey(pair.szPartnerMapId) &&
                        hasAllInfo(ubiTenths[t][pair.szSubjectMapId]) &&
                        hasAllInfo(ubiTenths[t][pair.szPartnerMapId]))
                    {

                        pair.sharedTimeInSecs += .1;
                        double dist = Utilities.calcSquaredDist(ubiTenths[t][pair.szSubjectMapId], ubiTenths[t][pair.szPartnerMapId]);
                        Boolean withinGofR = (dist <= (maxGr * maxGr)) && (dist >= (minGr * minGr));


                        Tuple<double, double> angles = Utilities.withinOrientationData(ubiTenths[t][pair.szSubjectMapId], ubiTenths[t][pair.szPartnerMapId]);
                        Boolean t1Hack = false;
                        if ((personDayMappings[pair.szSubjectMapId].leftUbi.Trim() == "00:11:CE:00:00:00:02:CE" || personDayMappings[pair.szPartnerMapId].leftUbi.Trim() == "00:11:CE:00:00:00:02:CE") &&
                                        t >= new DateTime(2019, 02, 12, 10, 28, 38, 644) &&
                                        t <= new DateTime(2019, 06, 3))
                        {
                            t1Hack = true;
                        }

                        Boolean orientedCloseness = withinGofR && ((Math.Abs(angles.Item1) <= angle && Math.Abs(angles.Item2) <= angle) || t1Hack);
                        //sw.WriteLine("Person 1, Person2, Interaction Time, Interaction Millisecond, Interaction, " + angle + "Interaction, Angle1, Angle2, Leftx,Lefty,Rightx,Righty, Leftx2,Lefty2,Rightx2,Righty2,Type1, Type2, Gender1, Gender2, Diagnosis1, Diagnosis2, WasTalking1, WasTalking2 ");

                        if (doAngles)
                            sw.WriteLine(pair.szSubjectMapId + "," +
                                                                            pair.szPartnerMapId + "," +
                                                                            t.ToLongTimeString() + "," +
                                                                            t.Millisecond + "," +
                                                                            (withinGofR ? "0.1" : "0") + "," +
                                                                            (orientedCloseness ? "0.1" : "0") + "," +
                                                                            (angles.Item1) + "," +
                                                                            (angles.Item2) + "," +
                                                                            ubiTenths[t][pair.szSubjectMapId].xl + "," +
                                                                            ubiTenths[t][pair.szSubjectMapId].yl + "," +
                                                                            ubiTenths[t][pair.szSubjectMapId].xr + "," +
                                                                            ubiTenths[t][pair.szSubjectMapId].yr + "," +
                                                                            ubiTenths[t][pair.szPartnerMapId].xl + "," +
                                                                            ubiTenths[t][pair.szPartnerMapId].yl + "," +
                                                                            ubiTenths[t][pair.szPartnerMapId].xr + "," +
                                                                            ubiTenths[t][pair.szPartnerMapId].yr + "," +
                                                                        personBaseMappings[pair.szSubjectMapId].subjectType + "," +
                                                                        personBaseMappings[pair.szPartnerMapId].subjectType + "," +
                                                                        personBaseMappings[pair.szSubjectMapId].gender + "," +
                                                                        personBaseMappings[pair.szPartnerMapId].gender + "," +
                                                                        personBaseMappings[pair.szSubjectMapId].diagnosis + "," +
                                                                        personBaseMappings[pair.szPartnerMapId].diagnosis + "," +
                                                                        ubiTenths[t][pair.szSubjectMapId].wasTalking + "," +
                                                                        ubiTenths[t][pair.szPartnerMapId].wasTalking);



                        //DEBUG DELETE ELAN INTERACTIONS
                        /*if(filters.ContainsKey(pair.szSubjectMapId) &&
                            t >= filters[pair.szSubjectMapId].Item1 &&
                            t< filters[pair.szSubjectMapId].Item2)
                        {
                            swi.WriteLine(pair.szSubjectMapId + "," +
                                                                           pair.szPartnerMapId + "," +
                                                                           t.ToLongTimeString() + "," +
                                                                           t.Millisecond + "," +
                                                                           (withinGofR ? "0.1" : "0") + "," +
                                                                           (orientedCloseness ? "0.1" : "0") + "," +
                                                                           ubiTenths[t][pair.szSubjectMapId].wasTalking + "," +
                                                                           ubiTenths[t][pair.szPartnerMapId].wasTalking);

                        }

                        */


                        //DEBUG DELETE ELAN INTERACTIONS
                        pair.pairProxDuration += (withinGofR ? .1 : 0);
                        if (withinGofR && orientedCloseness)
                        {
                            pair.pairBlockTalking += (ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildUttDuration + ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildUttDuration);

                            pair.pairProxOriDuration += .1;
                            //
                            pair.subjectLenaVarsInContact.totalTurnCounts += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalTurnCounts;
                            pair.subjectLenaVarsInContact.totalChildUttCount += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildUttCount;
                            pair.subjectLenaVarsInContact.totalChildUttDuration += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildUttDuration;
                            pair.subjectLenaVarsInContact.totalChildCryDuration += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildCryDuration;
                            pair.subjectLenaVarsInContact.totalAdultWordCount += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalAdultWordCount;
                            pair.subjectLenaVarsInContact.totalNoise += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalNoise;
                            pair.subjectLenaVarsInContact.totalOLN += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalOLN;
                            pair.subjectLenaVarsInContact.totalKchiCount += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiCount;
                            pair.subjectLenaVarsInContact.totalKchiDur += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiDur;
                            if (ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiDur > 0.0000 && ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildUttDuration > 0.0000)
                            {
                                pair.subjectLenaVarsInContact.totalKchiDurWLENA += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiDur;

                            }
                            if (ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiCount > 0.0000 && ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildUttCount > 0.0000)
                            {
                                pair.subjectLenaVarsInContact.totalKchiCountWLENA += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiCount;

                            }

                            pair.partnerLenaVarsInContact.totalTurnCounts += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalTurnCounts;
                            pair.partnerLenaVarsInContact.totalChildUttCount += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildUttCount;
                            pair.partnerLenaVarsInContact.totalChildUttDuration += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildUttDuration;
                            pair.partnerLenaVarsInContact.totalChildCryDuration += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildCryDuration;
                            pair.partnerLenaVarsInContact.totalAdultWordCount += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalAdultWordCount;
                            pair.partnerLenaVarsInContact.totalNoise += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalNoise;
                            pair.partnerLenaVarsInContact.totalOLN += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalOLN;
                            pair.partnerLenaVarsInContact.totalKchiCount += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalKchiCount;
                            pair.partnerLenaVarsInContact.totalKchiDur += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalKchiDur;

                            if (ubiTenths[t][pair.szPartnerMapId].lenaVars.totalKchiDur > 0.0000 && ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildUttDuration > 0.0000)
                            {
                                pair.partnerLenaVarsInContact.totalKchiDurWLENA += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalKchiDur;

                            }
                            if (ubiTenths[t][pair.szPartnerMapId].lenaVars.totalKchiCount > 0.0000 && ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildUttCount > 0.0000)
                            {
                                pair.partnerLenaVarsInContact.totalKchiCountWLENA += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiCount;

                            }
                            if (ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildCryDuration > 0.00 && ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildCryDuration > 0.00)
                            {
                                pair.joinedCry += (ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildCryDuration + ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildCryDuration);
                                pair.subjectJoinedCry += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildCryDuration;
                                pair.partnerJoinedCry += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildCryDuration;

                            }

                            pair.subjectAliceVarsInContact.kchi += ubiTenths[t][pair.szSubjectMapId].aliceVars.kchi;
                            pair.partnerAliceVarsInContact.kchi += ubiTenths[t][pair.szPartnerMapId].aliceVars.kchi;

                            pair.subjectAliceVarsInContact.chi += ubiTenths[t][pair.szSubjectMapId].aliceVars.chi;
                            pair.partnerAliceVarsInContact.chi += ubiTenths[t][pair.szPartnerMapId].aliceVars.chi;

                            pair.subjectAliceVarsInContact.fem += ubiTenths[t][pair.szSubjectMapId].aliceVars.fem;
                            pair.partnerAliceVarsInContact.fem += ubiTenths[t][pair.szPartnerMapId].aliceVars.fem;

                            pair.subjectAliceVarsInContact.mal += ubiTenths[t][pair.szSubjectMapId].aliceVars.mal;
                            pair.partnerAliceVarsInContact.mal += ubiTenths[t][pair.szPartnerMapId].aliceVars.mal;

                            pair.subjectAliceVarsInContact.speech += ubiTenths[t][pair.szSubjectMapId].aliceVars.speech;
                            pair.partnerAliceVarsInContact.speech += ubiTenths[t][pair.szPartnerMapId].aliceVars.speech;



                        }//insocialcontact

                        //if (withinGofR && orientedCloseness)
                        {
                            //FOR SOCIAL ONSETS
                            if (ubiTenths[t][pair.szSubjectMapId].wasTalking || ubiTenths[t][pair.szPartnerMapId].wasTalking)
                            {
                                if (lenaOnsets.ContainsKey(pair.szSubjectMapId))
                                {
                                    int sPos = 0;
                                    if (!onsetPos.ContainsKey(pair.szSubjectMapId))
                                        onsetPos.Add(pair.szSubjectMapId, 0);
                                    else
                                        sPos = onsetPos[pair.szSubjectMapId];

                                    for (; sPos < lenaOnsets[pair.szSubjectMapId].Count; sPos++)
                                    {
                                        if (t.CompareTo(lenaOnsets[pair.szSubjectMapId][sPos].startTime) < 0)
                                            break;
                                        if (t.CompareTo(lenaOnsets[pair.szSubjectMapId][sPos].startTime) >= 0 &&
                                            t.CompareTo(lenaOnsets[pair.szSubjectMapId][sPos].endTime) <= 0)
                                        {

                                            if (withinGofR && orientedCloseness)
                                            {

                                                lenaOnsets[pair.szSubjectMapId][sPos].inSocialContactAnyTalking = true;

                                                if (ubiTenths[t][pair.szSubjectMapId].wasTalking)
                                                {
                                                    lenaOnsets[pair.szSubjectMapId][sPos].inSocialContactSubjectTalking = true;
                                                }

                                                if (personBaseMappings[pair.szPartnerMapId].subjectType == "TEACHER")
                                                {
                                                    if (!lenaOnsets[pair.szSubjectMapId][sPos].teachersInContact.Contains(pair.szPartnerMapId))
                                                    {
                                                        lenaOnsets[pair.szSubjectMapId][sPos].teachersInContact.Add(pair.szPartnerMapId);
                                                    }
                                                }

                                            }
                                            else if (ubiTenths[t][pair.szSubjectMapId].wasTalking && personBaseMappings[pair.szPartnerMapId].subjectType != "TEACHER")
                                            {
                                                lenaOnsets[pair.szSubjectMapId][sPos].outsideKidsSocialContactSubjectTalking = true;
                                            }





                                        }
                                    }
                                    onsetPos[pair.szSubjectMapId] = sPos;
                                }


                                if (lenaOnsets.ContainsKey(pair.szPartnerMapId))
                                {
                                    int pPos = 0;
                                    if (!onsetPos.ContainsKey(pair.szPartnerMapId))
                                        onsetPos.Add(pair.szPartnerMapId, 0);
                                    else
                                        pPos = onsetPos[pair.szPartnerMapId];

                                    for (; pPos < lenaOnsets[pair.szPartnerMapId].Count; pPos++)
                                    {
                                        if (t.CompareTo(lenaOnsets[pair.szPartnerMapId][pPos].startTime) < 0)
                                            break;
                                        if (t.CompareTo(lenaOnsets[pair.szPartnerMapId][pPos].startTime) >= 0 &&
                                            t.CompareTo(lenaOnsets[pair.szPartnerMapId][pPos].endTime) <= 0)
                                        {

                                            if (withinGofR && orientedCloseness)
                                            {
                                                lenaOnsets[pair.szPartnerMapId][pPos].inSocialContactAnyTalking = true;
                                                if (ubiTenths[t][pair.szPartnerMapId].wasTalking)
                                                {
                                                    lenaOnsets[pair.szPartnerMapId][pPos].inSocialContactSubjectTalking = true;
                                                }

                                                if (personBaseMappings[pair.szSubjectMapId].subjectType == "TEACHER")
                                                {
                                                    if (!lenaOnsets[pair.szPartnerMapId][pPos].teachersInContact.Contains(pair.szSubjectMapId))
                                                    {
                                                        lenaOnsets[pair.szPartnerMapId][pPos].teachersInContact.Add(pair.szSubjectMapId);
                                                    }
                                                }

                                            }
                                            else if (ubiTenths[t][pair.szPartnerMapId].wasTalking && personBaseMappings[pair.szSubjectMapId].subjectType != "TEACHER")
                                            {
                                                lenaOnsets[pair.szPartnerMapId][pPos].outsideKidsSocialContactSubjectTalking = true;
                                            }

                                        }
                                    }
                                    onsetPos[pair.szPartnerMapId] = pPos;
                                }

                                ///

                            }

                        }

                        pair.subjectLenaVarsInWUBI.totalTurnCounts += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalTurnCounts;
                        pair.subjectLenaVarsInWUBI.totalChildUttCount += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildUttCount;
                        pair.subjectLenaVarsInWUBI.totalChildUttDuration += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildUttDuration;
                        pair.subjectLenaVarsInWUBI.totalChildCryDuration += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildCryDuration;
                        pair.subjectLenaVarsInWUBI.totalAdultWordCount += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalAdultWordCount;
                        pair.subjectLenaVarsInWUBI.totalNoise += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalNoise;
                        pair.subjectLenaVarsInWUBI.totalOLN += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalOLN;
                        pair.subjectLenaVarsInWUBI.totalKchiDur += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiDur;
                        pair.subjectLenaVarsInWUBI.totalKchiCount += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiCount;
                        if (ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiDur > 0.0000 && ubiTenths[t][pair.szSubjectMapId].lenaVars.totalChildUttDuration > 0.0000)
                        {
                            pair.subjectLenaVarsInWUBI.totalKchiCountWLENA += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiCount;
                            pair.subjectLenaVarsInWUBI.totalKchiDurWLENA += ubiTenths[t][pair.szSubjectMapId].lenaVars.totalKchiDur;

                        }

                        pair.partnerLenaVarsInWUBI.totalTurnCounts += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalTurnCounts;
                        pair.partnerLenaVarsInWUBI.totalChildUttCount += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildUttCount;
                        pair.partnerLenaVarsInWUBI.totalChildUttDuration += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildUttDuration;
                        pair.partnerLenaVarsInWUBI.totalChildCryDuration += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildCryDuration;
                        pair.partnerLenaVarsInWUBI.totalAdultWordCount += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalAdultWordCount;
                        pair.partnerLenaVarsInWUBI.totalNoise += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalNoise;
                        pair.partnerLenaVarsInWUBI.totalOLN += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalOLN;
                        pair.partnerLenaVarsInWUBI.totalKchiDur += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalKchiDur;
                        pair.partnerLenaVarsInWUBI.totalKchiCount += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalKchiCount;

                        if (ubiTenths[t][pair.szPartnerMapId].lenaVars.totalKchiDur > 0.0000 && ubiTenths[t][pair.szPartnerMapId].lenaVars.totalChildUttDuration > 0.0000)
                        {
                            pair.partnerLenaVarsInWUBI.totalKchiCountWLENA += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalKchiCount;
                            pair.partnerLenaVarsInWUBI.totalKchiDurWLENA += ubiTenths[t][pair.szPartnerMapId].lenaVars.totalKchiDur;

                        }

                        pair.subjectAliceVarsInWUBI.kchi += ubiTenths[t][pair.szSubjectMapId].aliceVars.kchi;
                        pair.partnerAliceVarsInWUBI.kchi += ubiTenths[t][pair.szPartnerMapId].aliceVars.kchi;

                        pair.subjectAliceVarsInWUBI.chi += ubiTenths[t][pair.szSubjectMapId].aliceVars.chi;
                        pair.partnerAliceVarsInWUBI.chi += ubiTenths[t][pair.szPartnerMapId].aliceVars.chi;

                        pair.subjectAliceVarsInWUBI.fem += ubiTenths[t][pair.szSubjectMapId].aliceVars.fem;
                        pair.partnerAliceVarsInWUBI.fem += ubiTenths[t][pair.szPartnerMapId].aliceVars.fem;

                        pair.subjectAliceVarsInWUBI.mal += ubiTenths[t][pair.szSubjectMapId].aliceVars.mal;
                        pair.partnerAliceVarsInWUBI.mal += ubiTenths[t][pair.szPartnerMapId].aliceVars.mal;

                        pair.subjectAliceVarsInWUBI.speech += ubiTenths[t][pair.szSubjectMapId].aliceVars.speech;
                        pair.partnerAliceVarsInWUBI.speech += ubiTenths[t][pair.szPartnerMapId].aliceVars.speech;

                        if (doApp)
                        {
                            double dist0 = 0;
                            double dist1 = 0;
                            DateTime dt0 = t.AddMilliseconds(-100);
                            double approachMetersS = 0;
                            double approachMetersP = 0;
                            if (ubiTenths.ContainsKey(dt0) && ubiTenths[dt0].ContainsKey(pair.szPartnerMapId) && ubiTenths[dt0].ContainsKey(pair.szSubjectMapId))
                            {
                                dist1 = Math.Sqrt(Utilities.calcSquaredDist(ubiTenths[t][pair.szSubjectMapId], ubiTenths[dt0][pair.szPartnerMapId]));
                                dist0 = Math.Sqrt(Utilities.calcSquaredDist(ubiTenths[dt0][pair.szSubjectMapId], ubiTenths[dt0][pair.szPartnerMapId]));
                                approachMetersS = dist0 - dist1;
                                if (!Double.IsNaN(approachMetersS))
                                {
                                    pair.subjectDistCount++;
                                    pair.subjectDist += dist0;
                                }

                                if (!Double.IsNaN(approachMetersS))
                                {

                                    ////
                                    String appLine =
                                        personBaseMappings[pair.szSubjectMapId].shortId + "," +
                                        personBaseMappings[pair.szPartnerMapId].shortId + "," +
                                        t.ToLongTimeString() + "," +
                                        t.Millisecond + "," +
                                        dist0 + "," +
                                        dist1 + "," +
                                        approachMetersS + "," +
                                        ubiTenths[dt0][pair.szSubjectMapId].x + "," +
                                        ubiTenths[dt0][pair.szSubjectMapId].y + "," +
                                        ubiTenths[dt0][pair.szPartnerMapId].x + "," +
                                        ubiTenths[dt0][pair.szPartnerMapId].y + "," +
                                        ubiTenths[t][pair.szSubjectMapId].x + "," +
                                        ubiTenths[t][pair.szSubjectMapId].y + "," +
                                        ubiTenths[t][pair.szPartnerMapId].x + "," +
                                        ubiTenths[t][pair.szPartnerMapId].y + "," +
                                        (withinGofR ? "TRUE" : "FALSE") + "," +
                                        (orientedCloseness ? "TRUE" : "FALSE") + "," +
                                        (angles.Item1) + "," +
                                        (angles.Item2) + "," +
                                        personBaseMappings[pair.szSubjectMapId].subjectType + "," +
                                        personBaseMappings[pair.szPartnerMapId].subjectType + "," +
                                        personBaseMappings[pair.szSubjectMapId].gender + "," +
                                        personBaseMappings[pair.szPartnerMapId].gender + "," +
                                        personBaseMappings[pair.szSubjectMapId].diagnosis + "," +
                                        personBaseMappings[pair.szPartnerMapId].diagnosis + "," +
                                        pair.szSubjectMapId + "," +
                                        pair.szPartnerMapId;


                                    swapp.WriteLine(appLine);
                                }


                                dist1 = Math.Sqrt(Utilities.calcSquaredDist(ubiTenths[t][pair.szPartnerMapId], ubiTenths[dt0][pair.szSubjectMapId]));
                                dist0 = Math.Sqrt(Utilities.calcSquaredDist(ubiTenths[dt0][pair.szPartnerMapId], ubiTenths[dt0][pair.szSubjectMapId]));
                                approachMetersP = dist0 - dist1;
                                if (!Double.IsNaN(approachMetersP))
                                {
                                    pair.partnerDistCount++;
                                    pair.partnerDist += dist0;
                                }

                                if (!Double.IsNaN(approachMetersP))
                                {

                                    ////   szSubjectMapId      szPartnerMapId
                                    String appLine =
                                        personBaseMappings[pair.szPartnerMapId].shortId + "," +
                                        personBaseMappings[pair.szSubjectMapId].shortId + "," +
                                        t.ToLongTimeString() + "," +
                                        t.Millisecond + "," +
                                        dist0 + "," +
                                        dist1 + "," +
                                        approachMetersS + "," +
                                        ubiTenths[dt0][pair.szPartnerMapId].x + "," +
                                        ubiTenths[dt0][pair.szPartnerMapId].y + "," +
                                        ubiTenths[dt0][pair.szSubjectMapId].x + "," +
                                        ubiTenths[dt0][pair.szSubjectMapId].y + "," +
                                        ubiTenths[t][pair.szPartnerMapId].x + "," +
                                        ubiTenths[t][pair.szPartnerMapId].y + "," +
                                        ubiTenths[t][pair.szSubjectMapId].x + "," +
                                        ubiTenths[t][pair.szSubjectMapId].y + "," +
                                        (withinGofR ? "TRUE" : "FALSE") + "," +
                                        (orientedCloseness ? "TRUE" : "FALSE") + "," +
                                        (angles.Item1) + "," +
                                        (angles.Item2) + "," +
                                        personBaseMappings[pair.szPartnerMapId].subjectType + "," +
                                        personBaseMappings[pair.szSubjectMapId].subjectType + "," +
                                        personBaseMappings[pair.szPartnerMapId].gender + "," +
                                        personBaseMappings[pair.szSubjectMapId].gender + "," +
                                        personBaseMappings[pair.szPartnerMapId].diagnosis + "," +
                                        personBaseMappings[pair.szSubjectMapId].diagnosis + "," +
                                        pair.szPartnerMapId + "," +
                                        pair.szSubjectMapId;


                                    swapp.WriteLine(appLine);
                                }

                            }
                        }



                    }
                }

                }
            }
            if (doAngles)
                sw.Close();
            if (doApp)
                swapp.Close();

            //DEBUG DELETE ELAN INTERACTIONS
            //swi.Close();
            //DEBUG DELETE ELAN INTERACTIONS
            //Date	Subject	Partner	SubjectShortID	PartnerShortID	SubjectDiagnosis	PartnerDiagnosis	
            //SubjectGender	PartnerGender	SubjectLanguage	PartnerLanguage	Adult	SubjectStatus	PartnerStatus	SubjectType	PartnerType
            return pairs;


        }//

        

        public void setMappings(String dir,String className,Dictionary<String, Person> personMappings, String mapById, int startHour, int endHour, int endMinute)
        {
            Boolean isOldFormat = classDay.Year < 2018 || (classDay.Year == 2018 && classDay.Month < 8);
            String mappingDayFileName = isOldFormat ? dir + "//MAPPING_" + className + "_BASE.CSV" : Utilities.getDayMappingFileName(dir, this.classDay, className);
            if (!mappingsSet)
            {
                personBaseMappings = personMappings;
                //if (this.classDay.Year >= 2023 || (this.classDay.Year == 2022 && this.classDay.Month >= 8))
                {
                    if (!File.Exists(mappingDayFileName))
                    {
                        mappingDayFileName = mappingDayFileName.Substring(mappingDayFileName.LastIndexOf("//") + 2);

                        mappingDayFileName = mappingDayFileName.Replace("OUTSIDE", "").Replace("BASE", "");
                        mappingDayFileName = (mappingDayFileName.IndexOf("_2")>0?mappingDayFileName.Substring(0, mappingDayFileName.IndexOf("_2")+1): mappingDayFileName.Substring(0, mappingDayFileName.IndexOf(".")  )+"_")  +Utilities.getDateStr(classDay, "", 0) +".csv";
                        
                        mappingDayFileName = dir + mappingDayFileName;
                        if (!File.Exists(mappingDayFileName))
                        {
                            String mapDate = mappingDayFileName.Substring(mappingDayFileName.LastIndexOf("_") + 1).Replace(".csv", "").Replace(".CSV", "");
                            if (mapDate.Length == 8)
                            {
                                mappingDayFileName = mappingDayFileName.Replace(mapDate, mapDate.Substring(0, 4) + mapDate.Substring(6, 2));
                            }


                        }

                    }
                     
                    if (File.Exists(mappingDayFileName))
                        using (StreamReader sr = new StreamReader(mappingDayFileName))
                        {
                            Dictionary<String, int> columnIndex = new Dictionary<String, int>();
                            columnIndex.Add("LONGID", -1);
                            columnIndex.Add("STATUS", -1);
                            columnIndex.Add("NODATA", -1);
                            columnIndex.Add("LENA", -1);
                            columnIndex.Add("SONY", -1);
                            columnIndex.Add("LEFT", -1);
                            columnIndex.Add("RIGHT", -1);
                            columnIndex.Add("START", -1);
                            columnIndex.Add("END", -1);

                            if (!sr.EndOfStream)
                            {
                                String commaLine = sr.ReadLine();
                                String[] line = commaLine.Split(',');
                                int cp = line[0].Trim().ToUpper()!="ROSTER"?0:1;
                                foreach (String header in line)
                                {
                                    if (!header.ToUpper().Trim().Contains("ID_VEST_LENA_TAGS"))//sf day mappings done w this column for 2223.... 
                                    {
                                        if (header.ToUpper().Trim().Contains("SUBJECT") && header.ToUpper().Trim().Contains("ID") && (!header.ToUpper().Trim().Contains("SHORT")) && columnIndex["LONGID"] < 0)
                                        {
                                            columnIndex["LONGID"] = cp;
                                        }
                                        else if ((!header.ToUpper().Trim().Contains("LABEL")) && header.ToUpper().Trim().Contains("LEFT") && (header.ToUpper().Trim().Contains("TAG") || header.ToUpper().Trim().Contains("UBI")))
                                        {
                                            columnIndex["LEFT"] = cp;
                                        }
                                        else if ((!header.ToUpper().Trim().Contains("LABEL")) && header.ToUpper().Trim().Contains("RIGHT") && (header.ToUpper().Trim().Contains("TAG") || header.ToUpper().Trim().Contains("UBI")))
                                        {
                                            columnIndex["RIGHT"] = cp;
                                        }
                                        else if (header.ToUpper().Trim()=="LENA"||(header.ToUpper().Trim().Contains("LENA") && header.ToUpper().Trim().Contains("ID")))
                                        {
                                            columnIndex["LENA"] = cp;
                                        }
                                        else if (header.ToUpper().Trim().Contains("SONY"))
                                        {
                                            columnIndex["SONY"] = cp;
                                        }
                                        else if (header.ToUpper().Trim().Contains("STATUS") || (isOldFormat && header.ToUpper().Trim().Contains("ABSENT")))
                                        {
                                            columnIndex["STATUS"] = cp;
                                        }
                                        else if (header.ToUpper().Trim().Contains("START"))
                                        {
                                            columnIndex["START"] = cp;
                                        }
                                        else if (header.ToUpper().Trim().Contains("EXPIRE") || (header.ToUpper().Trim().Contains("END") && (!header.ToUpper().Trim().Contains("GENDER"))))
                                        {
                                            columnIndex["END"] = cp;
                                        }
                                        else if (header.ToUpper().Trim().Contains("NODATA"))
                                        {
                                            columnIndex["NODATA"] = cp;
                                        }
                                    }

                                    cp++;
                                }
                            }

                            while (!sr.EndOfStream)
                            {
                                String commaLine = sr.ReadLine();
                                String[] line = commaLine.Split(',');
                                if (line.Length > 7 && line[1] != "")
                                {
                                    //Person person = personMappings[mapById];//new Person(commaLine, mapById, new List<int>(), new List<int>(),Person.columnIndex);
                                    if (personMappings.ContainsKey(line[columnIndex["LONGID"]].Trim().ToUpper()))
                                    {
                                        Boolean addFlag = true;
                                        String tempCommaLine = commaLine;
                                        if(isOldFormat)
                                        {
                                            String[] tempCommaLineArr = new string[line.Length];// columnIndex["STATUS"]];
                                            String status = line[columnIndex["STATUS"]].Contains(classDay.Month+"/"+ classDay.Day+"/"+ classDay.Year) ? "ABSENT" : "PRESENT";// PRESENT";
                                            String nodata = columnIndex["NODATA"]>=0?(line[columnIndex["NODATA"]].Contains(classDay.Month + "/" + classDay.Day + "/" + classDay.Year) ? "NODATA" : status):status;// PRESENT";
                                            status = status == "PRESENT" && nodata=="NODATA" ? nodata : status;
                                            //Array.Copy(originalArray, startIndex, subArray, 0, length);
                                            Array.Copy(line, 0, tempCommaLineArr, 0, columnIndex["STATUS"]);
                                            Array.Copy(line, columnIndex["STATUS"]+1, tempCommaLineArr, columnIndex["STATUS"] + 1, line.Length - columnIndex["STATUS"]-1);
                                            tempCommaLineArr[columnIndex["STATUS"]] = status;

                                            String szStart = startHour.ToString() + ":00";
                                            String szEnd = endHour.ToString()+":"+endMinute.ToString();
                                            tempCommaLineArr[columnIndex["START"]] = szStart;
                                            tempCommaLineArr[columnIndex["END"]] = szEnd;



                                            tempCommaLine = String.Join(",", tempCommaLineArr);
                                            addFlag = Convert.ToDateTime(line[columnIndex["START"]].Trim()) <= classDay && Convert.ToDateTime(line[columnIndex["END"]].Trim()) > classDay;
                                        }
                                        if(addFlag)
                                        {
                                            Person person = personMappings[line[columnIndex["LONGID"]].ToUpper()];
                                            PersonDayInfo personDayInfo = new PersonDayInfo(tempCommaLine, person.mapId, new DateTime(classDay.Year, classDay.Month, classDay.Day, startHour, 0, 0), new DateTime(classDay.Year, classDay.Month, classDay.Day, endHour, endMinute, 0), columnIndex);
                                            if (!personDayMappings.ContainsKey(person.mapId))
                                                personDayMappings.Add(person.mapId, personDayInfo);
                                        }
                                    }
                                }
                            }
                        }
                }
                 
            }
            mappingsSet = true;

        }
       
        
            public void findTagPerson(ref UbiLocation ubiLocation, DateTime dt)
            {
                foreach(String key in personDayMappings.Keys)
                {
                    PersonDayInfo pdi = personDayMappings[key];

                //00:11:CE:00:00:00:D4:C3	00:11:CE:00:00:00:D5:F4
 

                if (pdi.present && pdi.status=="PRESENT" &&
                        dt>=pdi.startDate && dt<=pdi.endDate)
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

             

        public void makeGofRFilesAndTimeDict(String dir, String szOutputFile, List<String> diagnosisList)
        {

            //DELETE OR DEBUG ??
            //String testDist = szOutputFile.Replace("//SYNC//GR//", "//SYNC//");
            //TextWriter swTest = new StreamWriter(testDist);// countDays > 0);
            //swTest.WriteLine("SUBJECTID,TIME,DISTANCE,KL_X,KR_X");

            String szDayFolder = Utilities.getDateDashStr(classDay);
            TextWriter sw = new StreamWriter(szOutputFile);// countDays > 0);
            TextWriter swCotalk = new StreamWriter(szOutputFile.Replace("GR", "COTALK"));// countDays > 0);
            swCotalk.WriteLine("SUBJECTID,TIME,KC_X,KC_Y,KC_O,VOCCHNCHF_LENAKF");
            string[] ubiLogFiles = Directory.GetFiles(dir + "//" + szDayFolder + "//Ubisense_Denoised_Data//");

            String newDiagnosis = "";
            foreach (String d in diagnosisList)
            {
                newDiagnosis += (d + ",");
            }
            sw.WriteLine("LOCATION,SUBJECTIDANDTAGTYPE,TIME,X,Y,Z," + newDiagnosis+"TYPE");
            /* 
             * 
             * 
             *   sw.WriteLine("Location," +
                                            subjectId + "L," +
                                            lineCols[0] + "," +
                                            lineCols[13] + "," +
                                            lineCols[14] + "," +
                                            lineCols[3]
                                            );
             * foreach (String d in subject.diagnosisList)
            {
                newPairDiagnosis += (d + "," + (partner.diagnosisList.Count > pos ? partner.diagnosisList[pos] : "") + ",");
                newPairDiagnosisP += ((partner.diagnosisList.Count > pos ? partner.diagnosisList[pos] : "") + "," + d + ",");
                pos++;
            }*/

            //TextWriter swl = new StreamWriter(szOutputFile.Replace(".CSV","_L.csv"));// countDays > 0);
            //TextWriter swr = new StreamWriter(szOutputFile.Replace(".CSV", "_R.csv"));// countDays > 0);


            foreach (string file in ubiLogFiles)
            {
                String fileName = Path.GetFileName(file);
                if (fileName.EndsWith(".csv"))
                {
                    int subjectIdx = fileName.IndexOf("_filtered_") + 10;
                    String subjectId = fileName.Substring(fileName.LastIndexOf("_filtered_") + 10).Replace(".csv", "").ToUpper();

                    if (personDayMappings.ContainsKey(subjectId))
                    {
                        using (StreamReader sr = new StreamReader(file))
                        {
                            if (!sr.EndOfStream)
                            {
                                sr.ReadLine();
                            }
                            /*Time,lx,ly,lz,rx,ry,rz,o,dis2d,cx,cy,cz,o_kf,lx_kf,ly_kf,rx_kf,ry_kf,dis2d_kf,cx_kf,cy_kf
                             chn_vocal   chf_vocal adult_vocal chn_vocal_average_dB chf_vocal_average_dB    adult_vocal_average_dB chn_vocal_peak_dB   chf_vocal_peak_dB adult_vocal_peak_dB

                            2022 - 01-28 08:58:00.200,1.3800916038677513,4.36540153126917,0.4596901265437715,0.9715147678818095,4.272437575392713,0.544450214826891,2.917870339559073,0.41901948402966077,1.1758031858747804,4.318919553330941,0.5020701706853312,2.917870339559072,1.3800916038677513,4.36540153126917,0.9715147678818095,4.272437575392712,0.41901948402966077,1.1758031858747804,4.318919553330941*/

                            while (!sr.EndOfStream)
                            {
                                String szLine = sr.ReadLine();
                                String[] lineCols = szLine.Split(',');

                                
                                if (lineCols.Length > 19 && lineCols[19].Trim() != "")
                                {
                                    PersonDayInfo pdi = personDayMappings[subjectId];

                                    Boolean filterOut = this.toFilter && Utilities.specialFilterOut(lineCols[0]);
                                    if (!filterOut)
                                    {
                                        String sDiagnosis = "";
                                        String sType = personBaseMappings[subjectId].subjectType.ToUpper();
                                        foreach (String d in personBaseMappings[subjectId].diagnosisList)
                                        {
                                            sDiagnosis += (d + ",");
                                        }

                                        sw.WriteLine("Location," +
                                            subjectId + "L," +
                                            lineCols[0] + "," +
                                            lineCols[13] + "," +
                                            lineCols[14] + "," +
                                            lineCols[3] + "," + sDiagnosis+ sType
                                            ); ;
                                        sw.WriteLine("Location," +
                                            subjectId + "R," +
                                            lineCols[0] + "," +
                                            lineCols[15] + "," +
                                            lineCols[16] + "," +
                                            lineCols[6] + "," + sDiagnosis+ sType
                                            );


                                       /* swl.WriteLine("Location," +
                                           subjectId + "L," +
                                           lineCols[0] + "," +
                                           lineCols[13] + "," +
                                           lineCols[14] + "," +
                                           lineCols[3]
                                           );

                                        swr.WriteLine("Location," +
                                            subjectId + "R," +
                                            lineCols[0] + "," +
                                            lineCols[15] + "," +
                                            lineCols[16] + "," +
                                            lineCols[6]
                                            );*/

                                        int isTalking = ((lineCols.Length > 20 && lineCols[20].Trim() == "1") || (lineCols.Length > 21 && lineCols[21].Trim() == "1") ? 1 : 0);
                                        swCotalk.WriteLine(
                                        subjectId + "," +
                                        lineCols[0] + "," +
                                        lineCols[18] + "," +
                                        lineCols[19] + "," +
                                        Convert.ToDouble(Convert.ToDouble(lineCols[12]) * (180 / Math.PI)) + "," +
                                        isTalking
                                        );

                                        DateTime timeMs = Utilities.getDate(lineCols[0]);


                                        if (!this.ubiTenths.ContainsKey(timeMs))
                                        {
                                            this.ubiTenths.Add(timeMs, new Dictionary<string, PersonSuperInfo>());
                                        }

                                        if (!this.ubiTenths[timeMs].ContainsKey(subjectId))
                                        {
                                            PersonSuperInfo psi = new PersonSuperInfo();
                                            psi.xl = Convert.ToDouble(lineCols[13]);
                                            psi.yl = Convert.ToDouble(lineCols[14]);
                                            psi.xr = Convert.ToDouble(lineCols[15]);
                                            psi.yr = Convert.ToDouble(lineCols[16]);

                                            psi.x = Convert.ToDouble(lineCols[18]);
                                            psi.y = Convert.ToDouble(lineCols[19]);
                                            psi.z = Convert.ToDouble(lineCols[3]);
                                            psi.orientation_pi = Convert.ToDouble(lineCols[12]);
                                            psi.orientation_deg = Convert.ToDouble(Convert.ToDouble(lineCols[12]) * (180 / Math.PI));
                                            this.ubiTenths[timeMs].Add(subjectId, psi);
                                        }

                                        /*Time,lx,ly,lz,rx,ry,rz,o,dis2d,cx,cy,cz,o_kf,lx_kf,ly_kf,rx_kf,ry_kf,dis2d_kf,cx_kf,cy_kf
                                chn_vocal   chf_vocal adult_vocal chn_vocal_average_dB chf_vocal_average_dB    adult_vocal_average_dB chn_vocal_peak_dB   chf_vocal_peak_dB adult_vocal_peak_dB




                                         swTest.WriteLine("SUBJECTID,TIME,DISTANCE,KL_X,KR_X");
    */
                                        //DELETE OR DEBUG ??
                                        // if (Convert.ToDouble(lineCols[17]) > 2.6)
                                        {
                                            //swTest.WriteLine(subjectId + "," + lineCols[0] + "," + lineCols[17] + "," + lineCols[13] + "," + lineCols[15]);
                                        }
                                    }

                                }

                            }
                        }
                    }
                }
            }

            //swTest.Close();//DELETE OR DEBUG ??


            sw.Close();

           // swl.Close();
            //swr.Close();

            swCotalk.Close();
            this.ubiTenths = this.ubiTenths.OrderBy(x => x.Key).ThenBy(x => x.Key.Millisecond).ToDictionary(x => x.Key, x => x.Value);
        }
        public void makeTimeDict(String dir)
        {
            String szDayFolder = Utilities.getDateDashStr(classDay);
            string[] ubiLogFiles = Directory.GetFiles(dir + "//" + szDayFolder + "//Ubisense_Denoised_Data//");

            foreach (string file in ubiLogFiles)
            {
                String fileName = Path.GetFileName(file);
                if (fileName.EndsWith(".csv"))
                {
                    int subjectIdx = fileName.IndexOf("_filtered_") + 10;
                    String subjectId = fileName.Substring(fileName.LastIndexOf("_filtered_") + 10).Replace(".csv", "").ToUpper();

                    if (personDayMappings.ContainsKey(subjectId))
                    {
                        using (StreamReader sr = new StreamReader(file))
                        {
                            if (!sr.EndOfStream)
                            {
                                sr.ReadLine();
                            }
                            /*Time,lx,ly,lz,rx,ry,rz,o,dis2d,cx,cy,cz,o_kf,lx_kf,ly_kf,rx_kf,ry_kf,dis2d_kf,cx_kf,cy_kf
                             chn_vocal   chf_vocal adult_vocal chn_vocal_average_dB chf_vocal_average_dB    adult_vocal_average_dB chn_vocal_peak_dB   chf_vocal_peak_dB adult_vocal_peak_dB

                            2022 - 01-28 08:58:00.200,1.3800916038677513,4.36540153126917,0.4596901265437715,0.9715147678818095,4.272437575392713,0.544450214826891,2.917870339559073,0.41901948402966077,1.1758031858747804,4.318919553330941,0.5020701706853312,2.917870339559072,1.3800916038677513,4.36540153126917,0.9715147678818095,4.272437575392712,0.41901948402966077,1.1758031858747804,4.318919553330941*/

                            while (!sr.EndOfStream)
                            {
                                String szLine = sr.ReadLine();
                                String[] lineCols = szLine.Split(',');


                                if (lineCols.Length > 19 && lineCols[19].Trim() != "")
                                {
                                    PersonDayInfo pdi = personDayMappings[subjectId];

                                    Boolean filterOut = this.toFilter && Utilities.specialFilterOut(lineCols[0]);
                                    if (!filterOut)
                                    {

                                        int isTalking = ((lineCols.Length > 20 && lineCols[20].Trim() == "1") || (lineCols.Length > 21 && lineCols[21].Trim() == "1") ? 1 : 0);
                                        DateTime timeMs = Utilities.getDate(lineCols[0]);


                                        if (!this.ubiTenths.ContainsKey(timeMs))
                                        {
                                            this.ubiTenths.Add(timeMs, new Dictionary<string, PersonSuperInfo>());
                                        }

                                        if (!this.ubiTenths[timeMs].ContainsKey(subjectId))
                                        {
                                            PersonSuperInfo psi = new PersonSuperInfo();
                                            psi.xl = Convert.ToDouble(lineCols[13]);
                                            psi.yl = Convert.ToDouble(lineCols[14]);
                                            psi.xr = Convert.ToDouble(lineCols[15]);
                                            psi.yr = Convert.ToDouble(lineCols[16]);

                                            psi.x = Convert.ToDouble(lineCols[18]);
                                            psi.y = Convert.ToDouble(lineCols[19]);
                                            psi.z = Convert.ToDouble(lineCols[3]);
                                            psi.orientation_pi = Convert.ToDouble(lineCols[12]);
                                            psi.orientation_deg = Convert.ToDouble(Convert.ToDouble(lineCols[12]) * (180 / Math.PI));
                                            this.ubiTenths[timeMs].Add(subjectId, psi);
                                        }

                                    }

                                }

                            }
                        }
                    }
                }
            }

            this.ubiTenths = this.ubiTenths.OrderBy(x => x.Key).ThenBy(x => x.Key.Millisecond).ToDictionary(x => x.Key, x => x.Value);
        }
        /// <summary>
        /// //////
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="sGrOutputFile"></param>
        public void getTenthsFromUbi(String dir, String sGrOutputFile)
        {
            getTenthsFromUbi( dir,  sGrOutputFile, true);
        }
        public void getTenthsFromUbi(String dir, String sGrOutputFile, Boolean makeGrFile)
        {
            String szDayFolder = Utilities.getDateDashStr(classDay);
            string[] ubiLogFiles = Directory.GetFiles(dir + "//" + szDayFolder + "//Ubisense_Data//");
            Dictionary<String, List<UbiLocation>> ubiLefts = new Dictionary<String, List<UbiLocation>>();
            Dictionary<String, List<UbiLocation>> ubiRights = new Dictionary<String, List<UbiLocation>>();


            TextWriter sw = new StreamWriter(sGrOutputFile, true);


            foreach (string file in ubiLogFiles)
            {
                String fileName = Path.GetFileName(file);
                if (fileName.StartsWith("MiamiLocation") && fileName.EndsWith(".log"))
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        while (!sr.EndOfStream)
                        {
                            //Location,00:11:CE:00:00:00:02:14,2023-02-10 08:50:00.835,1.35892486572266,2.7289354801178,0.25,
                            String szLine = sr.ReadLine();
                            String[] lineCols = szLine.Split(',');
                             
                            //Location,PR_LEAP_2122_T3L,2021-10-25 8:46:01.0,1.6005289068222,5.75430334854126,1
                            if (lineCols.Length > 5 && lineCols[5] != "")
                            {
                                UbiLocation ubiLoc = new UbiLocation();
                                ubiLoc.tag = lineCols[1];
                                DateTime lineTime = Convert.ToDateTime(lineCols[2]);
                                findTagPerson(ref ubiLoc, lineTime);
                                String subject = ubiLoc.id;

                                ubiLoc.time = lineTime;
                                ubiLoc.x = Convert.ToDouble(lineCols[3]);
                                ubiLoc.y = Convert.ToDouble(lineCols[4]);
                                if (ubiLoc.type == "L")
                                {
                                    if (!ubiLefts.ContainsKey(subject))
                                        ubiLefts.Add(subject, new List<UbiLocation>());
                                    ubiLefts[subject].Add(ubiLoc);
                                    /***********************T1HACK**************************************/
                                    if (ubiLoc.tag == "00:11:CE:00:00:00:02:CE" &&
                                        lineTime >= new DateTime(2019, 02, 12, 10, 28, 38, 644) &&
                                        lineTime <= new DateTime(2019, 06, 3))
                                    {
                                        if (!ubiRights.ContainsKey(subject))
                                            ubiRights.Add(subject, new List<UbiLocation>());

                                        UbiLocation ubiLocR = new UbiLocation();
                                        ubiLocR.tag = "00:11:CE:00:00:00:R2:CE";
                                        ubiLocR.time = ubiLoc.time;
                                        ubiLocR.x = ubiLoc.x;
                                        ubiLocR.y = ubiLoc.y;
                                        ubiLocR.type = "R";
                                       
                                        ubiRights[subject].Add(ubiLocR);
                                    }
                                    

                                }
                                else
                                {
                                    if (!ubiRights.ContainsKey(subject))
                                        ubiRights.Add(subject, new List<UbiLocation>());
                                    ubiRights[subject].Add(ubiLoc);
                                }

                            }
                        }
                    }
                }
            }
            Dictionary<String, Tuple<double, double,String>> interpolaionSecs = new Dictionary<String, Tuple<double, double,String>>();
            Dictionary<DateTime, Dictionary<String, PersonInfo>> ubiTenthsL = getTenths(ubiLefts);
            Dictionary<DateTime, Dictionary<String, PersonInfo>> ubiTenthsR = getTenths(ubiRights);
            Dictionary<String, double> subjectInterpolatedSecs = new Dictionary<String, double>();
            foreach (DateTime szTimeStamp in ubiTenthsL.Keys)
            {
                Boolean timeExistsInRights = ubiTenthsR.ContainsKey(szTimeStamp);
                if (timeExistsInRights || (!makeGrFile))
                {
                    foreach (String person in ubiTenthsL[szTimeStamp].Keys)
                    {
                        PersonInfo personInfo = ubiTenthsL[szTimeStamp][person];
                         




                        if (ubiTenthsR.ContainsKey(szTimeStamp) && ubiTenthsR[szTimeStamp].ContainsKey(person))
                        {
                            PersonInfo personInfoR = ubiTenthsR[szTimeStamp][person];
                            if (makeGrFile)
                            {
                                sw.WriteLine("Location," +
                                    person + "L," +
                                    Utilities.getDateStrYYMMDD(personInfo.time, "-", 0) + " " +
                                    Utilities.getTimeStr(personInfo.time) + "," +
                                    personInfo.x + "," +
                                    personInfo.y + "," +
                                    personInfo.z);



                                sw.WriteLine("Location," +
                                        person + "R," +
                                        Utilities.getDateStrYYMMDD(personInfoR.time, "-", 0) + " " +
                                        Utilities.getTimeStr(personInfoR.time) + "," +
                                        personInfoR.x + "," +
                                        personInfoR.y + "," +
                                        personInfoR.z);
                                //4/6/2023 8:31:00 AM   2023-01-24 10:32:55.940   


                                PersonSuperInfo p1 = new PersonSuperInfo();
                                p1.mapId = person;
                                p1.time = personInfo.time;
                                p1.xl = personInfo.x;
                                p1.yl = personInfo.y;
                                p1.xr = personInfoR.x;
                                p1.yr = personInfoR.y;
                                p1.x = Utilities.getCenter(p1.xr, p1.xl);
                                p1.y = Utilities.getCenter(p1.yr, p1.yl);

                                if(!ubiTenths.ContainsKey(personInfo.time))
                                {
                                    recSecs += .1; 
                                    ubiTenths.Add(personInfo.time, new Dictionary<string, PersonSuperInfo>());
                                }
                                if (!ubiTenths[personInfo.time].ContainsKey(person))
                                {
                                    ubiTenths[personInfo.time].Add(person, p1);
                                }

                            }
                            if (!interpolaionSecs.ContainsKey(person))
                            {
                                
                                interpolaionSecs.Add(person, new Tuple<double, double, String>(0, 0, personBaseMappings[person].shortId));

                            }

                            double intSecs = personInfo.interpolated && personInfoR.interpolated ? .1 : 0;
                             
                            Tuple<double, double,String> ii = new Tuple<double, double,String>(interpolaionSecs[person].Item1 + intSecs, interpolaionSecs[person].Item2 + .1, interpolaionSecs[person].Item3);
                            interpolaionSecs[person] = ii;

                            if(classDay.Year < 2021 || (classDay.Year == 2021 && classDay.Month < 8))
                            {
                                personDayMappings[person].maxTime = personDayMappings[person].maxTime < personInfo.time ? personInfo.time : personDayMappings[person].maxTime;
                            }


                        }
                    }
                }


            }
            if (!makeGrFile)
            {
                     
                foreach (String p in interpolaionSecs.Keys)
                {
                    sw.WriteLine(p + ","+ interpolaionSecs[p].Item3+","+classDay + "," + interpolaionSecs[p].Item1 + "," + interpolaionSecs[p].Item2);
                }
            }

                sw.Close();
        }
        public DateTime getTrunkTime()//oldversions
        {
            DateTime end = new DateTime();
            List<DateTime> maxTimes = new List<DateTime>();
            foreach(PersonDayInfo pdi in personDayMappings.Values)
            {
                if(pdi.status=="PRESENT")
                    maxTimes.Add(pdi.maxTime);
            }
            maxTimes = maxTimes.OrderBy(x => x.TimeOfDay).ToList();
            if (maxTimes.Count > 0)
            {
                end = maxTimes[maxTimes.Count - 1];
                foreach (DateTime dt in maxTimes)
                {
                    TimeSpan span = end.Subtract(dt);
                    if (span.Minutes <= 10)
                    {
                        return dt;
                    }
                }
            }
            return end;

        }

        public Dictionary<DateTime, Dictionary<String, PersonInfo>> getTenths(Dictionary<String, List<UbiLocation>> ubiLocations)
        { 
            Dictionary<DateTime, Dictionary<String, PersonInfo>> dayActivities = new Dictionary<DateTime, Dictionary<string, PersonInfo>>();
            foreach (String personId in ubiLocations.Keys)
            {
                List<UbiLocation> ubiLoc = ubiLocations[personId];
                DateTime first = ubiLoc[0].time;//first date from merged file ordered by time
                DateTime last = ubiLoc[ubiLoc.Count - 1].time ;//last date from merged file ordered by time

                //targets will begin at closest 100 ms multiple of start
                int ms = first.Millisecond / 100 * 100 + 100;
                if (first.Millisecond % 100 == 0)
                {
                    ms -= 100;
                }
                DateTime target = new DateTime();//will be next .1 sec
                if (ms == 1000)
                {
                    if (first.Second == 59)
                    {
                        target = new DateTime(first.Year, first.Month, first.Day, first.Hour, first.Minute + 1, 0, 0);
                    }
                    else
                    {
                        target = new DateTime(first.Year, first.Month, first.Day, first.Hour, first.Minute, first.Second + 1, 0);
                    }
                }
                else
                {
                    target = new DateTime(first.Year, first.Month, first.Day, first.Hour, first.Minute, first.Second, ms);
                }
                //Boolean subjFoundClose = true;
                string line = "";
                int lastIndex = 0;
                while (target.CompareTo(last) <= 0)
                {
                    /******/
                    //find next time row based on ms
                    //PersonInfo pi = new PersonInfo();
                    UbiLocation pi = new UbiLocation();
                    pi.time = target;
                    int index = ubiLoc.BinarySearch(pi, new DateTimeComparer());
                    if (index < 0)
                    {
                        index = ~index;
                    }
                     
                    if (index > 0)
                    {

                        //if ((raw[index - 1].dt.Hour == raw[index].dt.Hour) && (Math.Abs(raw[index - 1].dt.Minute - raw[index].dt.Minute) < 2))
                        //if ((Math.Abs(raw[index - 1].dt.Minute - raw[index].dt.Minute) < 2))
                        TimeSpan difference = ubiLoc[index].time.Subtract(ubiLoc[index - 1].time); // could also write `now - otherTime`
                        if (difference.TotalSeconds < 60)
                        {
                            //if (!subjFoundClose)
                            //    sw.WriteLine(line + "," + ubiLoc[index].time.ToLongTimeString() + "," + ubiLoc[index].time.Millisecond);
                            //subjFoundClose = true;

                            Tuple<double, double> targetpoint = linearInterpolate(target, ubiLoc[index - 1].time, ubiLoc[index - 1].x, ubiLoc[index - 1].y, ubiLoc[index].time, ubiLoc[index].x, ubiLoc[index].y);
                            double orientation1 = 0;// ubiLoc[index - 1].ori;
                            double orientation2 = 0;// ubiLoc[index].ori;
                            double targetorientation = linearInterpolate(target, ubiLoc[index - 1].time, orientation1, ubiLoc[index].time, orientation2);
                            PersonInfo pi2 = new PersonInfo();
                          
                            //if (difference.TotalSeconds < 60)
                            {
                                //if (!subjFoundClose)
                                //    sw.WriteLine(line + "," + ubiLoc[index].time.ToLongTimeString() + "," + ubiLoc[index].time.Millisecond);

                                // subjFoundClose = true
                                 
                                if (lastIndex == index)
                                {  
                                    pi2.interpolated = true;
                                }
                                
                                pi2.x = targetpoint.Item1;
                                pi2.y = targetpoint.Item2;

                                pi2.time = target;

                                if (!dayActivities.ContainsKey(target))
                                    dayActivities.Add(target, new Dictionary<string, PersonInfo>());

                                if (!dayActivities[target].ContainsKey(personId))
                                    dayActivities[target].Add(personId, pi2);
                            }

                        }
                       
                    }
                    lastIndex = index;
                    target = target.AddMilliseconds(100);
                }
                 
            }
            return dayActivities;
        }
        public double linearInterpolate(DateTime t, DateTime t1, double y0, DateTime t2, double y1)
        {
            double x0 = t1.Minute * 60000 + t1.Second * 1000 + t1.Millisecond;
            double x1 = t2.Minute * 60000 + t2.Second * 1000 + t2.Millisecond;
            double x = t.Minute * 60000 + t.Second * 1000 + t.Millisecond;
            double lerp = (y0 * (x1 - x) + y1 * (x - x0)) / (x1 - x0);
            return lerp;
        }
        public Tuple<double, double> linearInterpolate(DateTime t, DateTime t1, double xa, double ya, DateTime t2, double xb, double yb)
        {
            double x0 = t1.Minute * 60000 + t1.Second * 1000 + t1.Millisecond;
            double x1 = t2.Minute * 60000 + t2.Second * 1000 + t2.Millisecond;
            double x = t.Minute * 60000 + t.Second * 1000 + t.Millisecond;
            /**** got ms totLA***/

            double y0x = xa;
            double y1x = xb;
            double y0y = ya;
            double y1y = yb;

            double xlerp = (y0x * (x1 - x) + y1x * (x - x0)) / (x1 - x0);
            double ylerp = (y0y * (x1 - x) + y1y * (x - x0)) / (x1 - x0);
            return new Tuple<double, double>(xlerp, ylerp);
        }
        public void makeGofRFilesAndTimeDictFromUbi(String dir, String szOutputFile)
        {
            String szDayFolder = Utilities.getDateDashStr(classDay);
            TextWriter sw = new StreamWriter(szOutputFile,false);// countDays > 0);
            string[] ubiLogFiles = Directory.GetFiles(dir + "//" + szDayFolder + "//Ubisense_Data//");
            //MiamiLocation.2023-02-10_08-42-23-220_filtered.log
            Dictionary<String, List<String>> subjectGrInfo = new Dictionary<String, List<String>>();

            foreach (string file in ubiLogFiles)
            {
                String fileName = Path.GetFileName(file);
                if (fileName.StartsWith("MiamiLocation") && fileName.EndsWith(".log"))
                { 
                    using (StreamReader sr = new StreamReader(file))
                    {
                        while (!sr.EndOfStream)
                        {
                            //Location,00:11:CE:00:00:00:02:14,2023-02-10 08:50:00.835,1.35892486572266,2.7289354801178,0.25,
                            String szLine = sr.ReadLine();
                            String[] lineCols = szLine.Split(',');
                            UbiLocation ubiLoc = new UbiLocation();
                            ubiLoc.tag = lineCols[1];
                            DateTime lineTime = Convert.ToDateTime(lineCols[2]);
                            findTagPerson(ref ubiLoc, lineTime);

                            //Location,PR_LEAP_2122_T3L,2021-10-25 8:46:01.0,1.6005289068222,5.75430334854126,1
                            if (lineCols.Length > 5 && lineCols[5] != "")
                            {
                                String subject = ubiLoc.id;
                                String subjectInfo = "Location," +
                                    ubiLoc.id + ubiLoc.type + "," +
                                    lineCols[2] + "," +
                                    lineCols[3] + "," +
                                    lineCols[4] + "," +
                                    lineCols[5];
                                        
                                if (!subjectGrInfo.ContainsKey(subject))
                                {
                                    subjectGrInfo.Add(subject, new List<string>());
                                }
                                subjectGrInfo[subject].Add(subjectInfo);
                                     
                            }
                        }
                    }
                }
            }

           
            foreach (string subject in subjectGrInfo.Keys)
            {
                foreach (string line in subjectGrInfo[subject])
                {
                    sw.WriteLine(line);
                }
            }

            sw.Close();
        }


        public void createCleanUbiFile(String dir, int startHour, int endHour)//, ref ClassroomDay classroomDay)
        {
            /*String szDayFolder = Utilities.getDateDashStr(classDay);
            String szDenoisedFolder = dir + "//" + szDayFolder + "//Ubisense_Denoised_Data";
              
            if (!Directory.Exists(szDenoisedFolder))
                Directory.CreateDirectory(szDenoisedFolder);
             
            */
            
            String szDayFolder = Utilities.getDateDashStr(classDay);
            String szUnDenoisedFolder = dir + "//" + szDayFolder + "//Ubisense_Data";
            String szUnDenoisedUnfilteredFolder = dir + "//" + szDayFolder + "//Ubisense_Data" + "//Ubisense_Unfiltered_Data";
            String szUnDenoisedFilteredInvalidFolder = dir + "//" + szDayFolder + "//Ubisense_Data" + "//Ubisense_Filtered_Invalid_Data";
            //String szUbiQA = dir + "//Ubisense_QA";

            if (Directory.Exists(szUnDenoisedUnfilteredFolder))
            {
                string[] ufubiLogFiles = Directory.GetFiles(szUnDenoisedUnfilteredFolder+"//");
                foreach (string file in ufubiLogFiles)
                {
                    String fileName = Path.GetFileName(file); 
                    if(!File.Exists(szUnDenoisedFolder + "//" + fileName))
                    File.Move(file, szUnDenoisedFolder + "//" + fileName);
                }
                string[] fubiLogFiles = Directory.GetFiles(szUnDenoisedFolder + "//");
                foreach (string file in fubiLogFiles)
                {
                    String fileName = Path.GetFileName(file);
                    if ((fileName.StartsWith("MiamiLocation") || fileName.StartsWith("MiamiDataLogger")) && fileName.EndsWith("_filtered.log"))
                    {
                        File.Delete(file);
                    }
                }
            }
            else
                Directory.CreateDirectory(szUnDenoisedUnfilteredFolder);

            if (Directory.Exists(szUnDenoisedFilteredInvalidFolder))
            {
                Directory.Delete(szUnDenoisedFilteredInvalidFolder,true);
            }

            Directory.CreateDirectory(szUnDenoisedFilteredInvalidFolder);
        
            //szUbiQA
            //if (!Directory.Exists(szUbiQA))
            {
                //Directory.CreateDirectory(szUbiQA);
            }

            // TextWriter swqa = new StreamWriter(szUbiQA + "//QALOG.csv");
            // swqa.WriteLine("SUBJECT,TAG,LASTTIME,THISTIME,TOTALSECS");

            // TextWriter swqak = new StreamWriter(szUbiQA + "//QALOGKALMANBUG.csv");
            // swqak.WriteLine("SUBJECT,TAG,TAGTYPE,LASTTIME,THISTIME,TOTALSECS,KALMANBUG,THEREISGAPHOLE");

            //Dictionary<String, DateTime> lastTimes = new Dictionary<String, DateTime>();
            //Dictionary<String, Dictionary<String, DateTime>> lastTimesK = new Dictionary<string, Dictionary<string, DateTime>>();
            Dictionary<String, Tuple<String, List<String>>> tagPosTimes = new Dictionary<string, Tuple<string, List<string>>>();

            string[] ubiLogFiles = Directory.GetFiles(dir + "//" + szDayFolder + "//Ubisense_Data//");
            
            
            /***************FOR DEBUGGING UBI LOGS*********************/
            //Dictionary<String,int> logs= new Dictionary<String,int>();
            //Boolean logsExist = File.Exists(dir + "//LOGSBYDATEANDSUBJECT" + Utilities.szVersion + ".csv");
            //TextWriter swLogs = new StreamWriter(dir + "//LOGSBYDATEANDSUBJECT" + Utilities.szVersion + ".csv", true);
            //if (!logsExist)
            //    swLogs.WriteLine("SUBJECT,DATE,LOGS,GOOD_SUBJECTS,LINETYPE");


            foreach (string file in ubiLogFiles)
            {
                String fileName = Path.GetFileName(file);
                if ((fileName.StartsWith("MiamiLocation") || fileName.StartsWith("MiamiDataLogger")) && (!fileName.EndsWith("_filtered.log")) && fileName.EndsWith(".log"))
                {

                    // QAKalman qaKalman = new QAKalman();
                    // qaKalman.qa(file, szUbiQA + "//QALOGKALMAN"+ Utilities.szVersion + ".CSV", startHour, endHour, classDay, ref personDayMappings);



                    TextWriter sw = new StreamWriter(szUnDenoisedFolder + "//"+fileName.Replace(".log", "_filtered.log"));

                    /***************FOR DEBUGGING UBI LOGS*********************
                    TextWriter swInvalidZ = new StreamWriter((dir + "//invalidZ_"+Utilities.szVersion+".csv"),true);
                    TextWriter swInvalid = new StreamWriter(szUnDenoisedFilteredInvalidFolder + "//" + fileName.Replace(".log", "_filtered_invalid.csv"));
                    TextWriter swiRepeated = new StreamWriter(szUnDenoisedFilteredInvalidFolder + "//" + fileName.Replace(".log", "_filtered_invalidRepeated.csv"));
                    TextWriter swiInvalidRepeatedSummary = new StreamWriter(szUnDenoisedFilteredInvalidFolder + "//" + fileName.Replace(".log", "_filtered_invalidRepeatedSummary.csv"));
                    TextWriter swiValidRepeatedSummary = new StreamWriter(szUnDenoisedFilteredInvalidFolder + "//" + fileName.Replace(".log", "_filtered_validRepeatedSummary.csv"));
                     
                     
                    swInvalid.WriteLine("LOCATION,TAG,TIME,X,Y,Z,REPETITIONS,TYPE");
                    swiRepeated.WriteLine("LOCATION,TAG,TIME,X,Y,Z,TYPE");
                    swiInvalidRepeatedSummary.WriteLine("LOCATION,TAG,TIME,X,Y,Z,TIMES,COUNT");
                    swiValidRepeatedSummary.WriteLine("LOCATION,TAG,TIME,X,Y,Z,TIMES,COUNT");*/

                    using (StreamReader sr = new StreamReader(file))
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

                                    findTagPerson(ref ubiLoc, lineTime);


                                    
                                    if (ubiLoc.id != "" &&
                                        lineTime>= personDayMappings[ubiLoc.id].startDate &&
                                        lineTime<= personDayMappings[ubiLoc.id].endDate)
                                    {
                                        
                                        double zVal= Convert.ToDouble(line[5]);
                                        String xyValue = line[3].Trim() + "|" + line[4].Trim();
                                        Tuple<String, List<String>> tagXyTimes = new Tuple<string, List<string>>(xyValue, new List<string>());
                                        tagXyTimes.Item2.Add(line[2].Trim());


                                        Boolean isOldVersion = classDay.Year < 2021 || (classDay.Year == 2021 && classDay.Month < 8);

                                        if (zVal > maxZ && (!isOldVersion))
                                        {
                                            /***************FOR DEBUGGING UBI LOGS*********************
                                             * swInvalid.WriteLine(szLine + xyValue + (tagPosTimes.ContainsKey(tag) && tagPosTimes[tag].Item1 == xyValue ? ",ZANDREPEAT" : ",Z"));
                                            swInvalidZ.WriteLine(szLine);*/
                                        }
                                        else
                                        {
                                            sw.WriteLine(szLine);
                                             
                                            /***************FOR DEBUGGING UBI LOGS*********************
                                             * if(!logs.ContainsKey(ubiLoc.id))
                                            {
                                                logs.Add(ubiLoc.id, 0);
                                            }
                                            logs[ubiLoc.id]++;*/
                                        }


                                        if (!tagPosTimes.ContainsKey(tag))
                                        {
                                            tagPosTimes.Add(tag, tagXyTimes);
                                        }
                                        else if (tagPosTimes[tag].Item1 == xyValue)
                                        {
                                            List<String> times = tagPosTimes[tag].Item2;
                                            times.Add(line[2].Trim());
                                            tagXyTimes = new Tuple<string, List<string>>(xyValue, times);
                                            //swiRepeated.WriteLine(szLine + (zVal > maxZ ? "INVALID" : "VALID"));
                                        }
                                        else
                                        {
                                            if (tagPosTimes[tag].Item2.Count > 1)
                                            {
                                                List<String> times = tagPosTimes[tag].Item2;
                                                String szTimes = "";
                                                foreach (String t in times)
                                                {
                                                    szTimes += (t + "|");
                                                }
                                                /***************FOR DEBUGGING UBI LOGS*********************if (zVal > maxZ)
                                                {
                                                    swiInvalidRepeatedSummary.WriteLine(szLine + szTimes + "," + times.Count);
                                                }
                                                else
                                                {
                                                    swiValidRepeatedSummary.WriteLine(szLine + szTimes + "," + times.Count);
                                                }*/
                                        }
                                        tagPosTimes[tag] = tagXyTimes;
                                        }
 

                                         

                                        /*
                                        Boolean theresBigGap = false;
                                        Boolean theresKalmanBug = false;
                                        if (!lastTimesK.ContainsKey(ubiLoc.id))
                                        {
                                            lastTimesK.Add(ubiLoc.id, new Dictionary<string, DateTime>());
                                            lastTimesK[ubiLoc.id].Add(ubiLoc.type, lineTime);
                                        }
                                        
                                        
                                        if (!lastTimes.ContainsKey(ubiLoc.id))
                                        {
                                            lastTimes.Add(ubiLoc.id, lineTime);
                                        }
                                        else
                                        {
                                            if((lineTime - lastTimes[ubiLoc.id]).TotalSeconds>60)
                                            {
                                                theresBigGap = true;
                                                swqa.WriteLine(ubiLoc.id + "," + tag + "," + (lastTimes[ubiLoc.id].ToLongTimeString().Replace(" ", "." + lastTimes[ubiLoc.id].Millisecond) + "," + (lineTime.ToLongTimeString().Replace(" ", "." + lineTime.Millisecond)))+","+ (lineTime - lastTimes[ubiLoc.id]).TotalSeconds);
                                                
                                                {
                                                    if (lastTimesK[ubiLoc.id].ContainsKey(ubiLoc.type == "L" ? "R" : "L"))
                                                    {
                                                        if ((lineTime - lastTimesK[ubiLoc.id][ubiLoc.type == "L" ? "R" : "L"]).TotalSeconds < 60)
                                                        {
                                                            swqak.WriteLine(ubiLoc.id + "," + tag + "," + ubiLoc.type + "," + (lastTimesK[ubiLoc.id][ubiLoc.type == "L" ? "R" : "L"].ToLongTimeString().Replace(" ", "." + lastTimesK[ubiLoc.id][ubiLoc.type == "L" ? "R" : "L"].Millisecond) + "," + (lineTime.ToLongTimeString().Replace(" ", "." + lineTime.Millisecond))) + "," + (lineTime - lastTimesK[ubiLoc.id][ubiLoc.type == "L" ? "R" : "L"]).TotalSeconds + "," + theresKalmanBug + "," + theresBigGap);

                                                        }
                                                    }

                                                }
                                            }
                                            lastTimes[ubiLoc.id] = lineTime;
                                        }
                                        */


                                                //lastTimesK[ubiLoc.id][ubiLoc.type] = lineTime;

                                            }
                                        }
                            }
                        }

                        
                        sw.Close();
                        /***************FOR DEBUGGING UBI LOGS*********************
                        swInvalidZ.Close();
                        swInvalid.Close();
                        swiRepeated.Close();
                        swiInvalidRepeatedSummary.Close();
                        swiValidRepeatedSummary.Close();*/
                    }
                }
            }
            /*
            foreach (String s in logs.Keys)
            {
                swLogs.WriteLine(s + "," + classDay.ToShortDateString() + "," + logs[s] + ",,GOODUBILOGS");
            }
            swLogs.WriteLine("," + classDay.ToShortDateString() + ",," + logs.Keys.Count + ",GOODSUBJECTS");
             
            swLogs.Close();*/

                        foreach (string file in ubiLogFiles)
            {
                String fileName = Path.GetFileName(file);
                if ((fileName.StartsWith("MiamiLocation") || fileName.StartsWith("MiamiDataLogger")) && (!fileName.EndsWith("_filtered.log")))
                {
                    if(!File.Exists(szUnDenoisedUnfilteredFolder + "//" + fileName))
                    File.Move(file, szUnDenoisedUnfilteredFolder + "//" + fileName);
                }
            }
           // swqa.Close();
           // swqak.Close();
        }
        public void createDenoisedFile(String dir, String className)//, int startHour, int endHour)//, ref ClassroomDay classroomDay)
        {


            String szDayFolder = Utilities.getDateDashStr(classDay);
            String szDenoisedFolder = dir + "//" + szDayFolder + "//Ubisense_Denoised_Data";

            if (!Directory.Exists(szDenoisedFolder))
            {
                
                string[] ubiLogFiles = Directory.GetFiles(dir + "//" + szDayFolder + "//Ubisense_Data//");
                foreach (string file in ubiLogFiles)
                {
                    String ubiFileName = Path.GetFileName(file);
                    if (ubiFileName.StartsWith("MiamiLocation") || ubiFileName.StartsWith("MiamiDataLogger") && ubiFileName.EndsWith(".log"))
                    {
                        string cmd = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.Replace("\\bin\\Debug", ""), "denoisev5.py");
                        string cmdPython = cmd.Replace("denoisev5.py", "\\Python310\\python.exe");
                        String mappingDayFileName = Utilities.getDayMappingFileName(dir, this.classDay, className);
                        string args = mappingDayFileName+" " +
                            dir + "//" + szDayFolder + "//Ubisense_Data//" + ubiFileName + " " +//dir + "//" + szDayFolder + "//Ubisense_Data//MiamiLocation.2021-09-02_08-25-54-579_filtered.log " +
                            dir + "//" + szDayFolder + "//LENA_Data//ITS// " +
                            szDenoisedFolder + "//" + ubiFileName.Replace(".log", ".p");
                        ProcessStartInfo start = new ProcessStartInfo();
                        start.FileName = "C:\\VS\\UL_PROCESSOR_2223\\UL_Processor_V2020\\Python310\\python.exe";// C:\\Users\\lcv31\\AppData\\Local\\Programs\\Python\\Python37";// C:\\Users\\lcv31\\AppData\\Local\\Microsoft\\WindowsApps\\python.exe";//DEBUG CHANGE TO ....
                        //start.FileName = cmdPython;
                        start.Arguments = string.Format("{0} {1}", cmd, args);
                        start.UseShellExecute = false;
                        start.RedirectStandardOutput = true;
                        //

                        using (Process process = Process.Start(start))
                        {
                            Directory.CreateDirectory(szDenoisedFolder);

                            using (StreamReader reader = process.StandardOutput)
                            {
                                string result = reader.ReadToEnd();
                                Console.Write(result);  

                                process.WaitForExit();
                                Console.Write(result);
                            }
                        }

                        
                    }
           


                }


            }
        }
        
        public void mergeAndCleanExistingDenoised(String dir, int startHour, int endHour)//, ref ClassroomDay classroomDay)
        {
            String szDayFolder = Utilities.getDateDashStr(classDay);
            String szDenoisedFolder = dir + "//" + szDayFolder + "//Ubisense_Data_Denoised";
            String szUnfilteredDenoisedFolder = dir + "//" + szDayFolder + "//Ubisense_Data_Denoised//Unfiltered_Data";

            if (Directory.Exists(szDenoisedFolder))
            {
                if (!Directory.Exists(szUnfilteredDenoisedFolder))
                    Directory.CreateDirectory(szUnfilteredDenoisedFolder);

                
                string[] ubiLogFiles = Directory.GetFiles(szDenoisedFolder);
                String fileNameMerged = "";
                foreach (string file in ubiLogFiles)
                {
                    String fileName = Path.GetFileName(file);
                     
                    if (fileName.EndsWith(".csv"))
                    {
                        String subjectId = fileName.Replace("classroom_dataset_", "").Replace(".csv","");
                        PersonDayInfo pdi = personDayMappings[subjectId];

                        TextWriter sw;
                        if(fileNameMerged=="")
                        {
                            fileNameMerged = "MiamiLocationDenoisedFiltered"+ szDayFolder .Replace("-","")+ ".log";
                            sw = new StreamWriter(szDenoisedFolder + "//" + fileNameMerged, false);// countDays > 0);
                        }
                        else
                             sw = new StreamWriter(szDenoisedFolder + "//" + fileNameMerged,true);// countDays > 0);
                        //Location,00:11:CE:00:00:00:A9:F9,2021-09-02 08:25:58.401,5.88419485092163,0.497204780578613,0.405417650938034,
                        //""	Time	lx	ly	lz	rx	ry	rz	o	dis2d	cx	cy	cz	o_kf	lx_kf	ly_kf	rx_kf	ry_kf	dis2d_kf	cx_kf	cy_kf	vocal
                        using (StreamReader sr = new StreamReader(file))
                        {
                            if (!sr.EndOfStream)
                                sr.ReadLine();
                            while (!sr.EndOfStream)
                            {
                                String szLine = sr.ReadLine();
                                String[] line = szLine.Split(',');
                                if (line.Length >= 7 && pdi.rightUbi!="")
                                {
                                    String ltag = pdi.leftUbi;
                                    String rtag = pdi.rightUbi;

                                    DateTime lineTime = Convert.ToDateTime(line[1]);
                                    Double xPosl = Convert.ToDouble(line[2]);
                                    Double yPosl = Convert.ToDouble(line[3]);
                                    Double zPosl = Convert.ToDouble(line[4]);
                                    Double xPosr = Convert.ToDouble(line[5]);
                                    Double yPosr = Convert.ToDouble(line[6]);
                                    Double zPosr = Convert.ToDouble(line[7]);

                                    if (Utilities.isSameDay(lineTime, classDay) &&
                                        lineTime >= pdi.startDate &&
                                        lineTime <= pdi.endDate)
                                    {
                                       sw.WriteLine("Location,"+
                                           ltag+","+
                                           line[1]+","+
                                           xPosl+","+
                                           yPosl+","+
                                           zPosl);
                                        sw.WriteLine("Location," +
                                           rtag + "," +
                                           line[1] + "," +
                                           xPosr + "," +
                                           yPosr + "," +
                                           zPosr);
                                    }
                                }
                            }
                            sw.Close();
                        }
                    }
                }
                foreach (string file in ubiLogFiles)
                {
                    String fileName = Path.GetFileName(file);
                    File.Move(file, szUnfilteredDenoisedFolder + "//" + fileName);
                }
            }

        }

        public void setUbiTagData()
                    {

                    }

        public void readAliceAndGetOnsets(String dir,int startHour, int endHour, int endMinute, Dictionary<String, Tuple<String, DateTime>> lenaStartTimes)
        {
            String szDayFolder = Utilities.getDateDashStr(classDay);
            //String aliceFile = "C:\\IBSS\\LB1718\\diarization_outputLB1718.rttm";
            string[] szAliceFiles = new string[0];
            if (Directory.Exists(dir + "//" + szDayFolder + "//ALICE_Data//"))
            {
                szAliceFiles = Directory.GetFiles(dir + "//" + szDayFolder + "//ALICE_Data//", "*.rttm");
            }
            else if (Directory.Exists(dir + "//ALICE_Data//"))
            {
                szAliceFiles = Directory.GetFiles(dir + "//ALICE_Data//", "*.rttm");
            }

            //if (Directory.Exists(dir + "//" + szDayFolder + "//ALICE_Data//"))
            {
                //szAliceFiles = Directory.GetFiles(dir + "//" + szDayFolder + "//ALICE_Data//", "*.rttm");

                if (szAliceFiles.Length > 0 && File.Exists(szAliceFiles[0]))
                {
                    String aliceFile = szAliceFiles[0];
                    int uttNumber = 0;
                    using (StreamReader sr = new StreamReader(aliceFile))
                    {
                        while (!sr.EndOfStream)
                        {
                            String commaLine = sr.ReadLine();
                            String[] line = commaLine.Split(' ');

                            if (line.Length > 3 && line[1] != "")
                            {
                                String szLenaId = line[1].Trim();
                                szLenaId = szLenaId.Substring(szLenaId.LastIndexOf("_") + 2);
                                if (szLenaId.Substring(0, 1) == "0")
                                    szLenaId = szLenaId.Substring(1);
                                //if (szLenaId == dayMappings[currentDay].subjectMappings[szMapId].lenaId)
                                {
                                    PersonDayInfo pdi = getPersonInfoByLena(szLenaId);
                                    if (pdi.mapId != "")
                                    {
                                        Boolean hasLena = false;
                                        DateTime itsStartTime = new DateTime();
                                        foreach (String itsFileName in lenaStartTimes.Keys)
                                        {
                                            if (lenaStartTimes[itsFileName].Item1 == pdi.mapId)
                                            {
                                                hasLena = true;
                                                itsStartTime = lenaStartTimes[itsFileName].Item2;
                                                break;
                                            }

                                        }
                                        if (hasLena)
                                        {
                                            double aliceDurSecs = Convert.ToDouble(line[4]);
                                            double aliceOnsetSecsStart = Convert.ToDouble(line[3]);
                                            double aliceOnsetSecsEnd = aliceOnsetSecsStart + aliceDurSecs;

                                            DateTime stime = new DateTime(itsStartTime.Year, itsStartTime.Month, itsStartTime.Day, itsStartTime.Hour, itsStartTime.Minute, itsStartTime.Second);
                                            stime = stime.AddSeconds(aliceOnsetSecsStart);
                                            DateTime etime = stime.AddSeconds(aliceOnsetSecsEnd);
                                            String szType = line[7].Trim();

                                            if (szType == "KCHI")
                                            {
                                                LenaOnset lenaOnset = new LenaOnset();
                                                lenaOnset.recStartTime = stime.AddSeconds(aliceOnsetSecsStart);
                                                lenaOnset.startSec = aliceOnsetSecsStart;
                                                lenaOnset.endSec = aliceOnsetSecsEnd;
                                                lenaOnset.id = pdi.mapId;
                                                lenaOnset.type = "ALICE_KCHI";
                                                lenaOnset.durSecs = aliceDurSecs;
                                                lenaOnset.count = 1;
                                                //lenaOnset.count = tc;
                                                lenaOnset.startTime = stime;
                                                lenaOnset.endTime = etime;
                                                //lenaOnset.subjectType = pi.subjectType;
                                                if (!lenaOnsets.ContainsKey(lenaOnset.id))
                                                    lenaOnsets.Add(lenaOnset.id, new List<LenaOnset>());
                                                lenaOnsets[lenaOnset.id].Add(lenaOnset);
                                            }

                                        }
                                    }
                                }

                            }
                        }
                    }
                }
            }

           // LenaOnset lenaOnset = new LenaOnset();
            ////lenaOnset.recStartTime = recStartTime;
            //lenaOnset.startSec = startSecs;
            //lenaOnset.endSec = endSecs;
            //lenaOnset.id = pi.mapId;
            //lenaOnset.type = "Alice_KCHI";
            //lenaOnset.durSecs = bd;
            //lenaOnset.count = tc;
            //lenaOnset.startTime = start;
            //lenaOnset.endTime = end;
            //lenaOnset.subjectType = pi.subjectType;
            //if (!lenaOnsets.ContainsKey(lenaOnset.id))
           //     lenaOnsets.Add(lenaOnset.id, new List<LenaOnset>());
           // lenaOnsets[lenaOnset.id].Add(lenaOnset);


            /*
             * LenaOnset lenaOnset = new LenaOnset();
                                            lenaOnset.itsFile = itsFile;
                                            lenaOnset.lenaId = pdi.lenaId;
                                            lenaOnset.conversationid = convId.ToString();
                                            lenaOnset.recStartTime = recStartTime;
                                            lenaOnset.startSec = startSecs;
                                            lenaOnset.endSec = endSecs;
                                            lenaOnset.segmentDurSecs = 0;
                                            lenaOnset.id = pi.mapId;
                                            lenaOnset.type = "Conversation_turnTaking";
                                            lenaOnset.durSecs = bd;
                                            lenaOnset.tc = tc;
                                            lenaOnset.count = tc;
                                            lenaOnset.startTime = start;
                                            lenaOnset.parentStartTime = start;
                                            lenaOnset.endTime = end;
                                            lenaOnset.avgDb = dbAvg;
                                            lenaOnset.peakDb = dbPeak;
                                            lenaOnset.subjectType = pi.subjectType;
                                            if (!lenaOnsets.ContainsKey(lenaOnset.id))
                                                lenaOnsets.Add(lenaOnset.id, new List<LenaOnset>());
                                            lenaOnsets[lenaOnset.id].Add(lenaOnset);*/
        }
        public Dictionary<String, Tuple<String, DateTime>> readLenaItsAndGetOnsets(String dir,String szOutputFile, int startHour,int endHour, int endMinute )//, ref ClassroomDay classroomDay)
        {//
            Dictionary<String, Tuple<String, DateTime>> lenaStartTimes = new Dictionary<string, Tuple<string, DateTime>>();
            String szDayFolder = Utilities.getDateDashStr(classDay);
            TextWriter sw = new StreamWriter(szOutputFile);// countDays > 0);
            sw.WriteLine("File,Date,Subject,LenaID,SubjectType,ConversationId," +
                "voctype,recstart,startsec,endsec,starttime,endtime,duration," +
                "seg_duration,wordcount,avg_db,avg_peak,turn_taking," +
                "logActivities,children,teachers");//,children,teachers");

            double test1 = 0;

            string[] szLenaItsFiles = Directory.GetFiles(dir + "//"+szDayFolder+ "//LENA_Data//ITS//","*.its");
            foreach (string itsFile in szLenaItsFiles)
            {
                double testTc = 0;
                String szLenaId = Utilities.getLenaIdFromFileName(Path.GetFileName(itsFile));
                
                XmlDocument doc = new XmlDocument();
                doc.Load(itsFile);
                XmlNodeList rec = doc.ChildNodes[0].SelectNodes("ProcessingUnit/Recording") != null ? doc.ChildNodes[0].SelectNodes("ProcessingUnit/Recording") : doc.ChildNodes[2].SelectNodes("ProcessingUnit/Recording");
                double convId = 0;
                foreach (XmlNode recording in rec)
                {
                     
                    double recStartSecs = Convert.ToDouble(recording.Attributes["startTime"].Value.Substring(2, recording.Attributes["startTime"].Value.Length - 3));
                    DateTime recStartTime = DateTime.Parse(recording.Attributes["startClockTime"].Value);
                    var est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    recStartTime = TimeZoneInfo.ConvertTime(recStartTime, est);




                    XmlNodeList nodes = recording.SelectNodes("Conversation|Pause");
                    PersonDayInfo pdi = getPersonInfoByLena(szLenaId);

                    if (pdi.mapId != "")
                    {
                        Person pi = personBaseMappings[pdi.mapId];
    
                        
                        if (Utilities.isSameDay(recStartTime, classDay) &&
                                //recStartTime.Hour >= startHour &&
                                (recStartTime.Hour < endHour ||
                                    (recStartTime.Hour == endHour &&
                                        recStartTime.Minute <= endMinute
                                    )
                                )
                            )
                        { 
                           
                            foreach (XmlNode conv in nodes)
                            {
                                convId++;
                                XmlNodeList segments = conv.SelectNodes("Segment");
                                double startSecs = Convert.ToDouble(conv.Attributes["startTime"].Value.Substring(2, conv.Attributes["startTime"].Value.Length - 3)) - recStartSecs;
                                double endSecs = Convert.ToDouble(conv.Attributes["endTime"].Value.Substring(2, conv.Attributes["endTime"].Value.Length - 3)) - recStartSecs;
                                DateTime start = Utilities.geFullTime(recStartTime.AddSeconds(startSecs));
                                DateTime end = Utilities.geFullTime(recStartTime.AddSeconds(endSecs));
                                double dbAvg = Convert.ToDouble(conv.Attributes["average_dB"].Value);
                                double dbPeak = Convert.ToDouble(conv.Attributes["peak_dB"].Value);
                                double bdSecs = (end - start).Seconds;
                                double bdMilliseconds = (end - start).Milliseconds > 0 ? ((end - start).Milliseconds / 1000.00) : 0.00;  
                                double bd = bdSecs + bdMilliseconds;
                                 
                                if (Utilities.isSameDay(start, classDay) &&
                                start.Hour >= startHour &&
                                (start.Hour < endHour || (start.Hour == endHour && start.Minute <= endMinute)) &&
                                start >= pdi.startDate &&
                                start <= pdi.endDate)
                                {
                                    if (conv.Name == "Conversation")
                                    {
                                        double tc = Convert.ToDouble(conv.Attributes["turnTaking"].Value);;

                                        if (tc > 0)
                                        {
                                            if (!lenaStartTimes.ContainsKey(Path.GetFileName(itsFile)))
                                            {
                                                lenaStartTimes.Add(Path.GetFileName(itsFile), new Tuple<string, DateTime>(pdi.mapId, recStartTime));
                                            }
                                            
                                            testTc += tc;

                                            sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                            classDay + "," +
                                                                            pi.shortId + "," +
                                                                            pdi.lenaId + "," +
                                                                            pi.subjectType + "," +
                                                                            convId +
                                                                            ",Conversation_turnTaking," +
                                                                            Utilities.getTimeStr(recStartTime) + "," +
                                                                            startSecs + "," +
                                                                            endSecs + "," +
                                                                            Utilities.getTimeStr(start) + "," +
                                                                            Utilities.getTimeStr(end) + "," +
                                                                            String.Format("{0:0.00}", 0) + "," +
                                                                            String.Format("{0:0.00}", bd) + "," +
                                                                            "," +
                                                                            String.Format("{0:0.00}", dbAvg) + "," +
                                                                            String.Format("{0:0.00}", dbPeak) + "," +
                                                                            String.Format("{0:0.00}", tc) );
                                            //sw.WriteLine("File,Date,Subject,LenaID,SubjectType,conversationid,voctype,recstart,startsec,endsec,starttime,endtime,duration,seg_duration,count,avg_db,avg_peak,turn_taking ");
                                            LenaOnset lenaOnset = new LenaOnset();
                                            lenaOnset.itsFile = itsFile;
                                            lenaOnset.lenaId = pdi.lenaId;
                                            lenaOnset.conversationid = convId.ToString();
                                            lenaOnset.recStartTime = recStartTime;
                                            lenaOnset.startSec = startSecs;
                                            lenaOnset.endSec = endSecs;
                                            lenaOnset.segmentDurSecs = 0;
                                            lenaOnset.id = pi.mapId;
                                            lenaOnset.type = "Conversation_turnTaking";
                                            lenaOnset.durSecs = bd;
                                            lenaOnset.tc = tc;
                                            lenaOnset.count = tc;
                                            lenaOnset.startTime = start;
                                            lenaOnset.parentStartTime = start;
                                            lenaOnset.endTime = end;
                                            lenaOnset.avgDb = dbAvg;
                                            lenaOnset.peakDb = dbPeak;
                                            lenaOnset.subjectType = pi.subjectType;
                                            if (!lenaOnsets.ContainsKey(lenaOnset.id))
                                                lenaOnsets.Add(lenaOnset.id, new List<LenaOnset>());
                                            lenaOnsets[lenaOnset.id].Add(lenaOnset);

                                            if(tc>0)
                                            {
                                                bool flag = true;
                                            }

                                        }

                                    }


                                    foreach (XmlNode seg in segments)
                                    {
                                         
                                        startSecs = Convert.ToDouble(seg.Attributes["startTime"].Value.Substring(2, seg.Attributes["startTime"].Value.Length - 3)) - recStartSecs;
                                        endSecs = Convert.ToDouble(seg.Attributes["endTime"].Value.Substring(2, seg.Attributes["endTime"].Value.Length - 3)) - recStartSecs;
                                        start = Utilities.geFullTime(recStartTime.AddMilliseconds(startSecs * 1000));
                                        end = Utilities.geFullTime(recStartTime.AddMilliseconds(endSecs * 1000));
                                         

                                        bd = (end - start).Seconds + ((end - start).Milliseconds > 0 ? ((end - start).Milliseconds / 1000.00) : 0); //endSecs - startSecs;
                                        dbAvg = Convert.ToDouble(seg.Attributes["average_dB"].Value);
                                        dbPeak = Convert.ToDouble(seg.Attributes["peak_dB"].Value);
                                        String speaker = seg.Attributes["spkr"].Value;

                                        LenaOnset lenaSegmentOnset = new LenaOnset();
                                        lenaSegmentOnset.itsFile = itsFile;
                                        lenaSegmentOnset.lenaId = pdi.lenaId;
                                        lenaSegmentOnset.conversationid = convId.ToString();
                                        lenaSegmentOnset.recStartTime = recStartTime;
                                        lenaSegmentOnset.startSec = startSecs;
                                        lenaSegmentOnset.endSec = endSecs;
                                        lenaSegmentOnset.id = pi.mapId;
                                        lenaSegmentOnset.startTime = start;
                                        lenaSegmentOnset.parentStartTime = lenaSegmentOnset.startTime;
                                        lenaSegmentOnset.endTime = end;
                                        lenaSegmentOnset.segmentDurSecs = bd;
                                        lenaSegmentOnset.avgDb = dbAvg;
                                        lenaSegmentOnset.peakDb = dbPeak;
                                        lenaSegmentOnset.subjectType = pi.subjectType;
                                        
                                        switch (speaker)
                                        {
                                            case "CHN":
                                            case "CHF":
                                               

                                                double pivd = Convert.ToDouble(seg.Attributes["childUttLen"].Value.Substring(1, seg.Attributes["childUttLen"].Value.Length - 2));
                                                double pivc = Convert.ToDouble(seg.Attributes["childUttCnt"].Value);
                                                //sw.WriteLine("File,Date,Subject,LenaID,SubjectType,conversationid,voctype,recstart,startsec,endsec,starttime,endtime,duration,
                                                //seg_duration,wordcount,avg_db,avg_peak,turn_taking ");
                                                sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                        classDay + "," +
                                                                        pi.shortId + "," +
                                                                        pdi.lenaId + "," +
                                                                        pi.subjectType + "," +
                                                                        convId +
                                                                        ",CHN_CHF SegmentUttCount," +
                                                                        Utilities.getTimeStr(recStartTime) + "," +
                                                                        startSecs + "," +
                                                                        endSecs + "," +
                                                                        Utilities.getTimeStr(start) + "," +
                                                                        Utilities.getTimeStr(end) + "," +
                                                                        String.Format("{0:0.00}", pivd) + "," +
                                                                        String.Format("{0:0.00}", bd) + "," +
                                                                        "," +
                                                                        String.Format("{0:0.00}", dbAvg) + "," +
                                                                        String.Format("{0:0.00}", dbPeak) + "," );

                                                lenaSegmentOnset.type = "CHN_CHF SegmentUttCount";
                                                lenaSegmentOnset.durSecs = pivd;
                                                lenaSegmentOnset.parentStartTime = lenaSegmentOnset.startTime;
                                                lenaSegmentOnset.count = pivc;
                                                if (pivc > 0)
                                                {
                                                    bool stop = true;
                                                }
                                                if (!lenaOnsets.ContainsKey(lenaSegmentOnset.id))
                                                    lenaOnsets.Add(lenaSegmentOnset.id, new List<LenaOnset>());
                                                lenaOnsets[lenaSegmentOnset.id].Add(lenaSegmentOnset);

                                                /*personDayMappings[pi.mapId].totalLenaVars.avgDb += dbAvg;
                                                personDayMappings[pi.mapId].totalLenaVars.maxDb += dbPeak;
                                                personDayMappings[pi.mapId].totalLenaVars.totalSegments += 1;*/
            if (pdi.mapId == "PR_LEAP_1920_AM_1")
                                                {

                                                    test1 += pivc;
                                                }

                                                foreach (XmlAttribute atts in seg.Attributes)
                                                {
                                                    if (atts.Name.IndexOf("startCry") == 0)
                                                    {
                                                        String attStep = atts.Name.Substring(8);
                                                        String att = atts.Name;
                                                        double astartSecs = Convert.ToDouble(seg.Attributes[att].Value.Substring(2, seg.Attributes[att].Value.Length - 3)) - recStartSecs;
                                                        double aendSecs = Convert.ToDouble(seg.Attributes["endCry" + attStep].Value.Substring(2, seg.Attributes["endCry" + attStep].Value.Length - 3)) - recStartSecs;
                                                        DateTime astart = Utilities.geFullTime(recStartTime.AddMilliseconds(astartSecs * 1000));
                                                        DateTime aend = Utilities.geFullTime(recStartTime.AddMilliseconds(aendSecs * 1000));
                                                        double apicry = (aend - astart).Seconds + ((aend - astart).Milliseconds > 0 ? (aend - astart).Milliseconds / 1000.00 : 0); //cendSecs - cstartSecs;
                                                         
                                                        sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                    classDay + "," +
                                                                    pi.shortId + "," +
                                                                    pdi.lenaId + "," +
                                                                    pi.subjectType + "," +
                                                                    convId +
                                                                    ",CHN_CHF CryDur," +
                                                                    Utilities.getTimeStr(recStartTime) + "," +
                                                                    astartSecs + "," +
                                                                    aendSecs + "," +
                                                                    Utilities.getTimeStr(astart) + "," +
                                                                    Utilities.getTimeStr(aend) + "," +
                                                                    String.Format("{0:0.00}", apicry) + "," +
                                                                    String.Format("{0:0.00}", bd) + "," +
                                                                    "," +
                                                                    "," +
                                                                    "," + ","  ); //String.Format("{0:0.00}", dbPeak) + ",");




                                                        LenaOnset lenaAttOnset = new LenaOnset();
                                                        lenaAttOnset.itsFile = itsFile;
                                                        lenaAttOnset.lenaId = pdi.lenaId;
                                                        lenaAttOnset.conversationid = convId.ToString();
                                                        lenaAttOnset.recStartTime = recStartTime;
                                                        lenaAttOnset.startSec = astartSecs;
                                                        lenaAttOnset.endSec = aendSecs;
                                                        lenaAttOnset.id = pi.mapId;
                                                        lenaAttOnset.startTime = astart;
                                                        lenaAttOnset.endTime = aend;
                                                        lenaAttOnset.segmentDurSecs = bd;
                                                        lenaAttOnset.avgDb = dbAvg;
                                                        lenaAttOnset.peakDb = dbPeak;
                                                        lenaAttOnset.type = "CHN_CHF CryDur";
                                                        lenaAttOnset.durSecs = apicry;
                                                        lenaAttOnset.count = 0;
                                                        lenaAttOnset.parentStartTime = lenaSegmentOnset.startTime;

                                                        lenaAttOnset.subjectType = pi.subjectType;
                                                        if (!lenaOnsets.ContainsKey(lenaAttOnset.id))
                                                            lenaOnsets.Add(lenaAttOnset.id, new List<LenaOnset>());
                                                        lenaOnsets[lenaAttOnset.id].Add(lenaAttOnset);

                                                    }
                                                    else if (atts.Name.IndexOf("startUtt") == 0)
                                                    {
                                                        String attStep = atts.Name.Substring(8);
                                                        String att = atts.Name;
                                                        double astartSecs = Convert.ToDouble(seg.Attributes[att].Value.Substring(2, seg.Attributes[att].Value.Length - 3)) - recStartSecs;
                                                        double aendSecs = Convert.ToDouble(seg.Attributes["endUtt" + attStep].Value.Substring(2, seg.Attributes["endUtt" + attStep].Value.Length - 3)) - recStartSecs;
                                                        DateTime astart = Utilities.geFullTime(recStartTime.AddMilliseconds(astartSecs * 1000));
                                                        DateTime aend = Utilities.geFullTime(recStartTime.AddMilliseconds(aendSecs * 1000));
                                                        double apiutts = (aend - astart).Seconds + ((aend - astart).Milliseconds > 0 ? (aend - astart).Milliseconds / 1000.00 : 0); //cendSecs - cstartSecs;
                                                        sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                    classDay + "," +
                                                                    pi.shortId + "," +
                                                                    pdi.lenaId + "," +
                                                                    pi.subjectType + "," +
                                                                    convId +
                                                                    ",CHN_CHF UttDur," +
                                                                    Utilities.getTimeStr(recStartTime) + "," +
                                                                    astartSecs + "," +
                                                                    aendSecs + "," +
                                                                    Utilities.getTimeStr(astart) + "," +
                                                                    Utilities.getTimeStr(aend) + "," +
                                                                    String.Format("{0:0.00}", apiutts) + "," +
                                                                    String.Format("{0:0.00}", bd) + "," +
                                                                    "," +
                                                                    "," +
                                                                    "," + ",");   //String.Format("{0:0.00}", dbPeak) + ",");

                                                        LenaOnset lenaAttOnset = new LenaOnset();
                                                        lenaAttOnset.itsFile = itsFile;
                                                        lenaAttOnset.lenaId = pdi.lenaId;
                                                        lenaAttOnset.conversationid = convId.ToString();
                                                        lenaAttOnset.recStartTime = recStartTime;
                                                        lenaAttOnset.startSec = astartSecs;
                                                        lenaAttOnset.endSec = aendSecs;

                                                        lenaAttOnset.id = pi.mapId;
                                                        lenaAttOnset.startTime = astart;
                                                        lenaAttOnset.endTime = aend;
                                                        lenaAttOnset.segmentDurSecs = bd;
                                                        lenaAttOnset.avgDb = dbAvg;
                                                        lenaAttOnset.peakDb = dbPeak;
                                                        lenaAttOnset.type = "CHN_CHF UttDur";
                                                        lenaAttOnset.durSecs = apiutts;
                                                        lenaAttOnset.count = 0;
                                                        lenaAttOnset.parentStartTime = lenaSegmentOnset.startTime;

                                                        lenaAttOnset.subjectType = pi.subjectType;
                                                        if (!lenaOnsets.ContainsKey(lenaAttOnset.id))
                                                            lenaOnsets.Add(lenaAttOnset.id, new List<LenaOnset>());
                                                        lenaOnsets[lenaAttOnset.id].Add(lenaAttOnset);
                                                    }
                                                }
                                                break;
                                            case "FAN":
                                            case "MAN":
                                                Boolean isFemale = speaker == "FAN";
                                                double piac = isFemale ? Convert.ToDouble(seg.Attributes["femaleAdultWordCnt"].Value) : Convert.ToDouble(seg.Attributes["maleAdultWordCnt"].Value);
                                                double piad = isFemale ? Convert.ToDouble(seg.Attributes["femaleAdultUttLen"].Value.Substring(1, seg.Attributes["femaleAdultUttLen"].Value.Length - 2)) : Convert.ToDouble(seg.Attributes["maleAdultUttLen"].Value.Substring(1, seg.Attributes["maleAdultUttLen"].Value.Length - 2));
                                                piad = bd;

                                                sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                    classDay + "," +
                                                                    pi.shortId + "," +
                                                                    pdi.lenaId + "," +
                                                                    pi.subjectType + "," +
                                                                    convId +
                                                                    (isFemale ? ",FAN SegmentUtt," : ",MAN SegmentUtt,") +
                                                                    Utilities.getTimeStr(recStartTime) + "," +
                                                                    startSecs + "," +
                                                                    endSecs + "," +
                                                                    Utilities.getTimeStr(start) + "," +
                                                                    Utilities.getTimeStr(end) + "," +
                                                                    String.Format("{0:0.00}", piad) + "," +
                                                                    String.Format("{0:0.00}", bd) + "," +
                                                                    String.Format("{0:0.00}", piac) + "," +
                                                                    String.Format("{0:0.00}", dbAvg) + "," +
                                                                    String.Format("{0:0.00}", dbPeak) + "," + ","  );

                                                lenaSegmentOnset.type = (isFemale ? "FAN SegmentUtt" : "MAN SegmentUtt");
                                                lenaSegmentOnset.durSecs = piad;
                                                lenaSegmentOnset.count = piac;
                                                if (!lenaOnsets.ContainsKey(lenaSegmentOnset.id))
                                                    lenaOnsets.Add(lenaSegmentOnset.id, new List<LenaOnset>());
                                                lenaOnsets[lenaSegmentOnset.id].Add(lenaSegmentOnset);


                                                break;


                                            case "CXN":
                                            case "CXF":
                                                sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                     classDay + "," +
                                                                     pi.shortId + "," +
                                                                     pdi.lenaId + "," +
                                                                     pi.subjectType + "," +
                                                                     convId +
                                                                     ",CXN_CXF SegmentUttDur," +
                                                                     Utilities.getTimeStr(recStartTime) + "," +
                                                                     startSecs + "," +
                                                                     endSecs + "," +
                                                                     Utilities.getTimeStr(start) + "," +
                                                                     Utilities.getTimeStr(end) + "," +
                                                                     String.Format("{0:0.00}", 0) + "," +
                                                                     String.Format("{0:0.00}", bd) + "," +
                                                                     "0.00," +
                                                                     String.Format("{0:0.00}", dbAvg) + "," +
                                                                     String.Format("{0:0.00}", dbPeak) + "," + "," );

                                                lenaSegmentOnset.type = "CXN_XF SegmentUttDur";
                                                lenaSegmentOnset.durSecs = lenaSegmentOnset.segmentDurSecs;
                                                lenaSegmentOnset.count = 0;
                                                if (!lenaOnsets.ContainsKey(lenaSegmentOnset.id))
                                                    lenaOnsets.Add(lenaSegmentOnset.id, new List<LenaOnset>());
                                                lenaOnsets[lenaSegmentOnset.id].Add(lenaSegmentOnset);

                                                break;
                                            case "OLN":
                                                sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                    classDay + "," +
                                                                    pi.shortId + "," +
                                                                    pdi.lenaId + "," +
                                                                    pi.subjectType + "," +
                                                                    convId +
                                                                    ",OLN Dur," +
                                                                    Utilities.getTimeStr(recStartTime) + "," +
                                                                    startSecs + "," +
                                                                    endSecs + "," +
                                                                    Utilities.getTimeStr(start) + "," +
                                                                    Utilities.getTimeStr(end) + "," +
                                                                    String.Format("{0:0.00}", 0) + "," +
                                                                    String.Format("{0:0.00}", bd) + "," +
                                                                    "0.00," +
                                                                    String.Format("{0:0.00}", dbAvg) + "," +
                                                                    String.Format("{0:0.00}", dbPeak) + "," + "," );

                                                lenaSegmentOnset.type = "OLN Dur";
                                                lenaSegmentOnset.durSecs = lenaSegmentOnset.segmentDurSecs;
                                                lenaSegmentOnset.count = 0;
                                                if (!lenaOnsets.ContainsKey(lenaSegmentOnset.id))
                                                    lenaOnsets.Add(lenaSegmentOnset.id, new List<LenaOnset>());
                                                lenaOnsets[lenaSegmentOnset.id].Add(lenaSegmentOnset);

                                                break;
                                            case "NON":
                                                sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                   classDay + "," +
                                                                   pi.shortId + "," +
                                                                   pdi.lenaId + "," +
                                                                   pi.subjectType + "," +
                                                                   convId +
                                                                   ",NON Dur," +
                                                                   Utilities.getTimeStr(recStartTime) + "," +
                                                                   startSecs + "," +
                                                                   endSecs + "," +
                                                                   Utilities.getTimeStr(start) + "," +
                                                                   Utilities.getTimeStr(end) + "," +
                                                                   String.Format("{0:0.00}", 0) + "," +
                                                                   String.Format("{0:0.00}", bd) + "," +
                                                                   "0.00," +
                                                                   String.Format("{0:0.00}", dbAvg) + "," +
                                                                   String.Format("{0:0.00}", dbPeak) + "," + ","  );

                                                lenaSegmentOnset.type = "NON Dur";
                                                lenaSegmentOnset.durSecs = lenaSegmentOnset.segmentDurSecs;
                                                lenaSegmentOnset.count = 0;
                                                if (!lenaOnsets.ContainsKey(lenaSegmentOnset.id))
                                                    lenaOnsets.Add(lenaSegmentOnset.id, new List<LenaOnset>());
                                                lenaOnsets[lenaSegmentOnset.id].Add(lenaSegmentOnset);

                                                break;

                                            default:
                                                sw.WriteLine(Path.GetFileName(itsFile) + "," +
                                                                   classDay + "," +
                                                                   pi.shortId + "," +
                                                                   pdi.lenaId + "," +
                                                                   pi.subjectType + "," +
                                                                   convId + "," +
                                                                   speaker + "," +
                                                                   Utilities.getTimeStr(recStartTime) + "," +
                                                                   startSecs + "," +
                                                                   endSecs + "," +
                                                                   Utilities.getTimeStr(start) + "," +
                                                                   Utilities.getTimeStr(end) + "," +
                                                                   String.Format("{0:0.00}", bd) + "," +
                                                                   String.Format("{0:0.00}", bd) + "," +
                                                                   String.Format("{0:0.00}", 0) + "," +
                                                                   String.Format("{0:0.00}", dbAvg) + "," +
                                                                   String.Format("{0:0.00}", dbPeak) + "," + "," );


                                                lenaSegmentOnset.type = speaker;
                                                lenaSegmentOnset.durSecs = lenaSegmentOnset.segmentDurSecs;
                                                lenaSegmentOnset.count = 0;
                                                if (!lenaOnsets.ContainsKey(lenaSegmentOnset.id))
                                                    lenaOnsets.Add(lenaSegmentOnset.id, new List<LenaOnset>());
                                                lenaOnsets[lenaSegmentOnset.id].Add(lenaSegmentOnset);
                                                break;


                                        }
                                    }
                                }
                                else
                                {
                                    //DEBUG DELETE
                                    Boolean stop = true;
                                }

                            }
                        }
                    }

                }

            }
            sw.Close();
            return lenaStartTimes;
        }

        public PersonDayInfo getPersonInfoByLena(String lenaId)
        {
            if(lenaId.StartsWith("0"))
            {
                lenaId = lenaId.Substring(1);
            }
            foreach(PersonDayInfo pdi in personDayMappings.Values)
            {
                if (pdi.lenaId==lenaId && pdi.present && pdi.status == "PRESENT")
                {
                    return pdi;
                }
            }
            return new PersonDayInfo();
        }
        public PersonDayInfo getPersonInfoBySony(String sonyId)
        {
            foreach (PersonDayInfo pdi in personDayMappings.Values)
            {
                if (pdi.sonyId == sonyId && pdi.present && pdi.status == "PRESENT")
                {
                    return pdi;
                }
            }
            return new PersonDayInfo();
        }


        public static DateTime getMsTime(DateTime first)
        {
            //int ms = t.Millisecond > 0 ? t.Millisecond / 100 * 100 : t.Millisecond;// + 100;
            //return new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second, ms);

            //targets will begin at closest 100 ms multiple of start
            int ms = first.Millisecond / 100 * 100 + 100;
            if (first.Millisecond % 100 == 0)
            {
                ms -= 100;
            }
            DateTime target = new DateTime();//will be next .1 sec
            if (ms == 1000)
            {
                if (first.Second < 59)
                {
                    target = new DateTime(first.Year, first.Month, first.Day, first.Hour, first.Minute, first.Second + 1, 0);
                }
                else if (first.Minute < 59)
                {
                    target = new DateTime(first.Year, first.Month, first.Day, first.Hour, first.Minute + 1, 0, 0);
                }
                else
                {
                    target = new DateTime(first.Year, first.Month, first.Day, first.Hour + 1, 0, 0, 0);
                }
            }
            else
            {
                target = new DateTime(first.Year, first.Month, first.Day, first.Hour, first.Minute, first.Second, ms);
            }
             
            return target;
        }
        
        public void setTenthOfSecLENA()//and alice
        {
             
            foreach (String person in lenaOnsets.Keys)
            {
                List<LenaOnset> aliceOnsets = new List<LenaOnset>();
                foreach (LenaOnset lenaOnset in lenaOnsets[person])
                {
                    DateTime time = lenaOnset.startTime;
                    int ms = time.Millisecond > 0 ? time.Millisecond / 100 * 100 : time.Millisecond;// + 100;
                    time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, ms);
                    DateTime timeEnd = lenaOnset.startTime.AddSeconds(lenaOnset.durSecs);
                    ms = timeEnd.Millisecond > 0 ? timeEnd.Millisecond / 100 * 100 : timeEnd.Millisecond;// + 100;
                    timeEnd = new DateTime(timeEnd.Year, timeEnd.Month, timeEnd.Day, timeEnd.Hour, timeEnd.Minute, timeEnd.Second, ms);
 
                    double blockDur = (timeEnd - time).Seconds + ((timeEnd - time).Milliseconds > 0 ? ((timeEnd - time).Milliseconds / 1000.00) : 0);
                    double vocDur10 = (lenaOnset.durSecs / blockDur) / 10;
                    double vocCount10 = (lenaOnset.count / blockDur) / 10;
                    double tcCount10 = (lenaOnset.tc / blockDur) / 10;
 
                     
                    Boolean personExists = personDayMappings.ContainsKey(person);
                    if( personExists && blockDur>0)
                    {
                         
                        switch (lenaOnset.type)
                        {
                            case "ALICE_KCHI":

                                if (personExists)
                                {

                                    personDayMappings[person].totalLenaVars.totalKchiDur += lenaOnset.durSecs;
                                    personDayMappings[person].totalLenaVars.totalKchiCount += lenaOnset.count;
                                    aliceOnsets.Add(lenaOnset);
                                    //personDayMappings[person].totalAliceVars.kchiDur += lenaOnset.durSecs;
                                    //personDayMappings[person].totalAliceVars.kchiCount += lenaOnset.count;
                                }

                                break;
                            case "Conversation_turnTaking":
                                if (personExists)
                                {
                                    personDayMappings[person].totalLenaVars.totalTurnCounts += lenaOnset.tc;
                                }
                                break;

                            case "CHN_CHF CryDur":

                                if (personExists)
                                {
                                    personDayMappings[person].totalLenaVars.totalChildCryDuration += lenaOnset.durSecs;
                                }

                                break;
                            case "CHN_CHF UttDur":

                                if (personExists)
                                {
                                    personDayMappings[person].totalLenaVars.totalChildUttDuration += lenaOnset.durSecs;
                                }

                                break;
                            case "CHN_CHF SegmentUttCount":

                                if (personExists)
                                {
                                    personDayMappings[person].totalLenaVars.totalChildUttCount += lenaOnset.count;
                                }
                                break;
                            case "FAN SegmentUtt":

                                if (personExists)
                                {
                                    personDayMappings[person].totalLenaVars.totalAdultWordCount += lenaOnset.count;
                                }

                                break;
                            case "MAN SegmentUtt":

                                if (personExists)
                                {
                                    personDayMappings[person].totalLenaVars.totalAdultWordCount += lenaOnset.count;
                                }
                                break;

                            case "OLN Dur":

                                if (personExists)
                                {
                                    personDayMappings[person].totalLenaVars.totalOLN += lenaOnset.durSecs;
                                }
                                break;

                            case "NON Dur":

                                if (personExists)
                                {
                                    personDayMappings[person].totalLenaVars.totalNoise += lenaOnset.durSecs;
                                }
                                break;
                        }

                        if (lenaOnset.type != "Conversation_turnTaking" && lenaOnset.type != "CHN_CHF CryDur" && lenaOnset.type != "CHN_CHF UttDur")
                        {
                            personDayMappings[person].totalLenaVars.avgDb += lenaOnset.avgDb;
                            personDayMappings[person].totalLenaVars.maxDb += lenaOnset.peakDb;
                            personDayMappings[person].totalLenaVars.totalSegments += 1;
                        }

                      
     //////
                        do
                        {
                            Boolean WUBI = ubiTenths.ContainsKey(time) && ubiTenths[time].ContainsKey(person);
                            if (WUBI && lenaOnset.type != "Conversation_turnTaking" && lenaOnset.type != "CHN_CHF CryDur" && lenaOnset.type != "CHN_CHF UttDur")
                            {
                                personDayMappings[person].WUBILenaVars.avgDb += lenaOnset.avgDb;
                                personDayMappings[person].WUBILenaVars.maxDb += lenaOnset.peakDb;
                                personDayMappings[person].WUBILenaVars.totalSegments += 1;
                            }
 
                            //if(WUBI)
                            switch (lenaOnset.type)
                            {
                                case "Conversation_turnTaking":
                                   // if (personExists)//DEBUG FIX TAKE OFF
                                    {
                                        //personDayMappings[person].totalLenaVars.totalTurnCounts += tcCount10;
                                         
                                        if (WUBI)
                                        {
                                            ubiTenths[time][person].lenaVars.totalTurnCounts += tcCount10;
                                            personDayMappings[person].WUBILenaVars.totalTurnCounts += tcCount10;
                                        }
                                    }
                                    break;
                                
                                case "CHN_CHF CryDur":

                                    if (personExists)
                                    {
                                         
                                        if (WUBI)
                                        {
                                            ubiTenths[time][person].lenaVars.totalChildCryDuration += vocDur10;
                                            personDayMappings[person].WUBILenaVars.totalChildCryDuration += .1;
                                        }
                                    }

                                    break;

                                
                                case "CHN_CHF UttDur":

                                    if(personExists)
                                    {
                                         
                                        if (WUBI)
                                        {//CHECK WHY THE OR DEBUG
                                            ubiTenths[time][person].wasTalking = lenaOnset.durSecs > 0 || ubiTenths[time][person].wasTalking;//, vocCount10, turnCount10, vocDur10, adults10, noise10;
                                            ubiTenths[time][person].lenaVars.totalChildUttDuration += vocDur10;
                                            personDayMappings[person].WUBILenaVars.totalChildUttDuration += vocDur10;
                                        }
                                         
                                    }
                                    
                                    break;
                                case "CHN_CHF SegmentUttCount":

                                    if (personExists)
                                    {
                                        if (WUBI)
                                        {
                                            ubiTenths[time][person].lenaVars.totalChildUttCount += vocCount10;
                                            personDayMappings[person].WUBILenaVars.totalChildUttCount += vocCount10;
                                        }
                                         
                                    } 
                                    break;
                                case "FAN SegmentUtt":

                                    if (personExists)
                                    {
                                         
                                        if (WUBI)
                                        {
                                            ubiTenths[time][person].lenaVars.totalAdultWordCount += vocCount10;
                                            personDayMappings[person].WUBILenaVars.totalAdultWordCount += vocCount10;
                                        }
                                    }
                                     
                                    break;
                                case "MAN SegmentUtt":

                                    if (personExists)
                                    {
                                         
                                        if (WUBI)
                                        {
                                            ubiTenths[time][person].lenaVars.totalAdultWordCount += vocCount10;
                                            personDayMappings[person].WUBILenaVars.totalAdultWordCount += vocCount10;
                                        }
                                    }

                                     
                                    break;

                                case "OLN Dur":

                                    if (personExists)
                                    {
                                        
                                        if (WUBI)
                                        {
                                            ubiTenths[time][person].lenaVars.totalOLN += vocDur10;
                                            personDayMappings[person].WUBILenaVars.totalOLN += vocDur10;
                                        }
                                    }


                                    break;

                                case "NON Dur":

                                    if (personExists)
                                    {
                                        
                                        if (WUBI)
                                        {
                                            ubiTenths[time][person].lenaVars.totalNoise += vocDur10;
                                            personDayMappings[person].WUBILenaVars.totalNoise += vocDur10;
                                        }
                                    }


                                    break;
                            }
                           
                            time = time.AddMilliseconds(100);
                            //vocDur -= 0.1;
                            blockDur = Math.Round(blockDur- 0.1,2);

                             
                        } while (blockDur > 0);
                    }

                }
                foreach (LenaOnset lenaOnset in aliceOnsets)
                {
                    DateTime time = lenaOnset.startTime;
                    int ms = time.Millisecond > 0 ? time.Millisecond / 100 * 100 : time.Millisecond;// + 100;
                    time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, ms);
                    DateTime timeEnd = lenaOnset.startTime.AddSeconds(lenaOnset.durSecs);
                    ms = timeEnd.Millisecond > 0 ? timeEnd.Millisecond / 100 * 100 : timeEnd.Millisecond;// + 100;
                    timeEnd = new DateTime(timeEnd.Year, timeEnd.Month, timeEnd.Day, timeEnd.Hour, timeEnd.Minute, timeEnd.Second, ms);

                    double blockDur = (timeEnd - time).Seconds + ((timeEnd - time).Milliseconds > 0 ? ((timeEnd - time).Milliseconds / 1000.00) : 0);
                    double vocDur10 = (lenaOnset.durSecs / blockDur) / 10;
                    double vocCount10 = (lenaOnset.count / blockDur) / 10;
                    double tcCount10 = (lenaOnset.tc / blockDur) / 10;
                     
                        do
                        {
                        Boolean WUBI = ubiTenths.ContainsKey(time) && ubiTenths[time].ContainsKey(person);

                        if (WUBI)
                        {
                            ubiTenths[time][person].lenaVars.totalKchiDur += vocDur10;
                            personDayMappings[person].WUBILenaVars.totalKchiDur += vocDur10;

                            if (vocDur10 > 0.0000 &&
                                ubiTenths[time][person].lenaVars.totalChildUttDuration > 0.0000)
                            {
                                ubiTenths[time][person].lenaVars.totalKchiDurWLENA += vocDur10;
                                personDayMappings[person].WUBILenaVars.totalKchiDurWLENA += vocDur10;
                                personDayMappings[person].totalLenaVars.totalKchiDurWLENA += vocDur10;
                            }

                            ubiTenths[time][person].lenaVars.totalKchiCount += vocCount10;
                            personDayMappings[person].WUBILenaVars.totalKchiCount += vocCount10;

                            if (vocCount10 > 0.0000 &&
                                ubiTenths[time][person].lenaVars.totalChildUttCount > 0.0000)
                            {
                                ubiTenths[time][person].lenaVars.totalKchiCountWLENA += vocCount10;
                                personDayMappings[person].WUBILenaVars.totalKchiCountWLENA += vocCount10;
                                personDayMappings[person].totalLenaVars.totalKchiCountWLENA += vocDur10;
                            }
                        }
                            time = time.AddMilliseconds(100);
                            //vocDur -= 0.1;
                            blockDur = Math.Round(blockDur - 0.1, 2);
                            

                        } while (blockDur > 0);
                    }

                }
            }
        public void setTenthOfSecALICE()
        {

            foreach (String person in lenaOnsets.Keys)
            {

                List<Tuple<DateTime, DateTime, double, double>> tcOnsets = new List<Tuple<DateTime, DateTime, double, double>>();


                foreach (LenaOnset lenaOnset in lenaOnsets[person])
                {
                    DateTime time = lenaOnset.startTime;
                    int ms = time.Millisecond > 0 ? time.Millisecond / 100 * 100 : time.Millisecond;// + 100;
                    time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, ms);
                    DateTime timeEnd = lenaOnset.startTime.AddSeconds(lenaOnset.durSecs);
                    ms = timeEnd.Millisecond > 0 ? timeEnd.Millisecond / 100 * 100 : timeEnd.Millisecond;// + 100;
                    timeEnd = new DateTime(timeEnd.Year, timeEnd.Month, timeEnd.Day, timeEnd.Hour, timeEnd.Minute, timeEnd.Second, ms);

                    double blockDur = (timeEnd - time).Seconds + ((timeEnd - time).Milliseconds > 0 ? ((timeEnd - time).Milliseconds / 1000.00) : 0);
                    double vocDur10 = (lenaOnset.durSecs / blockDur) / 10;
                    double vocCount10 = (lenaOnset.count / blockDur) / 10;
                    double tcCount10 = (lenaOnset.tc / blockDur) / 10;


                    Boolean personExists = personDayMappings.ContainsKey(person);
                    if (personExists && blockDur > 0)
                    {

                        switch (lenaOnset.type)
                        {
                            case "ALICE_KCHI":

                                if (personExists)
                                {
                                    personDayMappings[person].totalLenaVars.totalChildUttDuration += lenaOnset.durSecs;
                                    personDayMappings[person].totalLenaVars.totalChildUttCount += lenaOnset.count;
                                }

                                break; 
                        }

                        do
                        {
                            Boolean WUBI = ubiTenths.ContainsKey(time) && ubiTenths[time].ContainsKey(person);
                             
                            switch (lenaOnset.type)
                            {
                                case "ALICE_KCHI":

                                    if (personExists)
                                    {
                                        if (WUBI)
                                        {//CHECK WHY THE OR DEBUG
                                            ubiTenths[time][person].wasTalking = lenaOnset.durSecs > 0 || ubiTenths[time][person].wasTalking;//, vocCount10, turnCount10, vocDur10, adults10, noise10;
                                            ubiTenths[time][person].lenaVars.totalChildUttDuration += vocDur10;
                                            ubiTenths[time][person].lenaVars.totalChildUttCount += vocCount10;
                                            personDayMappings[person].WUBILenaVars.totalChildUttDuration += vocDur10;
                                            personDayMappings[person].WUBILenaVars.totalChildUttCount += vocCount10;

                                          }
                                    }

                                    break;
                            }

                            time = time.AddMilliseconds(100);
                            //vocDur -= 0.1;
                            blockDur = Math.Round(blockDur - 0.1, 2);


                        } while (blockDur > 0);
                    }

                }

            }

        }
        public void setTenthOfSecLENAALICE(String dir, String className, Dictionary<String, Tuple<String, DateTime>> lenaStartTimes)
        {
            String beepOnsetFile = dir + "//BEEPONSETS_" + className + ".csv";
            Dictionary<String, double> sonyOnsetSecs = new Dictionary<String, double>();
            Dictionary<String, DateTime> sonyStartTimes = new Dictionary<String, DateTime>();
            Dictionary<String, String> subjectToSonyFile = new Dictionary<String, String>();
            DateTime lenaBeepTime = DateTime.Now;
            double lenaBeepOnset = 0;
            String szLenaSubjectFound = "";
            if (File.Exists(beepOnsetFile))
                using (StreamReader sr = new StreamReader(beepOnsetFile))
                {
                    if (!sr.EndOfStream)
                    {
                        String commaLine = sr.ReadLine();
                        String[] line = commaLine.Split(',');

                        while (!sr.EndOfStream)
                        {
                            commaLine = sr.ReadLine();
                            line = commaLine.Split(',');
                            if (line.Length > 3 && line[1] != "")
                            {
                                try
                                {
                                    if (lenaBeepOnset == 0)
                                        foreach (String itsFileName in lenaStartTimes.Keys)
                                        {
                                            if (itsFileName == line[1].Trim().Replace(".wav", ".its"))
                                            {
                                                szLenaSubjectFound = lenaStartTimes[itsFileName].Item1;
                                                lenaBeepOnset = Convert.ToDouble(line[2]);
                                                lenaBeepTime = lenaStartTimes[itsFileName].Item2;
                                                break;
                                            }
                                        }
                                    if (line[3].Trim().Contains(Utilities.getDateDashStr(this.classDay)))
                                    {
                                        String sonyNumber = line[1].Substring(0, line[1].IndexOf("_"));
                                        if (sonyNumber.StartsWith("0"))
                                        {
                                            sonyNumber = sonyNumber.Substring(1);
                                        }
                                        sonyNumber = sonyNumber.Trim();
                                        foreach (String sk in this.personDayMappings.Keys)
                                        {
                                            if (personDayMappings[sk].sonyId == sonyNumber)
                                            {
                                                if (!sonyOnsetSecs.ContainsKey(sk))
                                                {
                                                    sonyOnsetSecs.Add(sk, Convert.ToDouble(line[2]));
                                                    subjectToSonyFile.Add(line[1].Replace(".wav", "").Trim(), sk);
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
            //DEBUG ADD if (lenaBeepOnset != 0)
            foreach (String sonyUser in sonyOnsetSecs.Keys)
            {
                DateTime sonyStartTime = lenaBeepTime.AddSeconds(-sonyOnsetSecs[sonyUser]);

                sonyStartTimes.Add(sonyUser, sonyStartTime);
            }
            String szDayFolder = Utilities.getDateDashStr(classDay);

            string[] szAliceFiles = Directory.GetFiles(dir + "//" + szDayFolder + "//Alice_Data//", "*.rttm");
            foreach (String aliceFile in szAliceFiles)
            {
                using (StreamReader sr = new StreamReader(aliceFile))
                {
                    while (!sr.EndOfStream)
                    {
                        String szLine = sr.ReadLine();
                        String[] line = szLine.Split(' ');
                        if (line.Length >= 7)
                        {
                            if (subjectToSonyFile.ContainsKey(line[1].Trim()))
                            {
                                try
                                {
                                    String subject = subjectToSonyFile[line[1].Trim()];
                                    String szSartTime = line[3];
                                    String szBlockDur = line[4];
                                    String szType = line[7];
                                    PersonDayInfo pdi = personDayMappings[subject];
                                    double blockDur = Convert.ToDouble(szBlockDur);
                                    DateTime startTime = sonyStartTimes[subject];
                                    int ms = startTime.Millisecond > 0 ? startTime.Millisecond / 100 * 100 : startTime.Millisecond;// + 100;
                                    startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, startTime.Second, ms);
                                    startTime = startTime.AddSeconds(Convert.ToDouble(szSartTime));
                                    DateTime timeEnd = startTime.AddSeconds(blockDur);
                                    ms = timeEnd.Millisecond > 0 ? timeEnd.Millisecond / 100 * 100 : timeEnd.Millisecond;// + 100;
                                    timeEnd = new DateTime(timeEnd.Year, timeEnd.Month, timeEnd.Day, timeEnd.Hour, timeEnd.Minute, timeEnd.Second, ms);
                                    DateTime time = startTime;

                                    do
                                    {
                                        /*PersonDayInfo pdi = getPersonInfoByLena(szLenaId);
                 start >= pdi.startDate &&
                             start <= pdi.endDate)*/
                                        if (time >= pdi.startDate &&
                                            time <= pdi.endDate)
                                        {
                                            Boolean WUBI = ubiTenths.ContainsKey(time) && ubiTenths[time].ContainsKey(subject);
                                            if (WUBI)
                                            {
                                                bool stop = true;
                                            }


                                            switch (szType)
                                            {
                                                case "KCHI":
                                                    personDayMappings[subject].totalAliceVars.kchi += .1;
                                                    if (WUBI)
                                                    {
                                                        ubiTenths[time][subject].aliceVars.kchi += .1;
                                                        personDayMappings[subject].WUBIAliceVars.kchi += .1;
                                                    }
                                                    break;
                                                case "CHI":
                                                    personDayMappings[subject].totalAliceVars.chi += .1;
                                                    if (WUBI)
                                                    {
                                                        ubiTenths[time][subject].aliceVars.chi += .1;
                                                        personDayMappings[subject].WUBIAliceVars.chi += .1;
                                                    }
                                                    break;
                                                case "FEM":
                                                    personDayMappings[subject].totalAliceVars.fem += .1;
                                                    if (WUBI)
                                                    {
                                                        ubiTenths[time][subject].aliceVars.fem += .1;
                                                        personDayMappings[subject].WUBIAliceVars.fem += .1;
                                                    }
                                                    break;
                                                case "MAL":
                                                    personDayMappings[subject].totalAliceVars.mal += .1;
                                                    if (WUBI)
                                                    {
                                                        ubiTenths[time][subject].aliceVars.mal += .1;
                                                        personDayMappings[subject].WUBIAliceVars.mal += .1;
                                                    }
                                                    break;
                                                case "SPEECH":
                                                    personDayMappings[subject].totalAliceVars.speech += .1;
                                                    if (WUBI)
                                                    {
                                                        ubiTenths[time][subject].aliceVars.speech += .1;
                                                        personDayMappings[subject].WUBIAliceVars.speech += .1;
                                                    }
                                                    break;

                                            }
                                        }
                                        time = time.AddMilliseconds(100);
                                        blockDur = Math.Round(blockDur - 0.1, 2);
                                    } while (blockDur > 0);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);

                                }
                                /*DateTime time = lenaOnset.startTime;
                    int ms = time.Millisecond > 0 ? time.Millisecond / 100 * 100 : time.Millisecond;// + 100;
                    time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, ms);
                    DateTime timeEnd = lenaOnset.startTime.AddSeconds(lenaOnset.durSecs);
                    ms = timeEnd.Millisecond > 0 ? timeEnd.Millisecond / 100 * 100 : timeEnd.Millisecond;// + 100;
                    timeEnd = new DateTime(timeEnd.Year, timeEnd.Month, timeEnd.Day, timeEnd.Hour, timeEnd.Minute, timeEnd.Second, ms);

                    double blockDur = (timeEnd - time).Seconds + ((timeEnd - time).Milliseconds > 0 ? ((timeEnd - time).Milliseconds / 1000.00) : 0);
                    double vocDur10 = (lenaOnset.durSecs / blockDur) / 10;
                    double vocCount10 = (lenaOnset.count / blockDur) / 10;
                    double tcCount10 = (lenaOnset.tc / blockDur) / 10;


                    Boolean personExists = personDayMappings.ContainsKey(person);
                    if( personExists && blockDur>0)
                    {

                        switch (lenaOnset.type)
                        {*/
                            }
                        }
                    }
                }
            }



        }
        public void setTenthOfSecALICE(String dir, String className, Dictionary<String, Tuple<String, DateTime>> lenaStartTimes)
        {
            String beepOnsetFile = dir + "//BEEPONSETS_" + className + ".csv";
            Dictionary<String, double> sonyOnsetSecs = new Dictionary<String, double>();
            Dictionary<String, DateTime> sonyStartTimes = new Dictionary<String, DateTime>();
            Dictionary<String, String> subjectToSonyFile = new Dictionary<String, String>();
            DateTime lenaBeepTime = DateTime.Now;
            double lenaBeepOnset = 0;
            String szLenaSubjectFound = "";
            if (File.Exists(beepOnsetFile))
                using (StreamReader sr = new StreamReader(beepOnsetFile))
                {
                    if (!sr.EndOfStream)
                    {
                        String commaLine = sr.ReadLine();
                        String[] line = commaLine.Split(',');

                        while (!sr.EndOfStream)
                        {
                            commaLine = sr.ReadLine();
                            line = commaLine.Split(',');
                            if (line.Length > 3 && line[1] != "")
                            {
                                try 
                                {
                                    if(lenaBeepOnset==0)
                                    foreach(String itsFileName in lenaStartTimes.Keys)
                                    {
                                            if(itsFileName== line[1].Trim().Replace(".wav", ".its"))
                                            {
                                                szLenaSubjectFound = lenaStartTimes[itsFileName].Item1;
                                                lenaBeepOnset = Convert.ToDouble(line[2]);
                                                lenaBeepTime = lenaStartTimes[itsFileName].Item2;
                                                break;
                                            }
                                    }
                                    if (line[3].Trim().Contains(Utilities.getDateDashStr(this.classDay)))
                                    {
                                        String sonyNumber = line[1].Substring(0, line[1].IndexOf("_"));
                                        if (sonyNumber.StartsWith("0"))
                                        {
                                            sonyNumber = sonyNumber.Substring(1);
                                        }
                                        sonyNumber = sonyNumber.Trim();
                                        foreach (String sk in this.personDayMappings.Keys)
                                        {
                                            if (personDayMappings[sk].sonyId == sonyNumber)
                                            {
                                                if (!sonyOnsetSecs.ContainsKey(sk))
                                                {
                                                    sonyOnsetSecs.Add(sk, Convert.ToDouble(line[2]));
                                                    subjectToSonyFile.Add(line[1].Replace(".wav","").Trim(), sk) ;
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
           //DEBUG ADD if (lenaBeepOnset != 0)
                foreach (String sonyUser in sonyOnsetSecs.Keys)
                {
                    DateTime sonyStartTime = lenaBeepTime.AddSeconds(-sonyOnsetSecs[sonyUser]);

                    sonyStartTimes.Add(sonyUser, sonyStartTime);
                }
            String szDayFolder = Utilities.getDateDashStr(classDay);

            string[] szAliceFiles = Directory.GetFiles(dir + "//" + szDayFolder + "//Alice_Data//", "*.rttm");
            foreach(String aliceFile in szAliceFiles)
            {
                using (StreamReader sr = new StreamReader(aliceFile))
                { 
                    while (!sr.EndOfStream)
                    {
                        String szLine = sr.ReadLine();
                        String[] line = szLine.Split(' ');
                        if (line.Length >= 7)
                        {
                            if (subjectToSonyFile.ContainsKey(line[1].Trim()))
                            {
                                try
                                {
                                    String subject = subjectToSonyFile[line[1].Trim()];
                                    String szSartTime = line[3];
                                    String szBlockDur = line[4];
                                    String szType = line[7];
                                    PersonDayInfo pdi = personDayMappings[subject];
                                    double blockDur = Convert.ToDouble(szBlockDur);
                                    DateTime startTime = sonyStartTimes[subject];
                                    int ms = startTime.Millisecond > 0 ? startTime.Millisecond / 100 * 100 : startTime.Millisecond;// + 100;
                                    startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, startTime.Second, ms);
                                    startTime=startTime.AddSeconds(Convert.ToDouble(szSartTime));
                                    DateTime timeEnd = startTime.AddSeconds(blockDur);
                                    ms = timeEnd.Millisecond > 0 ? timeEnd.Millisecond / 100 * 100 : timeEnd.Millisecond;// + 100;
                                    timeEnd = new DateTime(timeEnd.Year, timeEnd.Month, timeEnd.Day, timeEnd.Hour, timeEnd.Minute, timeEnd.Second, ms);
                                    DateTime time = startTime;
                                     
                                    do
                                    {
                                        /*PersonDayInfo pdi = getPersonInfoByLena(szLenaId);
                 start >= pdi.startDate &&
                             start <= pdi.endDate)*/
                                        if (time >= pdi.startDate &&
                                            time <= pdi.endDate)
                                        {
                                            Boolean WUBI = ubiTenths.ContainsKey(time) && ubiTenths[time].ContainsKey(subject);
                                            if (WUBI)
                                            {
                                                bool stop = true;
                                            }
                                            
                                            
                                            switch (szType)
                                            {
                                                case "KCHI":
                                                    personDayMappings[subject].totalAliceVars.kchi += .1;
                                                    if (WUBI)
                                                    {
                                                        ubiTenths[time][subject].aliceVars.kchi += .1;
                                                        personDayMappings[subject].WUBIAliceVars.kchi += .1;
                                                    }
                                                    break;
                                                case "CHI":
                                                    personDayMappings[subject].totalAliceVars.chi += .1;
                                                    if (WUBI)
                                                    {
                                                        ubiTenths[time][subject].aliceVars.chi += .1;
                                                        personDayMappings[subject].WUBIAliceVars.chi += .1;
                                                    }
                                                    break;
                                                case "FEM":
                                                    personDayMappings[subject].totalAliceVars.fem += .1;
                                                    if (WUBI)
                                                    {
                                                        ubiTenths[time][subject].aliceVars.fem += .1;
                                                        personDayMappings[subject].WUBIAliceVars.fem += .1;
                                                    }
                                                    break;
                                                case "MAL":
                                                    personDayMappings[subject].totalAliceVars.mal += .1;
                                                    if (WUBI)
                                                    {
                                                        ubiTenths[time][subject].aliceVars.mal += .1;
                                                        personDayMappings[subject].WUBIAliceVars.mal += .1;
                                                    }
                                                    break;
                                                case "SPEECH":
                                                    personDayMappings[subject].totalAliceVars.speech += .1;
                                                    if (WUBI)
                                                    {
                                                        ubiTenths[time][subject].aliceVars.speech += .1;
                                                        personDayMappings[subject].WUBIAliceVars.speech += .1;
                                                    }
                                                    break;

                                            }
                                        }
                                            time = time.AddMilliseconds(100);
                                            blockDur = Math.Round(blockDur - 0.1, 2);
                                        } while (blockDur > 0) ;
                                    }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                
                                }
                                    /*DateTime time = lenaOnset.startTime;
                        int ms = time.Millisecond > 0 ? time.Millisecond / 100 * 100 : time.Millisecond;// + 100;
                        time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, ms);
                        DateTime timeEnd = lenaOnset.startTime.AddSeconds(lenaOnset.durSecs);
                        ms = timeEnd.Millisecond > 0 ? timeEnd.Millisecond / 100 * 100 : timeEnd.Millisecond;// + 100;
                        timeEnd = new DateTime(timeEnd.Year, timeEnd.Month, timeEnd.Day, timeEnd.Hour, timeEnd.Minute, timeEnd.Second, ms);

                        double blockDur = (timeEnd - time).Seconds + ((timeEnd - time).Milliseconds > 0 ? ((timeEnd - time).Milliseconds / 1000.00) : 0);
                        double vocDur10 = (lenaOnset.durSecs / blockDur) / 10;
                        double vocCount10 = (lenaOnset.count / blockDur) / 10;
                        double tcCount10 = (lenaOnset.tc / blockDur) / 10;


                        Boolean personExists = personDayMappings.ContainsKey(person);
                        if( personExists && blockDur>0)
                        {

                            switch (lenaOnset.type)
                            {*/
    }
}
                    }
                }
            }



        }
        public void writeTenthOfSec(String dir)
        {


            //DELETE FOR WUBI TIMES
            /* Dictionary<String, List<Tuple<DateTime, DateTime>>> sTimes = new Dictionary<string, List<Tuple<DateTime, DateTime>>>();
            String dir2 = dir.Replace(".CSV", "TIMES.CSV");
            TextWriter sw2 = new StreamWriter(dir2);
            sw2.WriteLine("BID, DateTime, From, To ");*/
            //DELETE FOR WUBI TIMES
            TextWriter sw = new StreamWriter(dir);
            sw.WriteLine("BID, DateTime, X, Y, Chaoming_Orientation, Talking, Aid, S, Type,rx,ry,lx,ly");
            TextWriter swLogs = new StreamWriter((dir.Substring(0, dir.IndexOf("SYNC")) + "LOGSBYDATEANDSUBJECT" + Utilities.szVersion + ".csv"), true);
            Dictionary<String,int> logs10 = new Dictionary<String,int>();

            foreach (DateTime t in ubiTenths.Keys)
            {
                recSecs += .1;
                foreach (String p in ubiTenths[t].Keys)
                { 
                    PersonSuperInfo psi = ubiTenths[t][p];
                    Person pi = personBaseMappings[p];
                   
                    //sw.WriteLine("BID, DateTime, X, Y, Chaoming_Orientation, Talking, Aid, S, Type,rx,ry,lx,ly");
                    sw.WriteLine(pi.mapId + "," +
                        t.ToString("hh:mm:ss.fff tt") + "," + //t.ToLongTimeString() + "," +
                        psi.x + "," +
                        psi.y + "," +
                        psi.ori_chaoming + "," +
                        psi.wasTalking + "," +
                        pi.diagnosis + "," +
                        pi.gender + "," +
                        pi.subjectType + "," +
                        psi.xr + "," +
                        psi.yr + "," +
                        psi.xl + "," +
                        psi.yl  
                        );

                    if(!logs10.ContainsKey(pi.mapId))
                    {
                        logs10.Add(pi.mapId, 1);
                    }
                    else
                        logs10[pi.mapId]++;

                   
                    //DELETE FOR WUBI TIMES
                    /*
                    DateTime from = t;
                    DateTime to = t;
                    Tuple<DateTime, DateTime> ti = new Tuple<DateTime, DateTime>(from, to);
                    if (!sTimes.ContainsKey(p)) 
                    {
                        sTimes.Add(p, new List<Tuple<DateTime, DateTime>>());
                        sTimes[p].Add(ti);
                    }
                    else
                    {
                        ti = sTimes[p][sTimes[p].Count - 1];
                        TimeSpan ts = t - ti.Item2; 
                        if(ts.TotalMilliseconds<=100)
                        {
                            ti = new Tuple<DateTime, DateTime>(ti.Item1, t);
                            sTimes[p][sTimes[p].Count - 1] = ti;
                        }
                        else
                        {
                            sTimes[p].Add(new Tuple<DateTime, DateTime>(from, to));
                        }

                    }*/
                    //DELETE FOR WUBI TIMES

                }

            }
            sw.Close();
            
            foreach (String s in logs10.Keys)
            {
                swLogs.WriteLine(s + "," + classDay.ToShortDateString() + "," + logs10[s] + ",,GOOD10THLOGS");
            }
            swLogs.WriteLine("," + classDay.ToShortDateString() + ",," + logs10.Keys.Count + ",GOOD10THSUBJECTS");
            swLogs.Close();
            //DELETE FOR WUBI TIMES
            /*foreach (String s in sTimes.Keys)
            {
                foreach (Tuple<DateTime, DateTime> ti in sTimes[s])
                {
                    //sw2.WriteLine("BID, DateTime, From To");
                    sw2.WriteLine(s + "," +
                    ti.Item1.ToShortDateString() + "," +
                    ti.Item1.ToString("hh:mm:ss.fff tt") + "," +
                    ti.Item2.ToString("hh:mm:ss.fff tt"));
                }
            }
            sw2.Close();*/
            //DELETE FOR WUBI TIMES

        }


    }
}

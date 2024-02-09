using System;
using System.Collections.Generic;

namespace UL_Processor_V2023
{

    public class Person
    {
        public String mapId = "";
        public String diagnosis = "";
        public String language = "";
        public DateTime dob = new DateTime();
        public String gender = "";
        public String longId = "";
        public String shortId = "";
        public String subjectType = "";
        public String prePLSLENA = "";
        public String prePLSDay = "";
        public String postPLSLENA = "";
        public String postPLSDay = "";


        public List<String> diagnosisList = new List<string>();
        public List<String> languagesList = new List<string>();
         
        public Person(String commaLine, String byId, List<int> dList, List<int> lList, Dictionary<String, int> columnIndexBase)
        {
            String[] line = commaLine.Split(',');
            try
            {
                String bid = line[3].Trim().ToUpper();
                String bid2 = line.Length > 18 ? line[18] : line[3].Trim().ToUpper();
                this.shortId = columnIndexBase.ContainsKey("SHORTID") && columnIndexBase["SHORTID"]>=0? line[columnIndexBase["SHORTID"]].Trim().ToUpper() : bid2.Length == 0 ? bid : bid.Length <= bid2.Length ? bid : bid2;
                this.longId = columnIndexBase.ContainsKey("LONGID") && columnIndexBase["LONGID"] >= 0 ? line[columnIndexBase["LONGID"]].Trim().ToUpper() : bid2.Length == 0 ? bid : bid.Length >= bid2.Length && bid2.Length > 0 ? bid : bid2;
                mapId = byId.ToUpper() == "SHORTID" ? shortId :  longId;
                gender = columnIndexBase.ContainsKey("SEX") && columnIndexBase["SEX"] >= 0 ? line[columnIndexBase["SEX"]].Trim().ToUpper() : line[15].Trim().ToUpper();
                dob = columnIndexBase.ContainsKey("DOB") && columnIndexBase["DOB"] >= 0 ? line[columnIndexBase["DOB"]].Trim()!=""?Convert.ToDateTime(line[columnIndexBase["DOB"]].Trim().ToUpper()):new DateTime(1985,1,1) : line[16].Trim()!=""?Convert.ToDateTime(line[16].Trim()):dob;
                subjectType = Utilities.getPersonType(columnIndexBase.ContainsKey("TYPE") && columnIndexBase["TYPE"] >= 0 ? line[columnIndexBase["TYPE"]].Trim().ToUpper() : line[17].Trim(), this.shortId);//.ToUpper(), this.shortId);
                prePLSLENA= columnIndexBase.ContainsKey("PREPLSLENA") && columnIndexBase["PREPLSLENA"] >= 0 ? line[columnIndexBase["PREPLSLENA"]].Trim().ToUpper() : "";  
                prePLSDay = columnIndexBase.ContainsKey("PREPLSDATE") && columnIndexBase["PREPLSDATE"] >= 0 ? line[columnIndexBase["PREPLSDATE"]].Trim().ToUpper() : ""; 
                postPLSLENA = columnIndexBase.ContainsKey("POSTPLSLENA") && columnIndexBase["POSTPLSLENA"] >= 0 ? line[columnIndexBase["POSTPLSLENA"]].Trim().ToUpper() : ""; 
                postPLSDay = columnIndexBase.ContainsKey("POSTPLSDATE") && columnIndexBase["POSTPLSDATE"] >= 0 ? line[columnIndexBase["POSTPLSDATE"]].Trim().ToUpper() : ""; 

                try
                {
                    foreach(int d in dList)
                    {
                        diagnosisList.Add(line[d]);
                    }
                    foreach (int l in lList)
                    {
                        languagesList.Add(line[l]);
                    }

                    //diagnosis = line[14].Trim().ToUpper();
                    //language = line[20].Trim().ToUpper();
                }
                catch(Exception e)
                {

                }
                //ADD DIANOSIS LANG EXTRA COLS


            }
            catch (Exception e)
            {
                
            }
             
        }
    }

 

}

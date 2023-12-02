using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public List<String> diagnosisList = new List<string>();
        public List<String> languagesList = new List<string>();
         
        public Person(String commaLine, String byId, List<int> dList, List<int> lList)
        {
            String[] line = commaLine.Split(',');
            try
            {
                String bid = line[3].Trim().ToUpper();
                String bid2 = line.Length > 18 ? line[18] : line[3].Trim().ToUpper();
                this.shortId = bid2.Length == 0 ? bid : bid.Length <= bid2.Length ? bid : bid2;
                this.longId = bid2.Length == 0 ? bid : bid.Length >= bid2.Length && bid2.Length > 0 ? bid : bid2;
                mapId = byId.ToUpper() == "SHORTID" ? shortId :  longId;
                gender = line[15].Trim().ToUpper();
                dob = line[16].Trim()!=""?Convert.ToDateTime(line[16].Trim()):dob;
                subjectType = Utilities.getPersonType( line[17].Trim(), this.shortId);//.ToUpper(), this.shortId);
                //
                diagnosis = line[14].Trim().ToUpper();
                language = line[20].Trim().ToUpper();

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

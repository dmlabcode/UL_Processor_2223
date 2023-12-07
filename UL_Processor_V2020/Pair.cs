using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UL_Processor_V2020;

namespace UL_Processor_V2023
{
    public class Pair
    {
        public String szPair = "";
        public String szSubjectMapId = "";
        public String szPartnerMapId = "";
        public double sharedTimeInSecs = 0;

        public int partnerDistCount = 0;
        public int subjectDistCount = 0;
        public double partnerDist = 0;
        public double subjectDist = 0;

        public double subjectTotalTimeInSecs = 0;
        public double partnerTotalTimeInSecs = 0;

        public double subjectJoinedCry;
        public double partnerJoinedCry;
        public double joinedCry;

        public double pairBlockTalking = 0;
        public double pairProxDuration = 0;
        public double pairProxOriDuration = 0;


        public double subjectJoinedCryUnstructured;
        public double partnerJoinedCryUnstructured;
        public double joinedCryUnstructured;

        public double pairBlockTalkingUnstructured = 0;
        public double pairProxDurationUnstructured = 0;
        public double pairProxOriDurationUnstructured = 0;
        public double sharedTimeInSecsUnstructured = 0;

        public LenaVars subjectLenaVarsInContact = new LenaVars();
        public LenaVars partnerLenaVarsInContact = new LenaVars();
         
        public LenaVars subjectLenaVarsInWUBI = new LenaVars();
        public LenaVars partnerLenaVarsInWUBI = new LenaVars();

        public AliceVars subjectAliceVarsInContact = new AliceVars();
        public AliceVars partnerAliceVarsInContact = new AliceVars();

        public AliceVars subjectAliceVarsInWUBI = new AliceVars();
        public AliceVars partnerAliceVarsInWUBI = new AliceVars();
        public Pair(String pair, String s, String p)
        {
            szSubjectMapId = s;
            szPartnerMapId = p;
            szPair = pair;
        }
    } 
}

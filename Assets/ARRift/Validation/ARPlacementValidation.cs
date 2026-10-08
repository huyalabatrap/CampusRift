using UnityEngine;
namespace CampusRift.AR.Validation
{
    public static class ARPlacementValidation
    {
        public static string Run()
        {
            int pass=0;
            Check(ARPlaneScoring.Score(2,.6f,1,35,1,1)>ARPlaneScoring.Score(1,.6f,1,35,1,1),"larger",ref pass);
            Check(ARPlaneScoring.Score(1,.6f,1,35,1,1)>ARPlaneScoring.Score(1,.6f,2,35,1,1),"nearer",ref pass);
            Check(ARPlaneScoring.Score(.5f,.6f,1,35,1,1)<0,"small rejected",ref pass);
            Check(ARPlaneScoring.Score(1,.6f,1,4,1,1)<0,"angle below 5 rejected",ref pass);
            var p=new[]{new Vector2(-1,-.5f),new Vector2(1,-.5f),new Vector2(1,.5f),new Vector2(-1,.5f)};float r;var c=ARPlaneScoring.Incenter(p,out r);
            Check(Mathf.Abs(ARPlaneScoring.Area(p)-2)<.001f&&r>.49f&&ARPlaneScoring.Inside(p,c),"polygon incenter",ref pass);
            Check(ARPlaneScoring.Convexity(p)>.99f,"convexity",ref pass);
            return pass+" PASS / 0 FAIL";
        }
        static void Check(bool ok,string label,ref int pass){if(!ok)throw new System.Exception("AR placement: "+label);pass++;}
    }
}

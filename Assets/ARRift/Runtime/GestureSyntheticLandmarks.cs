using UnityEngine;
namespace CampusRift.AR
{
    // Deterministic synthetic hands for Editor mocks and pure decision unit tests.
    public static class GestureSyntheticLandmarks
    {
        public static float[] Create(string label,Vector2 center,float height=.18f,float noise=0)
        {
            var p=new Vector2[21];p[0]=new Vector2(.5f,.35f);
            for(int f=0;f<4;f++)
            {int m=5+f*4;float x=.42f+f*.06f;bool extended=label=="Open_Palm"||f==0&&(label=="Pointing_Up"||label=="Victory")||f==1&&label=="Victory";p[m]=new Vector2(x,.5f);p[m+1]=new Vector2(x,.60f);p[m+2]=extended?new Vector2(x,.71f):new Vector2(x+.025f,.56f);p[m+3]=extended?new Vector2(x+(f==0?-.02f:f==1?.025f:0),.82f):new Vector2(x,.49f);}
            p[1]=new Vector2(.43f,.4f);p[2]=new Vector2(.38f,.43f);
            if(label=="Open_Palm"){p[3]=new Vector2(.3f,.48f);p[4]=new Vector2(.22f,.54f);}
            else if(label=="Thumb_Down"){p[2]=new Vector2(.38f,.4f);p[3]=new Vector2(.35f,.32f);p[4]=new Vector2(.31f,.2f);}
            else{p[3]=new Vector2(.35f,.46f);p[4]=new Vector2(.44f,.44f);}
            var data=new float[63];float scale=height/.47f;
            for(int i=0;i<21;i++){var q=center+(p[i]-new Vector2(.5f,.56f))*scale;data[i*3]=q.x+Mathf.Sin(i*12.7f)*noise;data[i*3+1]=1-q.y+Mathf.Cos(i*7.3f)*noise;}
            return data;
        }
    }
}

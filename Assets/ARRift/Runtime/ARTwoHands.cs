using System;
using UnityEngine;
namespace CampusRift.AR
{
    public struct GestureHandsFrame {public GestureFrame[] hands;public GestureFrame metadata;}
    // Model list index is never identity. Association is solved jointly for <=2 wrists.
    public sealed class ARTwoHands
    {
        public readonly GestureStateMachine[] D1={new GestureStateMachine(),new GestureStateMachine()};
        public readonly int[] Identity={0,0};public readonly string[] Pose={"None","None"};
        public readonly ARHandMotion[] Motion={new ARHandMotion(),new ARHandMotion()};
        readonly Vector2[] wrist=new Vector2[2];readonly string[] handed=new string[2];readonly double[] seen=new double[2];
        readonly GestureIntent?[] pending=new GestureIntent?[2];readonly GestureFrame[] latest=new GestureFrame[2];
        readonly bool[] active=new bool[2];readonly int[] assigned=new int[2];int serial,epoch=-1;long lastFrame=-1;
        public event Action<GestureIntent> Single;public event Action<bool,GestureIntent> Pair;
        public ARTwoHands(){for(int i=0;i<2;i++){int slot=i;D1[i].Intent+=intent=>pending[slot]=intent;}}
        public void Reset(){for(int i=0;i<2;i++){D1[i].Suspend(true);Motion[i].Cancel();active[i]=false;Identity[i]=0;pending[i]=null;latest[i]=default;wrist[i]=Vector2.zero;seen[i]=0;handed[i]=null;Pose[i]="None";}epoch=-1;lastFrame=-1;}
        float Cost(int slot,GestureFrame f,double now)
        {if(!active[slot]||now-seen[slot]>250)return .35f;var p=new Vector2(f.landmarks[0],f.landmarks[1]);float d=Vector2.Distance(wrist[slot],p);return d+(handed[slot]!=f.handed?.Trim()&& !string.IsNullOrEmpty(f.handed)?.15f:0);}
        public void Process(GestureHandsFrame batch,ARModeSettings settings,bool allowed,Camera camera)
        {
            var m=batch.metadata;if(m.epoch!=epoch){Reset();epoch=m.epoch;}if(m.frameId<=lastFrame)return;lastFrame=m.frameId;
            double now=m.consumeMs;int n=Math.Min(2,batch.hands?.Length??0);assigned[0]=assigned[1]=-1;
            if(n==2){float normal=Cost(0,batch.hands[0],now)+Cost(1,batch.hands[1],now),swap=Cost(0,batch.hands[1],now)+Cost(1,batch.hands[0],now);assigned[0]=normal<=swap?0:1;assigned[1]=1-assigned[0];
                if(Vector2.Distance(new Vector2(batch.hands[0].landmarks[0],batch.hands[0].landmarks[1]),new Vector2(batch.hands[1].landmarks[0],batch.hands[1].landmarks[1]))<.08f){for(int i=0;i<2;i++){D1[i].Suspend(true);Motion[i].Cancel();pending[i]=null;Pose[i]="None";}return;}}
            else if(n==1)assigned[Cost(0,batch.hands[0],now)<=Cost(1,batch.hands[0],now)?0:1]=0;
            for(int s=0;s<2;s++)
            {
                D1[s].Settings=settings;var f=assigned[s]>=0?batch.hands[assigned[s]]:m;
                if(assigned[s]>=0)
                {
                    float d=active[s]?Vector2.Distance(wrist[s],new Vector2(f.landmarks[0],f.landmarks[1])):0;
                    if(!active[s]||now-seen[s]>250||d>.25f)
                    {
                        // Do not undo a genuine 200ms/3-frame absence on reacquisition.
                        // A live wrist jump is ambiguous and still requires release.
                        if(!D1[s].ReleaseReady||active[s]&&now-seen[s]<=250&&d>.25f)D1[s].Suspend(true);
                        Motion[s].Cancel();pending[s]=null;Identity[s]=++serial;active[s]=true;
                    }
                    wrist[s]=new Vector2(f.landmarks[0],f.landmarks[1]);handed[s]=f.handed?.Trim();seen[s]=now;
                    f.handId=Identity[s];latest[s]=f;batch.hands[assigned[s]]=f;
                }
                else {f.landmarks=f.worldLandmarks=null;f.handPresent=false;f.label="None";f.score=0;if(now-seen[s]>250){pending[s]=null;Pose[s]="None";}}
                Motion[s].Process(f,camera,settings,allowed,false);D1[s].Process(f,allowed,Motion[s].BlocksStatic,Motion[s].CompletedThisFrame);
                Pose[s]=assigned[s]>=0&&!Motion[s].BlocksStatic&&f.score>=.6f&&D1[s].Geometry.inFrame&&!D1[s].Geometry.Contradicts(f.label)?f.label:"None";
            }
            if(!allowed){pending[0]=pending[1]=null;return;}
            bool palms=Pose[0]=="Open_Palm"&&Pose[1]=="Open_Palm";
            bool quick=Pose[0]=="Closed_Fist"&&Pose[1]=="Victory"||Pose[1]=="Closed_Fist"&&Pose[0]=="Victory";
            if((palms||quick)&&pending[0].HasValue&&pending[1].HasValue&&Math.Abs(pending[0].Value.triggerConsumeMs-pending[1].Value.triggerConsumeMs)<=180)
            {var intent=pending[0].Value;pending[0]=pending[1]=null;D1[0].RequireRelease();D1[1].RequireRelease();Pair?.Invoke(quick,intent);return;}
            Flush(now);
        }
        public void Flush(double now){for(int i=0;i<2;i++)if(pending[i].HasValue&&now-pending[i].Value.triggerConsumeMs>=180){var p=pending[i].Value;pending[i]=null;Single?.Invoke(p);}}
    }
}

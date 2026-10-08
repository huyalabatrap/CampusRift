using System;
using UnityEngine;
namespace CampusRift.AR
{
    public enum ARUltimate { SwordConvergence, IceLightningPrison, StarAbsorption }
    // D1 is the sole producer. No raw frames, extra dwell, or repeated held-pose intents here.
    public sealed class GestureSequenceMatcher
    {
        public struct Cast { public GestureIntent intent; public Vector3 aim; public bool aimValid; }
        public static readonly string[][] Seals={new[]{"Victory","Pointing_Up","Open_Palm"},new[]{"Thumb_Down","Closed_Fist","Pointing_Up"},new[]{"Open_Palm","Closed_Fist"}};
        public const float MaxGap=1.2f;
        public event Action<Cast> Single;
        public event Action<ARUltimate,Cast> Ultimate;
        public event Action Step;
        public event Action AssignedComplete;
        public int Count {get;private set;}
        public int CandidateMask {get;private set;}
        public bool Buffering=>Count>0;
        Cast first;float deadline;int epoch=-1;long lastId=-1;
        int assigned=-1;
        public void Cancel(){Count=0;CandidateMask=0;}
        public void TickAssigned(float now){if(Buffering&&now>deadline)Cancel();}
        // Same D1 deduplication and 1.2s timing, but an encounter-specified sequence and no fallback spell.
        public void SubmitAssigned(Cast cast,float now,int sequence)
        {
            var intent=cast.intent;if(intent.epoch<epoch||intent.epoch==epoch&&intent.id<=lastId)return;
            if(intent.epoch!=epoch||assigned!=sequence){Cancel();epoch=intent.epoch;assigned=sequence;}
            lastId=intent.id;TickAssigned(now);if(sequence<0||sequence>=Seals.Length)return;
            var labels=Seals[sequence];if(labels[Count]!=intent.label){Cancel();if(labels[0]!=intent.label)return;}
            CandidateMask=1<<sequence;Count++;deadline=now+MaxGap;Step?.Invoke();
            if(Count==labels.Length){Cancel();AssignedComplete?.Invoke();}
        }
        public void Tick(float now){if(Buffering&&now>deadline){var cast=first;Cancel();Single?.Invoke(cast);}}
        public void Submit(Cast cast,float now,bool ready)
        {
            var intent=cast.intent;if(intent.epoch<epoch||intent.epoch==epoch&&intent.id<=lastId)return;
            if(intent.epoch!=epoch){Cancel();epoch=intent.epoch;}lastId=intent.id;
            Tick(now);
            if(!ready){if(Buffering){var saved=first;Cancel();Single?.Invoke(saved);}Single?.Invoke(cast);return;}
            int mask=0;
            for(int i=0;i<Seals.Length;i++)if((Count==0||(CandidateMask&(1<<i))!=0)&&Count<Seals[i].Length&&Seals[i][Count]==intent.label)mask|=1<<i;
            if(mask==0)
            {
                if(Buffering){var saved=first;Cancel();Single?.Invoke(saved);SubmitFresh(cast,now);}
                else Single?.Invoke(cast);
                return;
            }
            if(Count==0)first=cast;CandidateMask=mask;Count++;deadline=now+MaxGap;Step?.Invoke();
            for(int i=0;i<Seals.Length;i++)if((mask&(1<<i))!=0&&Count==Seals[i].Length){var saved=first;Cancel();Ultimate?.Invoke((ARUltimate)i,saved);return;}
        }
        void SubmitFresh(Cast cast,float now)
        {
            int mask=0;for(int i=0;i<Seals.Length;i++)if(Seals[i][0]==cast.intent.label)mask|=1<<i;
            if(mask==0){Single?.Invoke(cast);return;}first=cast;Count=1;CandidateMask=mask;deadline=now+MaxGap;Step?.Invoke();
        }
    }
}

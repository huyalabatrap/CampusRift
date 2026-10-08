using System;
using UnityEngine;
namespace CampusRift.AR
{
    public sealed class ARBattleEvents : MonoBehaviour
    {
        ARBattlefield field;ARMonsterDirector director;ARSkillCaster caster;
        ARBattleEventKind[] schedule=Array.Empty<ARBattleEventKind>();int next;float eventAt,rescueUntil,held,lastFrame=-1;long lastStamp=-1;
        public bool RescueActive=>rescueUntil>0;
        public float RescueProgress=>Mathf.Clamp01(held/2);
        public bool BlocksWave=>RescueActive||next<schedule.Length||(GetComponent<ARPlayerCombat>()?.PendingHazards??false);
        public string Message {get;private set;}="";
        // Job3 registers meteor handling. Without Dodge + Floor + active opt-in it stays dependent.
        public event Action MeteorRequested;
        void Awake(){field=GetComponent<ARBattlefield>();director=GetComponent<ARMonsterDirector>();}
        void Start(){caster=GetComponent<ARSkillCaster>();if(caster!=null&&caster.source!=null){caster.source.Result+=Frame;caster.source.Invalidated+=ResetHold;}}
        public void BeginWave(ARWaveDefinition wave)
        {Clear();schedule=field.ModeSession.Has(ARModeFeature.Events)?wave.events:Array.Empty<ARBattleEventKind>();eventAt=field.Clock+4;}
        public void Clear(){schedule=Array.Empty<ARBattleEventKind>();next=0;rescueUntil=0;Message="";ResetHold();}
        void ResetHold(){held=0;lastFrame=-1;lastStamp=-1;}
        void Frame(GestureFrame frame)
        {
            if(!RescueActive||field.Paused||field.CheckLoad||caster==null){ResetHold();return;}
            if(frame.timestampMs<=lastStamp)return;lastStamp=frame.timestampMs;
            var geometry=GestureGeometry.Evaluate(frame,field.placement.settings);
            bool fresh=frame.acquireMs>0&&GestureRecognizerBridge.Now-frame.acquireMs<=250;
            bool good=fresh&&frame.label=="Open_Palm"&&frame.score>=.6f&&geometry.quality&&geometry.inFrame&&!geometry.Contradicts(frame.label)&&caster.AimValid&&Vector3.Distance(caster.Aim,field.Shrine.transform.position)<1.5f*field.Scale;
            float now=Time.unscaledTime;
            if(!good||lastFrame>=0&&now-lastFrame>.15f){held=0;lastFrame=good?now:-1;return;}
            if(lastFrame>=0)held+=now-lastFrame;lastFrame=now;
            if(held>=2){field.Shrine.Heal(field.Shrine.maxHealth*.25f);rescueUntil=0;Message=CampusRift.UI.LevelHUD.Vietnamese?"LINH TRẬN ĐÃ HỒI PHỤC":"SHRINE RESTORED";ResetHold();}
        }
        void Update()
        {
            if(field.Root==null||field.Shrine==null||director.Finished||field.CheckLoad){Clear();return;}
            if(field.Paused){ResetHold();return;}
            if(lastFrame>=0&&Time.unscaledTime-lastFrame>.25f)ResetHold();
            if(RescueActive&&field.Clock>=rescueUntil){rescueUntil=0;Message="";ResetHold();}
            if(next>=schedule.Length||field.Clock<eventAt)return;
            var kind=schedule[next];
            if(kind==ARBattleEventKind.PackLeader&&!director.CanSpawn)return;
            next++;eventAt=field.Clock+12;bool available=true;
            switch(kind)
            {
                case ARBattleEventKind.ShrineRescue:rescueUntil=field.Clock+12;Message=CampusRift.UI.LevelHUD.Vietnamese?"CẦU CỨU · NGẮM LINH TRẬN + GIỮ TAY MỞ 2s":"RESCUE · AIM AT SHRINE + OPEN PALM 2s";break;
                case ARBattleEventKind.PackLeader:director.SpawnActor(field.Rift.position,AREliteKind.Haste,false,true);Message=CampusRift.UI.LevelHUD.Vietnamese?"QUÁI ĐẦU ĐÀN XUẤT HIỆN":"PACK LEADER APPEARS";break;
                case ARBattleEventKind.MeteorRain:available=field.ModeSession.PlayerAttacksEnabled&&MeteorRequested!=null;if(available)MeteorRequested.Invoke();break;
            }
            director.PublishEvent(kind,available);
        }
        void OnDestroy(){if(caster!=null&&caster.source!=null){caster.source.Result-=Frame;caster.source.Invalidated-=ResetHold;}}
    }
}

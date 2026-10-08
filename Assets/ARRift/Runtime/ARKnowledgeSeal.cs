using UnityEngine;
using TMPro;
using CampusRift.Learning;
using CampusRift.Progression;
using CampusRift.Combat;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARKnowledgeSeal : MonoBehaviour
    {
        public bool Active {get;private set;}public float Remaining=>Active?Mathf.Max(0,end-field.Clock):0;
        public QuizSession Session {get;private set;}public int Target {get;private set;}=-1;
        public string Notice {get;private set;}="";public int Reward {get;private set;}
        public Transform[] Runes {get;private set;}=new Transform[0];
        public bool AnswersClipped {get{foreach(var text in texts)if(text!=null&&text.isTextOverflowing)return true;return false;}}
        ARBattlefield field;ARSkillCaster caster;float end;bool answered,policy;int wave,queuedBuff=-1;
        StatType? activeStat;TMP_Text[] texts=new TMP_Text[0];GameObject runeRoot;
        public bool EnabledForRun=>field.ModeSession.KnowledgeEnabled&&field.ModeSession.Has(ARModeFeature.Quiz)&&!caster.Practice&&!field.CheckLoad;
        void Awake(){field=GetComponent<ARBattlefield>();caster=GetComponent<ARSkillCaster>();}
        void Start(){field.Removing+=Reset;GetComponent<ARMonsterDirector>().BattleStarted+=Reset;}
        public void BeginBreak(int completedWave)
        {
            ClearRunes();ClearBuff();if(!EnabledForRun)return;
            wave=completedWave;Session=LearningService.Instance?.Engine.StartARKnowledge(field.ModeSession.Seed+wave*7919);
            if(Session==null)return;
            Active=true;answered=false;Target=-1;Reward=0;Notice="";end=field.Clock+10;policy=field.ModeSession.PersistenceAllowed;
            caster.sequences.Cancel();caster.gestures.RequireRelease();BuildRunes();
        }
        void BuildRunes()
        {
            runeRoot=new GameObject("Knowledge answer runes");runeRoot.transform.SetParent(field.Root,false);
            var options=Session.Questions[0].Options;Runes=new Transform[options.Count];texts=new TMP_Text[options.Count];
            var view=field.placement.view;var right=Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized;
            var toward=Vector3.ProjectOnPlane(view.transform.position-field.Root.position,Vector3.up).normalized;
            for(int i=0;i<options.Count;i++)
            {
                var go=new GameObject("Answer rune "+options[i].id,typeof(RectTransform),typeof(Canvas));var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;
                var rect=(RectTransform)go.transform;rect.SetParent(runeRoot.transform,false);rect.sizeDelta=new Vector2(390,190);rect.localScale=Vector3.one*(field.placement.Radius/options.Count*1.7f/390);
                rect.position=field.Root.position+toward*field.placement.Radius*.12f+right*((i-(options.Count-1)*.5f)*field.placement.Radius*.45f)+Vector3.up*field.Scale*1.4f;
                ARUI.Panel(rect,"Floating answer",0,0,390,190);texts[i]=ARUI.Text(rect,((char)('A'+i))+" · "+options[i].text,0,0,360,170,28);texts[i].enableAutoSizing=true;texts[i].fontSizeMin=18;texts[i].fontSizeMax=28;texts[i].margin=Vector4.zero;texts[i].fontStyle=FontStyles.Normal;texts[i].overflowMode=TextOverflowModes.Ellipsis;Runes[i]=rect;
            }
        }
        public bool Consume(GestureIntent intent)
        {
            if(!Active)return false;
            // All six static intents are intercepted throughout the break, including post-answer feedback.
            if(intent.label!="Closed_Fist"||answered||field.Paused)return true;
            UpdateTarget();if(Target<0){Notice=LevelHUD.Vietnamese?"Đưa tâm ngắm vào phù văn rồi nắm tay":"Aim the screen center at a rune, then close your fist";return true;}
            answered=true;var answer=Session.Answer(Session.Questions[0].Options[Target].id);
            policy&=field.ModeSession.PersistenceAllowed;
            Reward=LearningService.Instance.Engine.SubmitARKnowledge(Session,ProfileService.Ensure(),policy);
            if(answer.correct)
            {queuedBuff=new System.Random(field.ModeSession.Seed+wave*31).Next(3);Notice=(LevelHUD.Vietnamese?"ĐÚNG · buff đợt sau · +":"CORRECT · next-wave buff · +")+Reward+" LT";ARHaptics.Pulse();}
            else Notice=LevelHUD.Vietnamese?(policy?"Chưa đúng · đã thêm sổ ôn · không phạt":"Chưa đúng · không phạt · không lưu"):(policy?"Incorrect · added to notebook · no penalty":"Incorrect · no penalty · no save");
            Notice+="\n"+answer.explanation;
            return true;
        }
        void UpdateTarget()
        {
            Target=-1;if(answered)return;var ray=field.placement.view.ViewportPointToRay(new Vector3(.5f,.5f,0));float best=float.MaxValue;
            for(int i=0;i<Runes.Length;i++)
            {
                var rect=(RectTransform)Runes[i];if(rect==null)continue;
                if(!new Plane(rect.forward,rect.position).Raycast(ray,out float distance)||distance<=0)continue;
                var local=rect.InverseTransformPoint(ray.GetPoint(distance));if(rect.rect.Contains(local)&&distance<best){best=distance;Target=i;}
            }
        }
        void Update()
        {
            if(!Active)return;field.ModeSession.ObservePolicy();policy&=field.ModeSession.PersistenceAllowed;
            if(field.Root==null||GetComponent<ARMonsterDirector>().Finished){Reset();return;}
            if(field.Paused)return;
            for(int i=0;i<Runes.Length;i++){if(Runes[i]==null)continue;Runes[i].rotation=Quaternion.LookRotation(Runes[i].position-field.placement.view.transform.position);}
            UpdateTarget();for(int i=0;i<texts.Length;i++)texts[i].color=i==Target?ComicTheme.Gold:ComicTheme.Paper;
        }
        public void EndBreak()
        {
            ClearRunes();caster.gestures.RequireRelease();caster.sequences.Cancel();
            if(queuedBuff<0||caster.Caster==null)return;
            var stats=caster.Caster.GetComponent<PlayerStats>();
            activeStat=queuedBuff==0?StatType.DamageDealt:queuedBuff==1?StatType.CooldownReduction:StatType.MaxSpirit;
            stats.SetModifier(StatSource.Buff,"ar-knowledge",activeStat.Value,queuedBuff==2?30:.2f,0);
            if(queuedBuff==2)caster.Caster.GetComponent<SpiritPower>().Refill();queuedBuff=-1;
        }
        void ClearBuff(){if(activeStat.HasValue&&caster.Caster!=null)caster.Caster.GetComponent<PlayerStats>().SetModifier(StatSource.Buff,"ar-knowledge",activeStat.Value,0,0);activeStat=null;}
        void ClearRunes(){Active=false;Target=-1;if(runeRoot!=null)Destroy(runeRoot);runeRoot=null;Runes=new Transform[0];texts=new TMP_Text[0];}
        void Reset(){ClearRunes();ClearBuff();queuedBuff=-1;Session=null;}
        void OnDestroy(){if(field!=null)field.Removing-=Reset;var d=GetComponent<ARMonsterDirector>();if(d!=null)d.BattleStarted-=Reset;Reset();}
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Controls;
using CampusRift.Progression;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Levels;
using CampusRift.SkyBeast;
namespace CampusRift.Learning
{
    public enum ShrineFortune { Damage, Heal, Cooldown, Spirit, Speed, ControlGuard }
    public sealed partial class LearningShrine : CampusInteractable
    {
        public bool Used {get;private set;}
        public CampusExplorer Player {get;private set;}
        public QuizSession Session {get;private set;}
        public ShrineFortune[] Choices {get;private set;}
        public static LearningShrine Pending;
        Material stone,edge,glow;Transform rune;float activatedAt=-1;
        public override bool CanInteract=>!Used;
        public override string Label=>UI.LevelHUD.Vietnamese?"LINH BIA CƠ DUYÊN":"FORTUNE STELE";
        public bool Safe
        {
            get
            {
                if(ShelterDetector.AtFeet(transform.position)!=Shelter.Indoor)return false;
                foreach(var m in MonsterVitality.Active)if(m!=null&&!m.Defeated&&Mathf.Abs(m.transform.position.y-transform.position.y)<3&&(m.transform.position-transform.position).sqrMagnitude<64)return false;
                return true;
            }
        }
        public override void Interact(CampusExplorer player)
        {
            if(Used||!Safe||player==null||(player.transform.position-transform.position).sqrMagnitude>range*range)return;
            if(!CombatLine.Clear(player.transform.position+Vector3.up,transform.position+Vector3.up,player.transform))return;
            var engine=LearningService.Instance?.Engine;if(engine==null||engine.LearnedPool().Count==0)return;
            Player=player;Session=engine.StartShrine();if(Session==null)return;
            Pending=this;UI.LearningUIState.OpenShrine(this);
        }
        public void Resolve(bool correct)
        {
            Used=true;
            if(!correct){Choices=null;return;}
            var pool=Enum.GetValues(typeof(ShrineFortune)).Cast<ShrineFortune>().ToList();
            Choices=new ShrineFortune[3];for(int i=0;i<3;i++){int at=UnityEngine.Random.Range(0,pool.Count);Choices[i]=pool[at];pool.RemoveAt(at);}
        }
        public bool Choose(ShrineFortune fortune)
        {
            if(Player==null||Choices==null||!Choices.Contains(fortune))return false;
            var item=ScriptableObject.CreateInstance<ItemDefinition>();item.id="co-duyen-"+fortune;item.nameVN=FortuneText(fortune,true);item.nameEN=FortuneText(fortune,false);item.tint=new Color(.25f,1,.7f);item.hideFlags=HideFlags.DontSave;
            float duration=90;var effect=new ItemEffect{type=ItemEffectType.Stat,duration=duration};
            switch(fortune)
            {
                case ShrineFortune.Damage:effect.stat=StatType.DamageDealt;effect.value=.2f;break;
                case ShrineFortune.Heal:Player.GetComponent<PlayerMonsterHealth>()?.Heal(Player.GetComponent<PlayerMonsterHealth>().maxHealth*.3f);break;
                case ShrineFortune.Cooldown:effect.stat=StatType.CooldownReduction;effect.value=.2f;break;
                case ShrineFortune.Spirit:effect.stat=StatType.MaxSpirit;effect.value=30;effect.duration=float.MaxValue;break;
                case ShrineFortune.Speed:effect.stat=StatType.MoveSpeed;effect.value=.15f;break;
                case ShrineFortune.ControlGuard:effect.type=ItemEffectType.Flag;effect.flag=ItemFlag.ControlGuard;effect.value=1;effect.duration=float.MaxValue;break;
            }
            if(fortune!=ShrineFortune.Heal){item.effects.Add(effect);var buffs=Player.GetComponent<BuffSystem>()??Player.gameObject.AddComponent<BuffSystem>();buffs.Apply(item);}
            // Buff ownership stays with this shrine for this level; BeginLevel clears buffs before destroying it.
            rewardAssets.Add(item);Choices=null;activatedAt=Time.time;
            var vfx=Player.GetComponent<Skills.SkillVfxPool>();
            vfx?.Spawn(Skills.SkillVfxKind.Shockwave,transform.position,new Color(1,.65f,.08f),.6f,1.6f);
            vfx?.Spawn(Skills.SkillVfxKind.CopperMark,transform.position,new Color(.2f,1,.6f),2,1.1f);
            return true;
        }
        readonly List<ItemDefinition> rewardAssets=new List<ItemDefinition>();
        public static string FortuneText(ShrineFortune fortune,bool vn)
        {
            string[] vi={"+20% sát thương · 90 giây","Hồi 30% máu","−20% hồi chiêu · 90 giây","+30 Linh Lực tối đa · hết màn","+15% tốc chạy · 90 giây","Miễn 1 lần khống chế"};
            string[] en={"+20% damage · 90 seconds","Restore 30% health","−20% cooldown · 90 seconds","+30 max spirit · this level","+15% speed · 90 seconds","Resist one control effect"};return (vn?vi:en)[(int)fortune];
        }
        public void Cancel(){Used=true;Choices=null;if(Pending==this)Pending=null;}
        void Awake(){range=3;BuildAncientStele();}
        Material Mat(string name,Color color,bool emissive=false)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.25f);
            if(emissive){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.9f);}return m;
        }
        Transform Piece(string name,Vector3 position,Vector3 scale,Material mat,float yaw=0)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=2;go.transform.SetParent(transform,false);go.transform.localPosition=position;go.transform.localScale=scale;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);go.GetComponent<Renderer>().sharedMaterial=mat;return go.transform;
        }
        void Build()
        {
            stone=Mat("Stele weathered stone",new Color(.13f,.19f,.22f));edge=Mat("Stele ink",new Color(.012f,.018f,.025f));glow=Mat("Stele jade carving",new Color(.1f,.95f,.64f),true);
            Piece("Ink base",new Vector3(0,.1f,0),new Vector3(1.3f,.2f,.8f),edge);
            Piece("Stone base",new Vector3(0,.25f,0),new Vector3(1.1f,.2f,.7f),stone);
            Piece("Ink silhouette",new Vector3(0,1,0),new Vector3(.83f,1.4f,.44f),edge,4);
            Piece("Chipped stele",new Vector3(0,1,0),new Vector3(.75f,1.35f,.4f),stone,4);
            Piece("Cap",new Vector3(0,1.76f,0),new Vector3(.9f,.16f,.5f),edge,4);
            rune=Piece("Carved jade sigil",new Vector3(0,1.18f,-.238f),new Vector3(.26f,.26f,.025f),glow);rune.localRotation=Quaternion.Euler(0,4,45);
            Piece("Rune stroke",new Vector3(0,.82f,-.235f),new Vector3(.07f,.32f,.025f),glow,4);
            Piece("Gold inlay",new Vector3(0,.57f,-.235f),new Vector3(.38f,.035f,.025f),glow,4);
        }
        void Update()
        {
            if(glow==null)return;float intensity=Used?(activatedAt>=0?Mathf.Max(.06f,1-(Time.time-activatedAt)/2):.06f):.6f+.18f*Mathf.Sin(Time.time*2);
            glow.SetColor("_EmissionColor",new Color(.1f,.95f,.64f)*intensity);
            UpdateReadyLight();
        }
        void OnDestroy(){if(Pending==this)Pending=null;Destroy(stone);Destroy(edge);Destroy(glow);if(steleMesh!=null)Destroy(steleMesh);foreach(var a in rewardAssets)Destroy(a);}
    }
    public static class LearningShrines
    {
        static GameObject root;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){root=null;LearningShrine.Pending=null;}
        public static List<Vector3> Positions(LevelDefinition level)
        {
            var candidates=new List<Vector3>();
            foreach(var volume in UnityEngine.Object.FindObjectsByType<IndoorVolume>(FindObjectsSortMode.None))
            {
                if(volume.shelter!=Shelter.Indoor)continue;var box=volume.GetComponent<BoxCollider>();var b=box.bounds;
                var near=b.ClosestPoint(level.spawnPoint);near.y=Mathf.Max(b.min.y,level.spawnPoint.y);
                candidates.Add(near);candidates.Add(new Vector3(b.center.x,b.min.y+.1f,b.center.z));
            }
            for(int x=-24;x<=24;x+=4)for(int z=-24;z<=24;z+=4)candidates.Add(level.spawnPoint+new Vector3(x,0,z));
            var valid=new List<Vector3>();
            foreach(var c in candidates.OrderBy(p=>(p-level.spawnPoint).sqrMagnitude))
            {
                if(!NavMesh.SamplePosition(c,out var hit,2,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-level.spawnPoint.y)>2)continue;
                var p=hit.position;if(ShelterDetector.AtFeet(p)!=Shelter.Indoor||valid.Any(v=>(v-p).sqrMagnitude<100))continue;
                // Keep the capsule above the floor; touching the walkable floor must not reject every candidate.
                if(Physics.CheckCapsule(p+Vector3.up*.6f,p+Vector3.up*1.5f,.4f,ShelterDetector.EnvironmentMask,QueryTriggerInteraction.Ignore))continue;
                valid.Add(p);if(valid.Count==(level.index>=4?2:1))break;
            }
            return valid;
        }
        public static void Begin(LevelDefinition level)
        {
            if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}root=new GameObject("Linh Bia · Level "+level.index);root.transform.SetParent(LevelDirector.Instance.transform,false);
            int n=0;foreach(var p in Positions(level)){var go=new GameObject("Linh Bia "+(++n));go.transform.SetParent(root.transform);go.transform.position=p;go.AddComponent<LearningShrine>();}
        }
    }
}

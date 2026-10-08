using UnityEngine;
using CampusRift.Skills;
using CampusRift.Monsters;
using CampusRift.UI;
namespace CampusRift.Combat
{
    [DisallowMultipleComponent]
    public sealed class ReactionFeedback : MonoBehaviour
    {
        AR.ARCombatContext ar;float SessionNow=>ar!=null?ar.Now:Time.time;float Scale=>ar!=null?ar.scale:1;

        sealed class Sound { public AudioSource transient,body,tail; public float busyUntil; }
        struct Tighten { public SkillVfxPool.Node node; public float born; }
        sealed class Armor { public MonsterVitality target; public ComboGraphic graphic; }
        readonly Sound[] sounds=new Sound[8];
        readonly float[] lastVisual={-10,-10,-10,-10,-10,-10,-10,-10,-10};
        readonly Tighten[] tightens=new Tighten[16];
        readonly Armor[] armor=new Armor[32];
        Canvas indicators;
        SkillVfxPool pool;SkillImpact impact;ReactionConfig config;
        ReactionHintUI hint;
        public int VisualCount {get;private set;}
        public int AudioLayers {get;private set;}
        public ReactionHintUI Hint=>hint;
#if UNITY_EDITOR
        public void ClearForValidation(){System.Array.Clear(tightens,0,tightens.Length);foreach(var a in armor)if(a!=null){a.target=null;a.graphic.gameObject.SetActive(false);}hint?.Clear();}
#endif
        void Awake()
        {
            ar=GetComponent<AR.ARCombatContext>();
            pool=GetComponent<SkillVfxPool>();impact=GetComponent<SkillImpact>();config=ReactionConfig.Current;
            if(ar==null)hint=gameObject.AddComponent<ReactionHintUI>();
            for(int i=0;i<sounds.Length;i++)
            {
                var root=new GameObject("Reaction audio voice "+i);root.transform.SetParent(transform,false);
                sounds[i]=new Sound{transient=Source(root,.48f),body=Source(root,.38f),tail=Source(root,.26f)};
            }
            indicators=ComboUIFactory.Canvas("Armor break indicators",transform,20);
            for(int i=0;i<armor.Length;i++){var graphic=ComboUIFactory.Graphic("Armor break "+i,indicators.transform,new Vector2(34,40),Vector2.zero,ComboGraphic.Shape.Shield,ComicTheme.Gold);graphic.gameObject.SetActive(false);armor[i]=new Armor{graphic=graphic};}
        }
        AudioSource Source(GameObject root,float volume)
        {
            var source=root.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=.35f;source.volume=volume;source.minDistance=4*Scale;source.maxDistance=32*Scale;
            if(pool!=null&&pool.config!=null)source.outputAudioMixerGroup=pool.config.audioOutput;return source;
        }
        void OnEnable(){ReactionResolver.Feedback+=Triggered;}
        void OnDisable()
        {
            ReactionResolver.Feedback-=Triggered;foreach(var voice in sounds)if(voice!=null){voice.transient.Stop();voice.body.Stop();voice.tail.Stop();}
            if(armor!=null)foreach(var a in armor)if(a!=null){a.target=null;a.graphic.gameObject.SetActive(false);}hint?.Clear();
        }
        void Triggered(ReactionEvent ev)
        {
            if(ev.attacker!=gameObject||config==null||pool==null)return;
            DamageNumberPool.Instance?.ShowReaction(ev);hint?.Encounter(ev.type);
            var rule=config.Rule(ev.type);impact?.HoldVictim(ev.target,rule.hitStop);
            if(ev.type==ReactionType.ArmorShatter)TrackArmor(ev.target);
            if(ev.type==ReactionType.IceLightning){pool.DimIce(ev.point,10,.55f);GetComponent<IceSealRuntime>()?.ShatterForReaction(ev.target);}
            // One visual/SFX burst per type per group; gameplay and local hold still run for every target.
            if(SessionNow-lastVisual[(int)ev.type]<.15f)return;
            lastVisual[(int)ev.type]=SessionNow;VisualCount++;impact?.Pulse(rule.impulse,rule.hitStop);PlaySound(rule,ev.point);
            Vector3 p=ev.point+Vector3.up*Scale;Color cyan=new Color(.35f,.9f,1),gold=new Color(1,.8f,.15f),purple=new Color(.7f,.25f,1),fire=new Color(1,.35f,.1f);
            int pieces=SkillVfxPool.MobileQuality?4:8;
            switch(ev.type)
            {
                case ReactionType.IceLightning:
                    p=ev.point+Vector3.up*(1.6f*Scale);
                    ReactionFlash(p,cyan,1.65f);
                    var ice=GetComponent<IceSealRuntime>();if(ice==null||!ice.IsCasting)pool.Fragments(p,cyan,pieces,.45f,SkillVfxKind.Shard,true);
                    pool.Spawn(SkillVfxKind.Shockwave,ev.point,cyan,.7f,rule.radius);
                    for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;var bolt=pool.Priority(pool.Spawn(SkillVfxKind.ReactionBolt,p,gold,.38f,1.1f));if(bolt!=null)bolt.End=p+new Vector3(Mathf.Cos(a)*rule.radius,.7f,Mathf.Sin(a)*rule.radius)*Scale;}
                    pool.Spawn(SkillVfxKind.FrostMist,ev.point,cyan,.85f,2);break;
                case ReactionType.ElectricFlow:
                    ReactionFlash(p,gold,1.3f);
                    int arcs=0;
                    for(int i=0;i<MonsterVitality.Active.Count&&arcs<4;i++)
                    {
                        var m=MonsterVitality.Active[i];if(m==null||m.Defeated||(m.transform.position-ev.point).sqrMagnitude>rule.radius*rule.radius)continue;
                        var bolt=pool.Priority(pool.Spawn(SkillVfxKind.ReactionBolt,p,gold,.45f,.8f));if(bolt!=null)bolt.End=m.transform.position+Vector3.up;
                        var body=pool.Priority(pool.Spawn(SkillVfxKind.Bolt,m.transform.position+Vector3.up*.45f,purple,.5f,.9f));if(body!=null){body.Follow=m.transform;body.Offset=Vector3.up*.45f;body.End=m.transform.position+Vector3.up*1.7f;}
                        impact?.HoldVictim(m,rule.hitStop);arcs++;
                    }break;
                case ReactionType.FireExplosion:
                    ReactionFlash(p,fire,1.8f);pool.Priority(pool.Spawn(SkillVfxKind.FireBloom,ev.point+Vector3.up*.8f,fire,.6f,2));
                    pool.Spawn(SkillVfxKind.Shockwave,ev.point,fire,.7f,rule.radius);
                    pool.Spawn(SkillVfxKind.FireField,ev.point,fire,1,rule.radius);pool.Spawn(SkillVfxKind.Smoke,ev.point,fire,1.3f,1.5f);break;
                case ReactionType.Convergence:
                    ReactionFlash(p,new Color(1,.1f,.35f),1.8f);
                    for(int i=0;i<3;i++){var ring=pool.Priority(pool.Spawn(SkillVfxKind.Ring,ev.point+Vector3.up*((.15f+i*.55f)*Scale),purple,.55f,3-i*.35f));TrackTighten(ring);}
                    pool.Spawn(SkillVfxKind.Repulsion,ev.point,purple,.6f,3);break;
                case ReactionType.ArmorShatter:
                    ReactionFlash(p,gold,1.65f);pool.Fragments(p,gold,pieces,.45f,SkillVfxKind.BellShard,true);
                    pool.Fragments(p,new Color(.65f,.72f,.8f),pieces/2,.4f,SkillVfxKind.Shard,true);pool.Spawn(SkillVfxKind.Shockwave,ev.point,gold,.6f,2);break;
                case ReactionType.Wildfire:
                    ReactionFlash(p,fire,1.3f);pool.Spawn(SkillVfxKind.FireBloom,p,fire,.65f,1.2f);
                    int spread=0;foreach(var m in MonsterVitality.Active)if(m!=null&&m!=ev.target&&!m.Defeated&&(m.transform.position-ev.point).sqrMagnitude<16&&spread++<3){var wind=pool.Spawn(SkillVfxKind.Bolt,p,new Color(.15f,1,.35f),.7f,1);if(wind!=null){wind.End=m.transform.position+Vector3.up;wind.Progress=-1;}pool.Spawn(SkillVfxKind.FireBloom,m.transform.position+Vector3.up*.7f,fire,.6f,.7f);}break;
                case ReactionType.SwordSoul:
                    pool.Spawn(SkillVfxKind.Sword,p,gold,.9f,.8f);pool.Spawn(SkillVfxKind.Sword,p+Vector3.right*.3f,purple,.9f,.6f);pool.Spawn(SkillVfxKind.Ring,ev.point,gold,.8f,1.2f);ReactionFlash(p,purple,.9f);break;
                case ReactionType.GuardDrain:
                    pool.Spawn(SkillVfxKind.Ring,ev.point+Vector3.up*.3f,gold,.8f,1.8f);pool.Spawn(SkillVfxKind.Ring,ev.point+Vector3.up*.8f,cyan,.8f,1.4f);pool.Spawn(SkillVfxKind.SwordDust,p,cyan,.8f,1);ReactionFlash(p,cyan,.8f);break;
                case ReactionType.DomainResonance:
                    pool.Spawn(SkillVfxKind.CopperMark,ev.point,purple,1.1f,2);pool.Spawn(SkillVfxKind.Shockwave,ev.point,purple,.8f,3);pool.Spawn(SkillVfxKind.Ring,ev.point,Color.white,.65f,2);ReactionFlash(p,purple,.9f);break;
            }
            pool.Spawn(SkillVfxKind.Scorch,ev.point,ev.type==ReactionType.FireExplosion?fire:ev.type==ReactionType.ArmorShatter?gold:ev.type==ReactionType.Convergence?purple:cyan,2.3f,ev.type==ReactionType.FireExplosion?4:2.5f);
        }
        void ReactionFlash(Vector3 p,Color color,float size)
        {var node=pool.Priority(pool.Spawn(SkillVfxKind.Burst,p,color,.6f,size));if(node!=null)pool.Emit(node,SkillVfxPool.MobileQuality?10:28);}
        void TrackTighten(SkillVfxPool.Node node){if(node==null)return;for(int i=0;i<tightens.Length;i++)if(tightens[i].node==null||!tightens[i].node.Live){tightens[i]=new Tighten{node=node,born=SessionNow};return;}}
        void TrackArmor(MonsterVitality target)
        {
            foreach(var a in armor)if(a.target==target)return;
            foreach(var a in armor)if(a.target==null||a.target.Defeated||!a.target.isActiveAndEnabled||!a.target.GetComponent<StatusEffectHost>().Has(StatusType.ArmorBreak)){a.target=target;a.graphic.gameObject.SetActive(true);return;}
        }
        void PlaySound(ReactionRule rule,Vector3 point)
        {
            Sound voice=sounds[0];foreach(var s in sounds)if(s.busyUntil<SessionNow){voice=s;break;}
            voice.transient.Stop();voice.body.Stop();voice.tail.Stop();voice.transient.transform.position=point;
            voice.transient.clip=rule.transient;voice.body.clip=rule.body;voice.tail.clip=rule.tail;
            voice.transient.pitch=1.05f;voice.body.pitch=rule.type==ReactionType.Convergence?.65f:.9f;voice.tail.pitch=.8f;
            double start=AudioSettings.dspTime+.005;voice.transient.PlayScheduled(start);voice.body.PlayScheduled(start+.035);voice.tail.PlayScheduled(start+.11);
            voice.busyUntil=SessionNow+1.1f;AudioLayers+=3;
        }
        void LateUpdate()
        {
            if(ar!=null&&ar.Paused)return;
            for(int i=0;i<tightens.Length;i++){var t=tightens[i];if(t.node==null)continue;if(!t.node.Live||SessionNow-t.born>=.55f){tightens[i]=default(Tighten);continue;}t.node.Radius=Mathf.Lerp(3,.35f,Mathf.Clamp01((SessionNow-t.born)/.4f))*Scale;}
            bool gameplay=ar!=null||UIStateManager.Instance!=null&&(UIStateManager.Instance.State==UIState.Gameplay||UIStateManager.Instance.State==UIState.Modal);indicators.enabled=gameplay;
            var camera=Camera.main;if(camera==null)return;
            foreach(var a in armor)
            {
                if(a.target==null)continue;
                if(a.target.Defeated||!a.target.isActiveAndEnabled||!a.target.GetComponent<StatusEffectHost>().Has(StatusType.ArmorBreak)){a.target=null;a.graphic.gameObject.SetActive(false);continue;}
                Vector3 p=camera.WorldToScreenPoint(a.target.transform.position+Vector3.up*(2.8f*Scale));a.graphic.gameObject.SetActive(p.z>0);
                a.graphic.rectTransform.anchoredPosition=new Vector2(p.x/Screen.width*1920-960,p.y/Screen.height*1080-540);
            }
        }
    }
}

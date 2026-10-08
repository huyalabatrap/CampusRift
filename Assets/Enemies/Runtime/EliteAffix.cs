using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Levels;
namespace CampusRift.Enemies
{
    public enum EliteAffixKind { Berserk, MetalBody, Split, Vampiric, Explosion, Invisible, Guardian, FireHeart }
    [DisallowMultipleComponent]
    public sealed class EliteAffix : MonoBehaviour, IArmorShatterable
    {
        public bool IsElite {get;private set;}
        public bool MetalBroken {get;private set;}
        public readonly List<EliteAffixDefinition> Affixes=new List<EliteAffixDefinition>(2);
        public int ChildrenSpawned {get;private set;}
        public float SpeedMultiplier=>Has(EliteAffixKind.Berserk)?1.3f:1;
        public float AttackIntervalMultiplier=>Has(EliteAffixKind.Berserk)?1/1.3f:1;
        EnemyInstance owner;Vector3 originalScale;PlayerMonsterHealth player;EnemyTelegraph aura;float nextAura;
        readonly List<SkinnedMeshRenderer> outlines=new List<SkinnedMeshRenderer>();
        void Awake(){owner=GetComponent<EnemyInstance>();originalScale=transform.localScale;owner.Died+=Died;}
        public bool Has(EliteAffixKind kind)=>IsElite&&Affixes.Exists(a=>a!=null&&a.kind==kind);
        public void ResetLife(){StopAllCoroutines();if(player!=null)player.DamageReceived-=DamagePlayer;player=null;IsElite=false;MetalBroken=false;ChildrenSpawned=0;Affixes.Clear();transform.localScale=originalScale;aura?.Hide();aura=null;foreach(var r in outlines)if(r!=null)r.forceRenderingOff=true;EnemyHealthBars.Instance?.SetName(owner.Vitality,null);}
        public void Configure(int level,int seed,params EliteAffixKind[] forced)
        {
            ResetLife();IsElite=true;
            var available=Resources.LoadAll<EliteAffixDefinition>("P19/Affixes").Where(a=>level>=a.minLevel).OrderBy(a=>(int)a.kind).ToList();
            var random=new System.Random(seed);int count=level>=8?2:1;
            if(forced!=null&&forced.Length>0){foreach(var k in forced){var a=available.Find(x=>x.kind==k);if(a!=null&&!Affixes.Contains(a))Affixes.Add(a);}}
            else while(Affixes.Count<count&&available.Count>0){int n=random.Next(available.Count);Affixes.Add(available[n]);available.RemoveAt(n);}
            transform.localScale=originalScale*1.3f;owner.Vitality.SetMaxHealth(owner.MaxHealth,true);
            owner.GetComponent<SkyBeast.FireEnemyState>().fireHeart=Has(EliteAffixKind.FireHeart);
            if(Has(EliteAffixKind.FireHeart)){var flames=GetComponent<EliteFireHeartVisual>()??gameObject.AddComponent<EliteFireHeartVisual>();flames.Configure();}
            if(Has(EliteAffixKind.Invisible)&&GetComponent<EnemyConcealment>()==null)gameObject.AddComponent<EnemyConcealment>();
            string name=owner.archetype.LocalizedName(UI.LevelHUD.Vietnamese)+" · "+string.Join(" / ",Affixes.Select(a=>UI.LevelHUD.Vietnamese?a.nameVN:a.nameEN));
            EnemyHealthBars.Instance?.SetName(owner.Vitality,name);
            player=EnemyDirector.Ensure().Player;if(player!=null)player.DamageReceived+=DamagePlayer;
            nextAura=0;
            BuildOutline();
        }
        void BuildOutline()
        {
            var material=Resources.Load<Material>("P19/EliteOutline");if(material==null)return;
            var group=GetComponent<LODGroup>();if(group!=null&&outlines.Count==0)
            {
                var lods=group.GetLODs();for(int i=0;i<lods.Length;i++){var list=lods[i].renderers.ToList();foreach(var src in lods[i].renderers.OfType<SkinnedMeshRenderer>())
                    {var go=new GameObject("Elite outline");go.transform.SetParent(src.transform,false);go.layer=7;var r=go.AddComponent<SkinnedMeshRenderer>();r.sharedMesh=src.sharedMesh;r.bones=src.bones;r.rootBone=src.rootBone;r.localBounds=src.localBounds;r.sharedMaterials=Enumerable.Repeat(material,src.sharedMaterials.Length).ToArray();outlines.Add(r);list.Add(r);}lods[i].renderers=list.ToArray();}group.SetLODs(lods);
            }
            var block=new MaterialPropertyBlock();block.SetColor("_OutlineColor",Has(EliteAffixKind.FireHeart)?new Color(.85f,.29f,.035f,1):Affixes.Count>0?Affixes[0].color*2:Color.yellow);block.SetFloat("_Width",Has(EliteAffixKind.FireHeart)?.012f:.025f);foreach(var r in outlines){r.forceRenderingOff=false;r.SetPropertyBlock(block);}
        }
        public void BreakMetalBody(){MetalBroken=true;}
        void DamagePlayer(DamageInfo info)
        {if(owner.Alive&&Has(EliteAffixKind.Vampiric)&&info.attacker==gameObject)owner.Vitality.Heal(info.amount*.2f);}
        public static float Reduce(MonsterVitality victim,float amount)
        {
            var affix=victim.GetComponent<EliteAffix>();
            if(affix!=null&&affix.Has(EliteAffixKind.MetalBody)&&!affix.MetalBroken)amount*=.6f;
            if(EnemyDirector.Instance!=null)foreach(var e in EnemyDirector.Instance.Active)
                if(e!=null&&e.Alive&&e.Vitality!=victim&&e.Elite!=null&&e.Elite.Has(EliteAffixKind.Guardian)&&
                    EnemyAbilityRunner.InRange(e.transform.position,victim.transform.position,6)&&CombatLine.Clear(e.transform.position+Vector3.up,victim.transform.position+Vector3.up,e.transform))
                {amount*=.7f;break;}
            return victim.GetComponent<EnemyWard>()?.Absorb(amount)??amount;
        }
        void Update()
        {
            bool off=!IsElite||!owner.Alive||(GetComponent<EnemyConcealment>()?.Hidden??false);foreach(var r in outlines)if(r!=null)r.forceRenderingOff=off;
            if(!IsElite||!owner.Alive||Time.time<nextAura)return;
            nextAura=Time.time+.8f;
            aura?.Hide();aura=EnemyTelegraph.Show(transform.position,transform.forward,Has(EliteAffixKind.FireHeart)?GetComponent<EliteFireHeartVisual>().FootRadius:.8f,.8f,color:Has(EliteAffixKind.FireHeart)?new Color(1,.35f,.035f,.45f):Affixes.Count>0?Affixes[0].color*1.7f:Color.yellow);
        }
        void Died(EnemyInstance enemy)
        {
            if(!IsElite)return;aura?.Hide();
            if(Has(EliteAffixKind.Split))
            {
                for(int i=0;i<2;i++)
                {
                    var child=EnemyPool.Ensure().Spawn(owner.archetype,transform.position+transform.right*(i==0?-1.2f:1.2f),owner.scaling,false);
                    if(child==null)continue;child.transform.localScale*=.55f;child.Vitality.SetMaxHealth(owner.MaxHealth*.3f,true);ChildrenSpawned++;LevelDirector.Instance?.TrackSummoned(child);
                }
            }
            if(Has(EliteAffixKind.Explosion))StartCoroutine(Explode());
        }
        IEnumerator Explode()
        {
            string hold="p19-elite-explosion-"+GetEntityId();LevelDirector.Instance?.HoldWin(hold);
            var warning=EnemyTelegraph.Show(transform.position,transform.forward,4,.6f);
            yield return new WaitForSeconds(.6f);warning?.Hide();P19EnemyFeedback.HitPlayer(owner,transform.position,4,owner.Damage*1.5f,"elite-explosion",DamageSource.Reaction);
            EnemyAbilityRunner.Feedback(transform.position,4);LevelDirector.Instance?.ReleaseWin(hold);
        }
        void OnDisable(){aura?.Hide();if(player!=null)player.DamageReceived-=DamagePlayer;player=null;}
        void OnDestroy(){if(owner!=null)owner.Died-=Died;}
    }
}

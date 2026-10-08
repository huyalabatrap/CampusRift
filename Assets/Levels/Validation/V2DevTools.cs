#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.Progression;
using CampusRift.Skills;
using CampusRift.SkyBeast;
using CampusRift.UI;
namespace CampusRift.Levels
{
    // All DEV mutations use a disposable profile, including wallet/unlocks.
    public static class V2DevTools
    {
        public static void Disposable()
        {
            if(!ProfileService.Instance.Transient)
                ProfileService.Instance.UseTransient(JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(ProfileService.Instance.Data)));
        }
        public static void StartLevel(int index)
        {
            Disposable();var level=LevelCatalog.Instance.Get(Mathf.Clamp(index,1,10));
            ProfileService.Instance.Cultivation.SetState((Realm)level.requiredRealm,level.requiredTier,0);
            LevelSession.Select(index);LevelSession.Loadout=LoadoutUI.SuggestedIds(index);
            UIStateManager.Instance.EnterScene(true);var director=LevelDirector.Ensure();director.enabled=true;director.Begin(level);
        }
        public static void KillCurrentWave()
        {
            if(LevelDirector.Instance==null)return;
            foreach(var e in LevelDirector.Instance.Alive.ToArray())if(e!=null&&e.Alive)
            {var hit=DamageInfo.Create(1000000,Element.None,DamageSource.Skill,e.transform.position,Vector3.down,LevelDirector.Instance.PlayerTransform?.gameObject);hit.skillId="v2-dev";e.Vitality.ApplyDamage(hit);}
        }
        public static void FillIntent()
        {
            // Kill through the real feed, including queued/pending enemies. Never invent intent points.
            if(LevelDirector.Instance==null||LevelDirector.Instance.Level.index<8||
                (LevelDirector.Instance.State!=LevelDirector.Phase.Intro&&LevelDirector.Instance.State!=LevelDirector.Phase.Wave))return;
            var runner=LevelDirector.Instance.GetComponent<V2WaveClear>()??LevelDirector.Instance.gameObject.AddComponent<V2WaveClear>();runner.enabled=true;
        }
        public static void ToggleGod()
        {
            var player=Object.FindAnyObjectByType<PlayerMonsterHealth>();if(player==null)return;
            var qa=player.GetComponent<V2QaPlayer>()??player.gameObject.AddComponent<V2QaPlayer>();qa.God=!qa.God;
        }
        public static void ToggleDamage()
        {
            var stats=Object.FindAnyObjectByType<PlayerStats>();if(stats==null)return;
            var qa=stats.GetComponent<V2QaPlayer>()??stats.gameObject.AddComponent<V2QaPlayer>();qa.Lethal=!qa.Lethal;
            stats.SetModifier(StatSource.Buff,"v2-qa",StatType.Attack,qa.Lethal?100000:0,0);
        }
        public static void Heal(){var hp=Object.FindAnyObjectByType<PlayerMonsterHealth>();hp?.Revive(1,0);Object.FindAnyObjectByType<SpiritPower>()?.Refill();}
        public static void End()
        {
            RestLoadoutUI.Instance?.Close();LevelDirector.Instance?.End();
            var qa=Object.FindAnyObjectByType<V2QaPlayer>();if(qa!=null)Object.Destroy(qa);
            ProfileService.Instance.EndTransient();UIStateManager.Instance.EnterScene(true);
        }
    }
    public sealed class V2WaveClear:MonoBehaviour
    {
        void Update(){var d=LevelDirector.Instance;if(d!=null&&d.State==LevelDirector.Phase.Intro)return;V2DevTools.KillCurrentWave();if(d==null||d.AwaitingSkySword||d.State!=LevelDirector.Phase.Wave)enabled=false;}
    }
    public sealed class V2QaPlayer:MonoBehaviour
    {
        public bool God,Lethal;PlayerMonsterHealth hp;
        void Awake(){hp=GetComponent<PlayerMonsterHealth>();hp.BeforeDefeat+=Save;}
        bool Save(DamageInfo hit){if(!God)return false;hp.Revive(1,0);return true;}
        void Update(){if(God)hp.Heal(hp.maxHealth);}
        void OnDestroy(){if(hp!=null)hp.BeforeDefeat-=Save;GetComponent<PlayerStats>()?.SetModifier(StatSource.Buff,"v2-qa",StatType.Attack,0,0);}
    }
}
#endif

using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Enemies
{
    [DefaultExecutionOrder(240), DisallowMultipleComponent]
    public sealed class EnemyConcealment : MonoBehaviour, IEnemyConcealment
    {
        public bool Hidden { get; private set; }
        EnemyInstance owner; Renderer[] bodies; float revealedUntil; Vector3 prior; float idleSince;
        void Awake(){owner=GetComponent<EnemyInstance>();bodies=GetComponentsInChildren<Renderer>(true);owner.Vitality.Damaged+=Hit;}
        void Hit(DamageInfo hit){RevealFor(3);}
        public void ResetLife(){revealedUntil=0;idleSince=Time.time;prior=transform.position;SetHidden(false);}
        public void RevealFor(float seconds){revealedUntil=Mathf.Max(revealedUntil,Time.time+seconds);SetHidden(false);}
        void LateUpdate()
        {
            if(owner==null||!owner.Alive){SetHidden(false);return;}
            if((transform.position-prior).sqrMagnitude>.0001f)idleSince=Time.time;
            prior=transform.position;
            var player=EnemyDirector.Instance?.FindPlayer();
            bool shadow=owner.archetype.id=="anh-yeu"&&owner.scaling.level>=8&&player!=null&&Vector3.Distance(player.position,transform.position)>5;
            bool idle=owner.Elite!=null&&owner.Elite.Has(EliteAffixKind.Invisible)&&Time.time-idleSince>.65f;
            SetHidden(Time.time>=revealedUntil&&(shadow||idle));
        }
        void SetHidden(bool value){Hidden=value;if(bodies!=null)foreach(var r in bodies)if(r!=null)r.forceRenderingOff=value;}
        void OnDisable(){SetHidden(false);}
        void OnDestroy(){if(owner!=null&&owner.Vitality!=null)owner.Vitality.Damaged-=Hit;}
    }
}

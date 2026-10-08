using System.Collections;
using System.Reflection;
using UnityEngine;
using CampusRift.Monsters;
using CampusRift.Combat;
namespace CampusRift.Enemies
{
    // Keeps the original hunter stack intact; only this instance's configuration is scaled.
    [DefaultExecutionOrder(40)]
    public sealed class ShabanEnemyBridge : MonoBehaviour
    {
        EnemyInstance owner; MonsterAIConfig original, runtime; bool reported;Vector3 modelScale;
        public float EncounterAt {get;private set;}=-1;
        public MonsterAIConfig RuntimeConfig=>runtime;
        void Awake(){owner=GetComponent<EnemyInstance>();original=GetComponent<MonsterBrain>().config;modelScale=GetComponentInChildren<Animator>().transform.localScale;owner.Vitality.DefeatedOnce+=Died;}
        void OnDestroy(){if(owner!=null)owner.Vitality.DefeatedOnce-=Died;if(runtime!=null)Destroy(runtime);}
        public void Configure()
        {
            Cancel();reported=false;EncounterAt=-1;
            if(runtime!=null)Destroy(runtime);runtime=Instantiate(original);runtime.name=original.name+" (P12 runtime)";
            runtime.Damage=owner.Damage;runtime.enableTimeEscalation=false;
            runtime.BeliefParticles=Controls.CampusInput.Mobile?160:320;
            runtime.ChaseSpeed=original.ChaseSpeed*owner.scaling.speed;
            foreach(var component in GetComponents<MonoBehaviour>()){
                var field=component.GetType().GetField("config",BindingFlags.Instance|BindingFlags.Public);
                if(field!=null && field.FieldType==typeof(MonsterAIConfig))field.SetValue(component,runtime);
            }
            GetComponent<MonsterMemory>()?.Forget();GetComponent<MonsterBelief>()?.Deactivate();
            foreach(var c in GetComponentsInChildren<Collider>())c.enabled=true;
            var animator=GetComponentInChildren<Animator>();if(animator!=null){animator.transform.localScale=modelScale;animator.Rebind();animator.speed=1;animator.Update(0);}
            var nav=GetComponent<MonsterNavigation>();if(nav.roomGraph==null)nav.roomGraph=FindAnyObjectByType<RoomGraph>();
            GetComponent<MonsterBrain>().enabled=true;GetComponent<MonsterCombat>().enabled=true;
            GetComponent<BossController>()?.ResetLife();
        }
        void Update()
        {
            if(EncounterAt>=0||!owner.Alive)return;
            var p=EnemyDirector.Ensure().FindPlayer();var perception=GetComponent<MonsterPerception>();
            if(p!=null && (perception.CanSeePlayer||Vector3.Distance(p.position,transform.position)<12))EncounterAt=Time.time;
        }
        void Died()
        {
            if(reported)return;reported=true;GetComponent<BossController>()?.Cancel();
            GetComponent<MonsterBrain>().enabled=false;GetComponent<MonsterCombat>().Interrupt();GetComponent<MonsterNavigation>().Stop();
            foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
            EnemyDirector.Instance?.Unregister(owner,true);owner.RaiseDied();
            StartCoroutine(ReturnAfterDeath());
        }
        IEnumerator ReturnAfterDeath(){float t=0;var model=GetComponentInChildren<Animator>().transform;var scale=model.localScale;while(t<1){t+=Time.deltaTime;model.localScale=scale*Mathf.Max(.03f,1-t);yield return null;}model.localScale=scale;owner.Pool?.Release(owner);}
        public void Cancel(){StopAllCoroutines();GetComponent<BossController>()?.Cancel();GetComponent<MonsterCombat>()?.Interrupt();GetComponent<MonsterNavigation>()?.Stop();}
    }
}

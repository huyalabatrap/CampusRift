using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Combat;
using CampusRift.Monsters;
namespace CampusRift.Levels
{
    // One warning and one hit per strike; only the trial's gameplay clock advances it.
    public sealed class EndgameHazard:MonoBehaviour
    {
        LevelDefinition level;float next,impact;Vector3 point;EnemyTelegraph warning;
        public static bool Silenced=>LevelDirector.Instance!=null&&LevelDirector.Instance.Level!=null&&LevelDirector.Instance.Level.runMode==EndgameMode.Tower&&LevelDirector.Instance.Level.towerRule==TowerRule.Silence;
        public void Begin(LevelDefinition d){Clear();level=d;next=Time.time+8;}
        void Update()
        {
            var director=LevelDirector.Instance;
            if(level==null||level.runMode!=EndgameMode.Tower||level.towerRule!=TowerRule.Lightning||director==null)return;
            if(director.State!=LevelDirector.Phase.Wave){Clear();return;}
            if(director.CinematicPaused||(UI.UIStateManager.Instance!=null&&UI.UIStateManager.Instance.State!=UI.UIState.Gameplay))return;
            if(warning!=null&&Time.time>=impact)
            {
                warning.Hide();warning=null;
                var hp=director.PlayerTransform?.GetComponent<PlayerMonsterHealth>();
                if(hp!=null&&Vector3.ProjectOnPlane(hp.transform.position-point,Vector3.up).sqrMagnitude<=9)
                {var hit=DamageInfo.Create(25*level.damageMultiplier,Element.Loi,DamageSource.Environment,point,Vector3.down);hit.skillId="tower-lightning";hit.isArea=true;hp.ApplyDamage(hit);}
                EnemyAbilityRunner.Feedback(point,3);
            }
            if(Time.time>=next&&director.PlayerTransform!=null)
            {var offset=Random.insideUnitCircle*5;point=director.PlayerTransform.position+new Vector3(offset.x,0,offset.y);warning=EnemyTelegraph.Show(point,Vector3.forward,3,1.2f,color:new Color(.3f,.6f,1));impact=Time.time+1.2f;next=Time.time+8;}
        }
        void Clear(){warning?.Hide();warning=null;}
        void OnDisable(){Clear();}void OnDestroy(){Clear();}
    }
}

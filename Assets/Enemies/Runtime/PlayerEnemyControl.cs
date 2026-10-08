using UnityEngine;
namespace CampusRift.Enemies
{
    public sealed class PlayerEnemyControl : MonoBehaviour
    {
        float stunUntil,chillUntil,slow,holdUntil,priorSpeed=1;CampusExplorer explorer;Animator animator;
        public bool Stunned=>Time.time<stunUntil;
        public float Multiplier=>Stunned?0:Time.time<chillUntil?1-slow:1;
        public static PlayerEnemyControl Ensure(GameObject player){var c=player.GetComponent<PlayerEnemyControl>();return c!=null?c:player.AddComponent<PlayerEnemyControl>();}
        void Awake(){explorer=GetComponent<CampusExplorer>();animator=GetComponentInChildren<Animator>();}
        public void ImpactHold(float seconds){if(animator==null)return;if(Time.time>=holdUntil)priorSpeed=animator.speed;holdUntil=Mathf.Max(holdUntil,Time.time+seconds);}
        public void Clear(){stunUntil=chillUntil=slow=holdUntil=0;if(explorer!=null)explorer.EnemyMoveMultiplier=1;if(animator!=null&&animator.speed==0)animator.speed=priorSpeed;}
        bool Immune=> (GetComponent<Skills.MartialAvatarRuntime>()?.ControlImmune??false)||(GetComponent<Progression.BuffSystem>()?.ControlImmune??false);
        bool BlockControl()=>Immune||(GetComponent<Progression.BuffSystem>()?.ConsumeControlGuard()??false);
        public void Stun(float seconds){if(!BlockControl())stunUntil=Mathf.Max(stunUntil,Time.time+seconds);}
        public void Chill(float seconds,float fraction){if(!BlockControl()){bool active=Time.time<chillUntil;chillUntil=Mathf.Max(chillUntil,Time.time+seconds);slow=active?Mathf.Max(slow,fraction):fraction;}}
        void Update(){if(explorer!=null)explorer.EnemyMoveMultiplier=Multiplier;if(animator!=null){if(Time.time<holdUntil)animator.speed=0;else if(animator.speed==0)animator.speed=priorSpeed;}}
        void OnDisable(){if(explorer!=null)explorer.EnemyMoveMultiplier=1;if(animator!=null&&animator.speed==0)animator.speed=priorSpeed;stunUntil=chillUntil=slow=holdUntil=0;}
    }
}

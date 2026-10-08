using UnityEngine;
namespace CampusRift.Enemies
{
    public sealed class EnemyWard:MonoBehaviour
    {
        public float Remaining {get;private set;}float expires;EnemyTelegraph ring;
        public void Grant(float amount,float seconds=12){Remaining=amount;expires=Time.time+seconds;}
        public float Absorb(float amount){if(Time.time>=expires)Remaining=0;float take=Mathf.Min(amount,Remaining);Remaining-=take;return amount-take;}
        void Update(){if(Remaining<=0||Time.time>=expires){Remaining=0;ring?.Hide();return;}if(ring==null||!ring.Live)ring=EnemyTelegraph.Show(transform.position,transform.forward,.65f,.5f,color:new Color(.3f,.9f,2));}
        public void Clear(){Remaining=0;ring?.Hide();ring=null;}
        void OnDisable(){Clear();}
    }
}

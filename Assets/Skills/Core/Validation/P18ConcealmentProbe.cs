#if UNITY_EDITOR
using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    // Contract fixture for the upcoming P19 concealed enemy; no gameplay prefab is changed.
    public sealed class P18ConcealmentProbe:MonoBehaviour,IEnemyConcealment
    {
        public int Calls;public float Until;
        public void RevealFor(float seconds){Calls++;Until=Time.time+seconds;}
    }
}
#endif

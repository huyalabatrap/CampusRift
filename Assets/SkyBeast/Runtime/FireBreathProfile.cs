using UnityEngine;
namespace CampusRift.SkyBeast
{
    [CreateAssetMenu(menuName="Campus Rift/Fire Breath Profile")]
    public sealed class FireBreathProfile:ScriptableObject
    {
        public int level=8,phase=1;
        public float cycleSeconds=45,warningSeconds=6,breathSeconds=4,afterfireSeconds=10;
        public float outdoorDamage=280,partialDamage=126,indoorDamage=34,recommendedHealth=500;
        public float Total(Shelter s)=>s==Shelter.Indoor?indoorDamage:s==Shelter.Partial?partialDamage:outdoorDamage;
        public static FireBreathProfile Load(int level,int phase=1)=>Resources.Load<FireBreathProfile>("P13/Fire"+Mathf.Clamp(level,8,10)+"Phase"+Mathf.Clamp(phase,1,level==10?3:level==9?2:1));
    }
}

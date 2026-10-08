using UnityEngine;
namespace CampusRift.SkyBeast
{
    [DisallowMultipleComponent]
    public sealed class FireEnemyState:MonoBehaviour
    {
        public bool fireHeart;
        float boostUntil;
        public float SpeedMultiplier=>Time.time<boostUntil?1.2f:1;
        public bool Immune
        {
            get
            {
                var v=GetComponent<Monsters.MonsterVitality>();var e=GetComponent<Enemies.EnemyInstance>();
                return fireHeart||(v!=null&&v.Element==Combat.Element.Hoa)||(e!=null&&e.archetype!=null&&e.archetype.id=="bao-thi");
            }
        }
        public void AfterBreath(){if(Immune)boostUntil=Time.time+10;}
        public void ResetLife(){boostUntil=0;fireHeart=false;}
        void OnDisable(){boostUntil=0;}
    }
}

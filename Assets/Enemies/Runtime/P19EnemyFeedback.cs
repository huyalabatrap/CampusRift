using UnityEngine;
using CampusRift.Combat;
using CampusRift.Skills;
namespace CampusRift.Enemies
{
    public static class P19EnemyFeedback
    {
        public static SkillVfxPool Vfx=>EnemyDirector.Instance?.Player?.GetComponent<SkillVfxPool>();
        public static void Smoke(Vector3 point,Color color,float size=1.3f)
        {var v=Vfx;if(v==null)return;var node=v.Spawn(SkillVfxKind.Smoke,point+Vector3.up*.3f,color,.9f,size,v.config.voidCast,smokeTint:color);if(node==null)return;node.particles.Play();for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var p=new ParticleSystem.EmitParams{position=point+new Vector3(Mathf.Cos(a)*size*.35f,.2f,Mathf.Sin(a)*size*.35f),velocity=Vector3.up*.6f,startSize=size*.65f,startLifetime=.7f,startColor=new Color(color.r,color.g,color.b,.85f)};node.particles.Emit(p,1);}}
        public static bool HitPlayer(EnemyInstance source,Vector3 point,float radius,float damage,string id,DamageSource type=DamageSource.Skill,Element element=Element.None)
        {
            var player=EnemyDirector.Instance?.Player;
            if(player==null||!EnemyAbilityRunner.InRange(player.transform.position,point,radius)||!CombatLine.Clear(point+Vector3.up,player.transform.position+Vector3.up,source.transform,true))return false;
            var info=DamageInfo.Create(damage,element==Element.None?source.archetype.element:element,type,player.transform.position+Vector3.up,(player.transform.position-point).normalized,source.gameObject);info.skillId=id;return player.ApplyDamage(info);
        }
    }
}

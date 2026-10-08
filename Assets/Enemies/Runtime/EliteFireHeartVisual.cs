using System.Linq;
using UnityEngine;
using CampusRift.SkyBeast;
namespace CampusRift.Enemies
{
    // Cosmetic only. A small fixed particle budget and local simulation keep the flames
    // close to the silhouette instead of leaving a world-space orange cloud behind it.
    public sealed class EliteFireHeartVisual:MonoBehaviour
    {
        ParticleSystem flame;EliteAffix affix;EnemyInstance owner;
        SkinnedMeshRenderer[] skins;
        public float FootRadius{get;private set;}
        public ParticleSystem Flame=>flame;
        public void Configure()
        {
            affix=GetComponent<EliteAffix>();owner=GetComponent<EnemyInstance>();
            if(flame==null)
            {
                flame=FireVisualFactory.Particles(transform,"Bounded FireHeart flames","flame",false,0,.12f,.5f,new Color(1,.43f,.08f,.28f));
                var main=flame.main;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.maxParticles=24;main.startSpeed=0;main.gravityModifier=0;main.startSize=new ParticleSystem.MinMaxCurve(.06f,.12f);
                var velocity=flame.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;velocity.y=.28f;
                var growth=flame.sizeOverLifetime;growth.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.25f),new Keyframe(.4f,1),new Keyframe(1,.1f)));
                var renderer=flame.GetComponent<ParticleSystemRenderer>();renderer.maxParticleSize=.025f;renderer.minParticleSize=0;
            }
            flame.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            skins=GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.name!="Elite outline").ToArray();
            FitToBody();
            FootRadius=Mathf.Clamp(GetComponent<CapsuleCollider>().radius*transform.lossyScale.x*1.2f,.35f,.9f);
            var emission=flame.emission;emission.rateOverTime=FireVisualQuality.Choose(18,12,8);flame.Play();
        }
        void FitToBody()
        {
            var bounds=skins[0].bounds;foreach(var r in skins)bounds.Encapsulate(r.bounds);
            var scale=transform.lossyScale;var shape=flame.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.rotation=Vector3.zero;
            shape.scale=new Vector3(Mathf.Min(.6f,bounds.size.x*.70f)/scale.x,bounds.size.y*.65f/scale.y,Mathf.Min(.45f,bounds.size.z*.70f)/scale.z);
            flame.transform.localPosition=transform.InverseTransformPoint(bounds.center);
        }
        void LateUpdate()
        {
            if(flame==null)return;
            bool show=affix!=null&&affix.Has(EliteAffixKind.FireHeart)&&owner.Alive&&!(GetComponent<EnemyConcealment>()?.Hidden??false);
            if(show)FitToBody();
            if(show&&!flame.isPlaying)flame.Play();else if(!show&&flame.isPlaying)flame.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void OnDisable(){if(flame!=null)flame.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    }
}

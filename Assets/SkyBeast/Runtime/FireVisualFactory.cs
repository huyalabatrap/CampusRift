using UnityEngine;
namespace CampusRift.SkyBeast
{
    public static class FireVisualFactory
    {
        public static ParticleSystem RollingFire(Transform parent,string name,float size,float life)
        {
            var p=Particles(parent,name,"flame",false,0,size,life,Color.white);
            p.GetComponent<ParticleSystemRenderer>().sharedMaterial=Resources.Load<Material>("P13/rolling-fire");
            var main=p.main;main.startRotation=new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);
            main.maxParticles=FireVisualQuality.Choose(96,64,48);
            var rotation=p.rotationOverLifetime;rotation.enabled=true;rotation.z=new ParticleSystem.MinMaxCurve(-1.8f,1.8f);
            return p;
        }
        public static ParticleSystem Particles(Transform parent,string name,string texture,bool glow,float rate,float size,float life,Color color,float radius=0)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            var p=go.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            float peak=Mathf.Max(1,Mathf.Max(color.r,Mathf.Max(color.g,color.b)));
            var main=p.main;main.playOnAwake=false;main.loop=true;main.startLifetime=life;main.startSpeed=glow?2.5f:1;main.startSize=new ParticleSystem.MinMaxCurve(size*.65f,size);main.startColor=new Color(color.r/peak,color.g/peak,color.b/peak,color.a);main.maxParticles=256;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=glow?-.1f:-.05f;
            var emission=p.emission;emission.rateOverTime=rate;
            var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=15;shape.radius=radius;shape.rotation=new Vector3(-90,0,0);
            var fade=p.colorOverLifetime;fade.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(.75f,.6f),new GradientAlphaKey(0,1)});fade.color=g;
            var growth=p.sizeOverLifetime;growth.enabled=true;growth.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.3f),new Keyframe(.25f,1),new Keyframe(1,glow?.1f:1.7f)));
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Resources.Load<Material>("P13/"+texture+"-"+(glow?"glow":"alpha"));renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var tint=new MaterialPropertyBlock();tint.SetColor("_Color",new Color(peak,peak,peak,1));renderer.SetPropertyBlock(tint);
            return p;
        }
        public static LineRenderer Line(Transform parent,string name,float width,Color color,bool glow)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=2;
            line.sharedMaterial=Resources.Load<Material>("P13/line-"+(glow?"glow":"alpha"));line.widthMultiplier=width;line.startColor=line.endColor=Color.white;
            // Vertex colors are packed into bytes; preserve HDR fire tint in a float uniform.
            var tint=new MaterialPropertyBlock();tint.SetColor("_Color",color);line.SetPropertyBlock(tint);
            line.widthCurve=new AnimationCurve(new Keyframe(0,.9f),new Keyframe(.18f,1),new Keyframe(1,0));line.numCapVertices=3;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
            line.widthMultiplier=width;
            return line;
        }
    }
}

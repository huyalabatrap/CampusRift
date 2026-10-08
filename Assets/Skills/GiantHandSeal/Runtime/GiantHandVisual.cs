using UnityEngine;
using UnityEngine.Rendering;

namespace CampusRift.Skills
{
    public sealed class GiantHandVisual : MonoBehaviour
    {
        GiantHandConfig config;
        MeshRenderer marker,portal,halo,hand,inlay,shock,echo,scar,aura,flashDisk;
        Light impactLight;
        ParticleSystem sparks,dust;
        AudioSource voice,chargeVoice;
        MaterialPropertyBlock block;
        HandSealTarget target;
        Quaternion facing;
        bool descending,ending;
        LineRenderer contour;
        readonly Vector3[] contourPoints=new Vector3[65];
        readonly GiantHandTargeting ground=new GiantHandTargeting();
        Transform owner;
        float nextContour;AR.ARCombatContext ar;float Scale=>ar!=null?ar.scale:1;float SessionNow=>ar!=null?ar.Now:Time.time;
        public int LiveParticles => sparks.particleCount+dust.particleCount;
        public bool Visible => hand.enabled || portal.enabled || marker.enabled || scar.enabled;
        static readonly Color Gold=new Color(1,0.63f,0.16f),Violet=new Color(0.63f,0.2f,1),Cyan=new Color(0.1f,0.85f,1);

        public void Initialize(GiantHandConfig c, Transform caster=null)
        {
            config=c;owner=caster;ar=caster!=null?caster.GetComponent<AR.ARCombatContext>():null;block=new MaterialPropertyBlock();
            if(ar!=null)gameObject.AddComponent<AR.ARVisualBounds>().context=ar;
            marker=Mesh("Target seal",c.planeMesh,c.sigilMaterial);portal=Mesh("Rift aperture",c.planeMesh,c.sigilMaterial);
            halo=Mesh("Counter-rotating crown",c.planeMesh,c.sigilMaterial);
            hand=Mesh("Celestial palm",c.handMesh,c.handMaterial);inlay=Mesh("Palm inscriptions",c.inlayMesh,c.handMaterial);
            shock=Mesh("Impact wave",c.planeMesh,c.sigilMaterial);echo=Mesh("Aftershock",c.planeMesh,c.sigilMaterial);
            scar=Mesh("Residual seal",c.planeMesh,c.sigilMaterial);aura=Mesh("Caster invocation",c.planeMesh,c.sigilMaterial);
            flashDisk=Mesh("Impact flash",c.planeMesh,c.sigilMaterial);
            var glow=new GameObject("Impact light");glow.transform.SetParent(transform,false);glow.transform.localPosition=Vector3.up*(.6f*Scale);
            impactLight=glow.AddComponent<Light>();impactLight.type=LightType.Point;impactLight.color=Gold;impactLight.range=c.radius*2;impactLight.shadows=LightShadows.None;impactLight.intensity=0;
            var outline=new GameObject("Surface contour");outline.transform.SetParent(transform,false);contour=outline.AddComponent<LineRenderer>();
            contour.sharedMaterial=c.particleMaterial;contour.positionCount=65;contour.widthMultiplier=0.028f*Scale;
            contour.useWorldSpace=true;contour.shadowCastingMode=ShadowCastingMode.Off;contour.receiveShadows=false;
            sparks=Particles("Rift fragments",false);dust=Particles("Impact motes",true);
            voice=gameObject.AddComponent<AudioSource>();chargeVoice=gameObject.AddComponent<AudioSource>();
            foreach(var source in new[]{voice,chargeVoice}){source.playOnAwake=false;source.outputAudioMixerGroup=c.output;source.spatialBlend=0.65f;source.minDistance=5*Scale;source.maxDistance=35*Scale;source.rolloffMode=AudioRolloffMode.Logarithmic;}
            Skills.SkillAudio.Apply(voice,chargeVoice);
            Finish();
        }
        MeshRenderer Mesh(string label,Mesh mesh,Material material)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return r;
        }
        ParticleSystem Particles(string label,bool mist)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.playOnAwake=false;main.maxParticles=mist?64:100;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(0.2f,mist?0.9f:0.65f);
            main.startSize=new ParticleSystem.MinMaxCurve((mist?0.08f:0.045f)*Scale,(mist?0.32f:0.2f)*Scale);main.startSpeed=0;
            main.gravityModifier=(mist?0.1f:0.65f)*Scale;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(0.5f,0.3f,1),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});color.color=gradient;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=config.particleMaterial;
            renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=config.shardMesh;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return ps;
        }
        void Sigil(MeshRenderer renderer,Vector3 point,float radius,float opacity,float mode,Color color,float rotation=0)
        {
            renderer.enabled=opacity>0.005f;renderer.transform.SetPositionAndRotation(point,Quaternion.Euler(0,rotation,0));
            renderer.transform.localScale=Vector3.one*radius;
            block.Clear();block.SetColor("_Tint",color);block.SetFloat("_Opacity",opacity);block.SetFloat("_Mode",mode);block.SetFloat("_Progress",SessionNow);renderer.SetPropertyBlock(block);
        }
        public void Preview(HandSealTarget p)
        {
            Sigil(marker,p.point,config.radius,0.65f,0,p.valid?Gold:new Color(1,0.15f,0.18f));
            if(owner!=null && SessionNow>=nextContour)
            {
                nextContour=SessionNow+0.08f;
                for(int i=0;i<65;i++)
                {
                    float a=i*Mathf.PI*2/64;Vector3 sample=p.point+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*config.radius;
                    if(ground.Cast(sample+Vector3.up*1.25f,Vector3.down,2.5f,owner,out var hit))sample.y=hit.point.y+0.045f;
                    contourPoints[i]=sample;
                }
                contour.SetPositions(contourPoints);
            }
            contour.enabled=owner!=null;contour.startColor=contour.endColor=p.valid?Gold:new Color(1,0.15f,0.18f);
        }
        public void HidePreview(){marker.enabled=false;contour.enabled=false;}
        public void Begin(HandSealTarget p,Quaternion rotation)
        {
            target=p;facing=rotation;descending=false;ending=false;transform.position=p.point;
            sparks.Clear();dust.Clear();sparks.Play();dust.Play();if(owner!=null){var ar=owner.GetComponent<CampusRift.AR.ARCombatContext>();if(ar!=null)ar.VfxStarted();}
            Play(config.cast,0.5f,1);voice.PlayOneShot(config.rift,0.5f);
            chargeVoice.clip=config.charge;chargeVoice.loop=true;chargeVoice.volume=0.16f;chargeVoice.pitch=0.75f;chargeVoice.Play();
            Burst(sparks,p.point+Vector3.up*p.height,32,1.2f,true);
        }
        public void Animate(float elapsed,Vector3 caster)
        {
            float summon=Mathf.Clamp01(elapsed/config.summonTime);
            float fall=Mathf.Clamp01((elapsed-config.summonTime)/config.descentTime);
            float aftermath=elapsed-config.summonTime-config.descentTime;
            float fade=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((aftermath-0.2f)/(config.aftermathTime-0.2f)));
            float opening=Mathf.SmoothStep(0,1,Mathf.Clamp01(summon*2.2f));
            float scale=target.visualScale;
            Sigil(marker,target.point,config.radius,fade*0.6f,0,Gold,0);
            Sigil(aura,caster+Vector3.up*(0.08f*Scale),0.8f*Scale,(1-summon)*0.8f,0,Violet,elapsed*120);
            Sigil(portal,target.point+Vector3.up*(target.height+0.04f*Scale),2.15f*scale*opening,fade,1,Violet,elapsed*65);
            Sigil(halo,target.point+Vector3.up*(target.height-0.08f*Scale),2.3f*scale*opening,fade*0.7f,0,Cyan,-elapsed*90);
            if(target.openSky)
            {portal.transform.rotation*=Quaternion.Euler(18,0,0);halo.transform.rotation*=Quaternion.Euler(-12,0,0);}
            float handFade=Mathf.Clamp01((aftermath-0.16f)/0.58f);
            hand.enabled=inlay.enabled=elapsed>0.08f && handFade<0.995f;
            float height=Mathf.Lerp(target.height-0.3f*Scale,0.18f*Scale,fall*fall*fall);
            float tilt=(1-fall)*Mathf.Min(12,target.height*2);
            hand.transform.SetPositionAndRotation(target.point+Vector3.up*height,facing*Quaternion.Euler(tilt,0,0));
            hand.transform.localScale=Vector3.one*scale;
            inlay.transform.SetPositionAndRotation(hand.transform.position,hand.transform.rotation);inlay.transform.localScale=hand.transform.localScale;
            block.Clear();block.SetFloat("_Reveal",Mathf.Clamp01((summon-0.1f)*1.6f));block.SetFloat("_Dissolve",handFade);block.SetColor("_Tint",Gold);hand.SetPropertyBlock(block);
            block.SetColor("_Tint",Cyan*1.3f);inlay.SetPropertyBlock(block);
            chargeVoice.pitch=0.75f+summon*0.7f;
            if(!descending && fall>0){descending=true;chargeVoice.Stop();Play(config.descent,0.7f,0.7f);}
            if(aftermath>=0)
            {
                Sigil(shock,target.point+Vector3.up*(0.06f*Scale),config.radius*Mathf.Lerp(0.12f,1.12f,Mathf.Clamp01(aftermath/0.35f)),Mathf.Clamp01(1-aftermath/0.38f),3,Gold);
                float second=aftermath-0.09f;
                Sigil(echo,target.point+Vector3.up*(0.11f*Scale),config.radius*Mathf.Lerp(0.2f,1.18f,Mathf.Clamp01(second/0.45f)),second<0?0:Mathf.Clamp01(1-second/0.5f)*0.75f,3,Cyan);
                bool reduced=UI.SettingsManager.Instance?.Current.ReduceSkillFlashes??false;
                Sigil(flashDisk,target.point+Vector3.up*(.26f*Scale),config.radius,Mathf.Clamp01(1-aftermath/.18f)*(reduced?.12f:1),4,Gold);
                impactLight.intensity=(reduced?.4f:6)*Mathf.Pow(Mathf.Clamp01(1-aftermath/.22f),2);
                impactLight.enabled=aftermath<.22f;
                Sigil(scar,target.point+Vector3.up*(0.015f*Scale),config.radius*0.9f,fade*0.75f,2,Gold);
                if(!ending && aftermath>0.42f){ending=true;Play(config.dissipate,0.22f,0.85f);}
            }
        }
        public void Impact()
        {
            Play(config.impact,0.85f,0.72f);voice.PlayOneShot(config.aftershock,0.4f);
            Burst(sparks,target.point+Vector3.up*(0.15f*Scale),78,6,false);Burst(dust,target.point+Vector3.up*(0.12f*Scale),48,3.5f,false);
        }
        void Burst(ParticleSystem ps,Vector3 center,int count,float speed,bool inward)
        {
            if(ar!=null)count=Mathf.CeilToInt(count*.5f);
            for(int i=0;i<count;i++)
            {
                float angle=i*2.39996f;Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var e=new ParticleSystem.EmitParams();
                e.position=center+radial*(inward?1.6f*target.visualScale:Random.Range(0.1f,1.4f)*Scale);
                e.velocity=(radial*(inward?-1:1)+Vector3.up*(inward?0.2f:Random.Range(0.1f,0.9f)))*speed*Random.Range(0.5f,1)*Scale;
                e.startColor=i%3==0?Cyan:i%4==0?Violet:Gold;e.startLifetime=Random.Range(0.25f,0.8f);ps.Emit(e,1);
            }
        }
        void Play(AudioClip clip,float volume,float pitch){if(clip==null)return;voice.pitch=pitch;voice.PlayOneShot(clip,volume);}
        public void Unavailable(){Play(config.unavailable,0.15f,0.8f);}
        public void Finish()
        {
            marker.enabled=portal.enabled=halo.enabled=hand.enabled=inlay.enabled=shock.enabled=echo.enabled=scar.enabled=aura.enabled=flashDisk.enabled=false;
            impactLight.intensity=0;impactLight.enabled=false;
            contour.enabled=false;sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            chargeVoice.Stop();voice.Stop();
        }
    }
}

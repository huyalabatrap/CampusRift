from pathlib import Path
def edit(path,old,new,count=1):
    p=Path(path);s=p.read_text(encoding='utf-8-sig');assert s.count(old)==count,(path,old[:60],s.count(old));p.write_text(s.replace(old,new),encoding='utf-8')
f='Assets/Skills/Core/Runtime/SkillVfxPool.cs'
edit(f,'SwordDust, ReactionBolt }','SwordDust, ReactionBolt, CopperMark, SwordImpact }')
edit(f,'annulus,bellFragment;','annulus,bellFragment,impactStar;')
edit(f,'public Vector3 Offset;','public Vector3 Offset;\n            public float Opacity=1;internal float dimUntil;')
edit(f,'bellFragment=SkillVfxMeshes.BellFragment();','bellFragment=SkillVfxMeshes.BellFragment();impactStar=SkillVfxMeshes.ImpactStar();')
edit(f,'n.Progress=1;n.Crack=0;','n.Progress=1;n.Crack=0;n.Opacity=1;n.dimUntil=0;')
edit(f,'kind==SkillVfxKind.Scorch?disc:','kind==SkillVfxKind.Scorch||kind==SkillVfxKind.CopperMark?disc:')
edit(f,'if(kind==SkillVfxKind.Accretion)mesh=annulus;','if(kind==SkillVfxKind.Accretion)mesh=annulus;\n                if(kind==SkillVfxKind.SwordImpact)mesh=impactStar;')
edit(f,'if(kind==SkillVfxKind.Accretion)n.surface.sharedMaterial=config.accretion;','if(kind==SkillVfxKind.Accretion)n.surface.sharedMaterial=config.accretion;\n                if(kind==SkillVfxKind.CopperMark)n.surface.sharedMaterial=config.copperMark;\n                if(kind==SkillVfxKind.SwordImpact)n.surface.sharedMaterial=config.impactStar;')
edit(f,'if(kind==SkillVfxKind.Accretion)n.surface.enabled=true;','if(kind==SkillVfxKind.Accretion||kind==SkillVfxKind.CopperMark||kind==SkillVfxKind.SwordImpact)n.surface.enabled=true;\n                if(kind==SkillVfxKind.FireBloom)n.surface.enabled=n.ink.enabled=false;\n                n.surface.sortingOrder=0;n.ink.sortingOrder=0;')
edit(f,'if(kind==SkillVfxKind.ChainBolt)n.line.sharedMaterial=config.layeredLightning;','if(kind==SkillVfxKind.ChainBolt)n.line.sharedMaterial=config.layeredLightning;\n                if(kind==SkillVfxKind.Slash)n.line.sharedMaterial=config.slashStroke;')
edit(f,'n.line.widthCurve=n.core.widthCurve=n.detail.widthCurve=','n.line.textureMode=LineTextureMode.Stretch;\n                n.line.widthCurve=n.core.widthCurve=n.detail.widthCurve=')
edit(f,'var particleRenderer=n.particles.GetComponent<ParticleSystemRenderer>();','var particleRenderer=n.particles.GetComponent<ParticleSystemRenderer>();particleRenderer.sortingOrder=0;')
edit(f,'float fade=Mathf.Clamp01((n.duration-n.age)/.5f),f=n.age/n.duration;','float fade=Mathf.Clamp01((n.duration-n.age)/.5f)*n.Opacity,f=n.age/n.duration;\n                if(n.kind==SkillVfxKind.Ice&&n.dimUntil>Time.time)fade*=.16f;')
edit(f,'if(n.kind==SkillVfxKind.Scorch){','if(n.kind==SkillVfxKind.Scorch||n.kind==SkillVfxKind.CopperMark){')
edit(f,'n.Color.maxColorComponent>.5f?Dark(n.Color):n.Color','n.kind==SkillVfxKind.CopperMark?n.Color:n.Color.maxColorComponent>.5f?Dark(n.Color):n.Color')
edit(f,'case SkillVfxKind.FireBloom:n.model.localScale=Vector3.one*n.Radius*Mathf.SmoothStep(.2f,1,Mathf.Clamp01(n.age/.12f));break;','case SkillVfxKind.FireBloom:FireBillows(n,fade);break;')
edit(f,'case SkillVfxKind.Portal:Orbit(n,n.Radius,fade,12);break;','case SkillVfxKind.Portal:Orbit(n,n.Radius,fade,12);n.line.startColor=n.line.endColor=n.Color*n.Progress;n.core.startColor=n.core.endColor=n.Color*n.Progress;n.line.widthMultiplier*=Mathf.Sqrt(n.Progress);break;')
edit(f,'case SkillVfxKind.Scorch:\n','case SkillVfxKind.CopperMark:\n                        n.model.localScale=Vector3.one*n.Radius;n.model.localPosition=Vector3.up*.025f;break;\n                    case SkillVfxKind.Scorch:\n')
edit(f,'case SkillVfxKind.Burst:\n','case SkillVfxKind.SwordImpact:\n                        n.surface.enabled=n.age<.09f;n.model.localRotation=viewCamera!=null?viewCamera.transform.rotation:Quaternion.identity;n.model.localScale=Vector3.one*n.Radius*(.6f+Mathf.Clamp01(n.age/.06f)*.8f);\n                        Circle(n,n.Position-Vector3.up*.17f,Mathf.Clamp01(n.age/.24f)*1.15f,fade*.85f);n.line.sharedMaterial=config.layeredWide;n.line.widthMultiplier=.14f;break;\n                    case SkillVfxKind.Burst:\n')
edit(f,'        void Stream(Node n,float rate)', '''        // Reaction passes draw after the triggering skill; reset all render state on reuse.
        public Node Priority(Node n,bool debris=false)
        {
            if(n==null)return null;n.surface.sortingOrder=n.line.sortingOrder=n.core.sortingOrder=20;
            if(n.kind==SkillVfxKind.ReactionBolt||n.kind==SkillVfxKind.ChainBolt||n.kind==SkillVfxKind.Bolt)n.line.sharedMaterial=config.reactionStroke;
            else if(n.surface.enabled)n.surface.sharedMaterial=debris?config.reactionIce:config.reactionSurface;
            n.particles.GetComponent<ParticleSystemRenderer>().sortingOrder=20;return n;
        }
        public void DimIce(Vector3 center,float radius,float seconds)
        {for(int i=0;i<nodes.Length;i++){var n=nodes[i];if(n.Live&&n.kind==SkillVfxKind.Ice&&(n.Position-center).sqrMagnitude<radius*radius)n.dimUntil=Mathf.Max(n.dimUntil,Time.time+seconds);}}
        void FireBillows(Node n,float fade)
        {
            // Rising lobes, narrow stem then spreading crown: never a shield/sphere mesh.
            n.surface.enabled=n.ink.enabled=false;n.emissionTimer+=Time.deltaTime*(MobileQuality?24:52);int count=(int)n.emissionTimer;n.emissionTimer-=count;
            if(count>0&&!n.particles.isPlaying)n.particles.Play();
            for(int i=0;i<count&&ParticleCount<(MobileQuality?400:1500);i++)
            {
                float a=Random.value*Mathf.PI*2,spread=n.Radius*Mathf.Lerp(.18f,.65f,Mathf.Clamp01(n.age/.35f));
                var e=new ParticleSystem.EmitParams();e.position=n.Position+new Vector3(Mathf.Cos(a)*spread,n.age*n.Radius*2,Mathf.Sin(a)*spread);
                e.velocity=new Vector3(Mathf.Cos(a)*.7f,2.8f,Mathf.Sin(a)*.7f);e.startSize=n.Radius*Random.Range(.55f,.9f);e.startLifetime=.45f;e.startColor=new Color(1,1,1,fade*.85f);n.particles.Emit(e,1);
            }
        }
        void Stream(Node n,float rate)''')
edit(f,'coreWidth*1.3f','coreWidth*1.65f')
edit(f,'            Draw(n.line,9,n.Color,fade,width*1.85f);\n            for(int i=0;i<9;i++)slashBuffer[i]=Vector3.Lerp(n.Position,end,i/8f);n.line.SetPositions(slashBuffer);','''            Draw(n.line,9,n.Color,fade,width*1.85f);
            Vector3 side=Vector3.Cross((end-n.Position).normalized,Vector3.up);
            n.line.widthCurve=Crescent;
            for(int i=0;i<9;i++){float t=i/8f;slashBuffer[i]=Vector3.Lerp(n.Position,end,t)+side*Mathf.Sin(t*Mathf.PI)*.8f;}n.line.SetPositions(slashBuffer);
            // Short electric teeth along each edge, drawn by the two already warmed lines.
            for(int edge=0;edge<2;edge++){
                var line=edge==0?n.core:n.detail;line.sharedMaterial=config.layeredSmall;Draw(line,12,new Color(.7f,.15f,1),fade,.12f);
                for(int i=0;i<12;i++){float t=(i/2+1)/7f;var p=Vector3.Lerp(n.Position,end,t)+side*(Mathf.Sin(t*Mathf.PI)*.8f+(edge==0?-1:1)*width*.65f);runes12[i]=p+(i%2==0?Vector3.zero:Vector3.up*(.18f+.35f*Mathf.Sin(i*2.6f+n.age*15)));}line.SetPositions(runes12);
            }''')
edit(f,'static readonly AnimationCurve Uniform=', 'static readonly AnimationCurve Crescent=new AnimationCurve(new Keyframe(0,0),new Keyframe(.15f,.4f),new Keyframe(.5f,1),new Keyframe(.85f,.4f),new Keyframe(1,0));\n        static readonly AnimationCurve Uniform=')
edit(f,'Destroy(bellFragment);','Destroy(bellFragment);Destroy(impactStar);')

# Wider soft gold, distinct purple border; white filament remains thin.
f='Assets/Skills/Core/P10LayeredStroke.shader'
edit(f,'exp(-d*d*16)','exp(-d*d*8)')
edit(f,'half3(.11,.006,.2)+half3(.42,.015,.7)*purple','half3(.045,.003,.11)+half3(.28,.009,.65)*purple')
edit(f,'gold*.8,purple*.24','gold*.95,purple*.65')

# Petal UVs follow each petal, never a world-space crack grid.
f='Assets/Skills/Core/Runtime/SkillVfxMeshes.cs'
edit(f,'var colors=new List<Color>();\n            for (int layer','var colors=new List<Color>();var uv=new List<Vector2>();\n            for (int layer')
edit(f,'colors.Add(Color.black);colors.Add(Color.white);colors.Add(Color.black);','colors.Add(new Color(.18f,.05f,.01f));colors.Add(Color.white);colors.Add(new Color(.18f,.05f,.01f));uv.Add(new Vector2(0,f));uv.Add(new Vector2(.5f,f));uv.Add(new Vector2(1,f));')
edit(f,'mesh.SetColors(colors);return mesh;','mesh.SetColors(colors);mesh.SetUVs(0,uv);return mesh;')
f='Assets/Skills/Core/P10Surface.shader'
edit(f,'half4 c:COLOR;};','half4 c:COLOR;float2 uv:TEXCOORD0;};')
edit(f,'half4 c:TEXCOORD3;};','half4 c:TEXCOORD3;float2 uv:TEXCOORD4;};')
edit(f,'v.c=a.c;return v;','v.c=a.c;v.uv=a.uv;return v;')
old='if(_LotusGlow>0){color=lerp(half3(.16,.015,.006),color,step(.18,v.c.r));color=lerp(color,half3(3.5,2.4,.4),saturate((.4-length(v.o.xz))*2)*_Progress);}'
new='if(_LotusGlow>0){float edge=smoothstep(0,.12,min(v.uv.x,1-v.uv.x));float vein=1-smoothstep(.009,.023,abs(v.uv.x-.5));color=lerp(half3(.13,.008,.003),half3(1.6,.3,.014)+half3(2.5,1.9,.2)*pow(1-v.uv.y,3)*_Progress,edge);color+=vein*half3(.55,.19,.008)*sin(v.uv.y*3.14);}'
edit(f,old,new)
f='Assets/Skills/FireLotus/Runtime/FireLotusRuntime.cs'
edit(f,'Accent,.5f,2.6f);if(bloom!=null)vfx.Emit(bloom,SkillVfxPool.MobileQuality?8:16);','Accent,.6f,2.5f);')
edit(f,'DangerZoneRegistry.Remove(danger);danger=DangerZoneRegistry.Register(point,7,4);','vfx.Spawn(SkillVfxKind.Shockwave,point,Accent,.65f,7);\n            DangerZoneRegistry.Remove(danger);danger=DangerZoneRegistry.Register(point,7,4);')

# Darken textured bodies briefly rather than replacing their appearance with opaque black.
f='Assets/Skills/GiantHandSeal/Runtime/MonsterVitality.cs'
edit(f,'Paint(new Color(.025f,.01f,.04f),new Color(.04f,.015f,.06f))','Paint(new Color(.32f,.25f,.42f),new Color(.32f,.25f,.42f))')
print('Pool, flame, slash and lightning visual changes applied')

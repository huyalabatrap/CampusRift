from pathlib import Path
def edit(path,pairs):
    f=Path(path);s=f.read_text(encoding='utf-8-sig')
    for old,new in pairs:
        assert old in s,(path,old)
        s=s.replace(old,new)
    f.write_text(s,encoding='utf-8')
core='Assets/Skills/Core/Runtime/'
edit(core+'SkillRuntime.cs',[
('public virtual bool IsUnlocked => SkillUnlockService.IsUnlocked(this);','public AR.ARCombatContext ARContext => GetComponent<AR.ARCombatContext>();\n        public float WorldScale => ARContext!=null?ARContext.scale:1;\n        public bool SessionPaused => ARContext!=null&&ARContext.Paused;\n        protected float SessionNow => ARContext!=null?ARContext.Now:Time.time;\n        public virtual bool IsUnlocked => ARContext!=null&&ARContext.allUnlocked&&System.Array.IndexOf(AR.GestureSkillMapper.Ids,Id)>=0 || SkillUnlockService.IsUnlocked(this);'),
('Progression.LocalTelemetry.Skill(Id);','if(ARContext==null)Progression.LocalTelemetry.Skill(Id);'),
('UI.TutorialDirector.SkillUsed(Id);','if(ARContext==null)UI.TutorialDirector.SkillUsed(Id);')])
edit(core+'Set1SkillRuntime.cs',[
('Time.time','SessionNow'),
('isActiveAndEnabled&&IsUnlocked&&health','isActiveAndEnabled&&!SessionPaused&&IsUnlocked&&health'),
('(input==null||input.Allowed)','(ARContext!=null||input==null||input.Allowed)'),
('Range+.2f','Range+.2f*WorldScale'),
('transform.position+Vector3.up,destination+Vector3.up*.3f','transform.position+Vector3.up*WorldScale,destination+Vector3.up*(.3f*WorldScale)'),
('protected virtual void Update()\n        {','protected virtual void Update()\n        {\n            if(SessionPaused)return;'),
('info.point=victim.transform.position+Vector3.up;','info.point=victim.transform.position+Vector3.up*WorldScale;'),
('Vector3 delta=victim.transform.position-center;','Vector3 delta=victim.transform.position-center;radius*=WorldScale;'),
('Mathf.Abs(delta.y)<3','Mathf.Abs(delta.y)<3*WorldScale'),
('center+Vector3.up*.8f,victim.transform.position+Vector3.up,','center+Vector3.up*(.8f*WorldScale),victim.transform.position+Vector3.up*WorldScale,')])
edit(core+'SkillSpirit.cs',[(
'return loadout != null ? loadout.Find(skillId) : null;',
'if(owner.GetComponent<AR.ARCombatContext>()!=null)foreach(var skill in owner.GetComponents<SkillRuntime>())if(skill.Id==skillId)return skill;\n            return loadout != null ? loadout.Find(skillId) : null;')])
edit(core+'GroundAimIndicator.cs', [('public void Hide(){','public void SetExternal(Vector3 point,bool valid){Point=point;Valid=valid;if(indicator!=null){indicator.Position=point;indicator.Opacity=valid?1:0;}}\n        public void Hide(){')])
edit('Assets/ARRift/Runtime/GestureRecognizerBridge.cs',[('public bool Busy=>bridge==null||bridge.Call<bool>("isBusy");','public bool Busy=>bridge==null||!results.IsEmpty||bridge.Call<bool>("isBusy");')])
for name,folder,number in [('IceSeal','IceSeal',10),('ChainLightning','ChainLightning',25),('BlackHole','BlackHole',15),('SwordRain','SwordRain',18)]:
    edit(f'Assets/Skills/{folder}/Runtime/{name}Runtime.cs',[(f'Range=>{number};',f'Range=>{number}*WorldScale;')])
edit('Assets/Skills/ChainLightning/Runtime/ChainLightningRuntime.cs',[
('Vector3.up*1.5f','Vector3.up*(1.5f*WorldScale)'),('Vector3.up*1.7f+transform.right*.5f','(Vector3.up*1.7f+transform.right*.5f)*WorldScale'),
('m.transform.position+Vector3.up,','m.transform.position+Vector3.up*WorldScale,'),('m.transform.position+Vector3.up):','m.transform.position+Vector3.up*WorldScale):'),
('if(viewport.z<=0)continue;float s=', 'if(ARContext==null&&viewport.z<=0)continue;float s='),
('var e=m.GetComponent<EnemyInstance>();if(e!=null','if(ARContext!=null)s=(m.transform.position-point).sqrMagnitude;\n                var e=m.GetComponent<EnemyInstance>();if(ARContext==null&&e!=null'),
('if(elapsed>=nextAt&&bounce<','while(elapsed>=nextAt&&bounce<'),('nextAt=elapsed+.08f','nextAt+=.08f'),
('Vector3.up*1.6f','Vector3.up*(1.6f*WorldScale)'),
('Vector3.up*(fork==0?.85f:-.65f)+(fork==0?Vector3.left:Vector3.right)*.7f','(Vector3.up*(fork==0?.85f:-.65f)+(fork==0?Vector3.left:Vector3.right)*.7f)*WorldScale'),
('Vector3.up*.6f+(branch==0?Vector3.left:Vector3.right)*.65f','(Vector3.up*.6f+(branch==0?Vector3.left:Vector3.right)*.65f)*WorldScale'),
('Mathf.Max(.8f,renderer.bounds.size.y*.75f)','Mathf.Max(.8f,renderer.bounds.size.y*.75f/WorldScale)'),
('float nearest=64;','float nearest=64*WorldScale*WorldScale;')])
edit('Assets/Skills/IceSeal/Runtime/IceSealRuntime.cs',[
('Register(transform.position,10,','Register(transform.position,10*WorldScale,'),
('transform.position+direction*5','transform.position+direction*(5*WorldScale)'),('float range=2+row*1.8f','float range=(2+row*1.8f)*WorldScale'),
('Vector3.up*.6f','Vector3.up*(.6f*WorldScale)'),('Vector3.up,Accent','Vector3.up*WorldScale,Accent'),
('Mathf.Max(.9f,renderer.bounds.size.y*.9f)','Mathf.Max(.9f,renderer.bounds.size.y*.9f/WorldScale)')])
edit('Assets/Skills/SwordRain/Runtime/SwordRainRuntime.cs',[
('Register(point,8,','Register(point,8*WorldScale,'),('Vector3.forward*4+Vector3.up*3.7f','(Vector3.forward*4+Vector3.up*3.7f)*WorldScale'),
('Math.Sqrt(', 'Math.Sqrt(') if False else ('*Mathf.PI*2;float r=Mathf.Sqrt((float)rng.NextDouble())*8;','*Mathf.PI*2;float r=Mathf.Sqrt((float)rng.NextDouble())*8*WorldScale;'),
('destination+Vector3.up*2,Vector3.down,out floor,4,','destination+Vector3.up*(2*WorldScale),Vector3.down,out floor,4*WorldScale,'),
('Vector3.up*.2f','Vector3.up*(.2f*WorldScale)'),('Vector3.up*.4f','Vector3.up*(.4f*WorldScale)'),
('int total=Mastered?50:30;','int total=Mastered?50:30;')])
# Per-session resource/combo clocks, preserving normal defaults.
for name in ['SpiritPower','GenerationChainTracker','ReactionResolver','ReactionFeedback']:
    path=f'Assets/Combat/Runtime/{name}.cs';f=Path(path);s=f.read_text(encoding='utf-8-sig')
    s=s.replace('Time.time','SessionNow')
    marker='public sealed class '+name
    start=s.index('{',s.index(marker))+1
    s=s[:start]+'\n        AR.ARCombatContext ar;float SessionNow=>ar!=null?ar.Now:Time.time;float Scale=>ar!=null?ar.scale:1;\n'+s[start:]
    if 'void Awake() {' in s:s=s.replace('void Awake() {','void Awake() { ar=GetComponent<AR.ARCombatContext>();',1)
    else:s=s.replace('void Awake()\n        {','void Awake()\n        {\n            ar=GetComponent<AR.ARCombatContext>();',1)
    s=s.replace('void Update()\n        {','void Update()\n        {\n            if(ar!=null&&ar.Paused)return;',1)
    s=s.replace('void LateUpdate()\n        {','void LateUpdate()\n        {\n            if(ar!=null&&ar.Paused)return;',1)
    if name=='SpiritPower':s=s.replace('if (ui != null && ui.State','if (ar == null && ui != null && ui.State')
    if name=='ReactionResolver':
        s=s.replace('receiver.readyAt[(int)type]=SessionNow','receiver.readyAt[(int)type]=receiver.SessionNow')
        s=s.replace('Mathf.Abs(delta.y) > 3','Mathf.Abs(delta.y) > 3*Scale').replace('rule.radius * rule.radius','rule.radius * rule.radius * Scale * Scale')
        s=s.replace(' + Vector3.up', ' + Vector3.up*Scale')
    if name=='ReactionFeedback':
        s=s.replace('source.minDistance=4;source.maxDistance=32','source.minDistance=4*Scale;source.maxDistance=32*Scale')
        s=s.replace('Vector3.up*1.6f','Vector3.up*(1.6f*Scale)').replace('ev.point+Vector3.up;','ev.point+Vector3.up*Scale;')
        s=s.replace('new Vector3(Mathf.Cos(a)*rule.radius,.7f,Mathf.Sin(a)*rule.radius)','new Vector3(Mathf.Cos(a)*rule.radius,.7f,Mathf.Sin(a)*rule.radius)*Scale')
        s=s.replace('Vector3.up*(.15f+i*.55f)','Vector3.up*((.15f+i*.55f)*Scale)').replace('Mathf.Clamp01((SessionNow-t.born)/.4f))','Mathf.Clamp01((SessionNow-t.born)/.4f))*Scale')
        s=s.replace('bool gameplay=UIStateManager.Instance!=null&&','bool gameplay=ar!=null||UIStateManager.Instance!=null&&').replace('Vector3.up*2.8f','Vector3.up*(2.8f*Scale)')
    f.write_text(s,encoding='utf-8')
edit('Assets/Combat/Runtime/FlyingSword.cs',[
('rainStart=point+Vector3.up*6.5f','rainStart=point+Vector3.up*(6.5f*caster.WorldScale)'),
('trail.widthMultiplier=CampusRift.Skills.SkillVfxPool.MobileQuality?.14f:.22f;','trail.widthMultiplier=(CampusRift.Skills.SkillVfxPool.MobileQuality?.14f:.22f)*caster.WorldScale;'),
('void UpdateRain(float dt)\n        {','void UpdateRain(float dt)\n        {\n            if(rainOwner!=null&&rainOwner.SessionPaused)return;')])
print('M5 shared runtime patch applied')

from pathlib import Path
import shutil
def edit(p, a, b):
    p=Path(p);s=p.read_text(encoding='utf-8');assert a in s,p;s=s.replace(a,b);p.write_text(s,encoding='utf-8')
edit('Assets/Controls/Runtime/MobileChasePlayTest.cs',
     'bool stable=objects==',
     'yield return new WaitForEndOfFrame(); // ItemBar swaps layout in Update; Destroy settles at frame end.\n            bool stable=objects==')
edit('Assets/Localization/Runtime/LocalizationPlayTest.cs',
     'IEnumerator Start()\n        {',
     'void Awake(){DontDestroyOnLoad(gameObject);} // Acceptance crosses actual Hub/gameplay scenes.\n        IEnumerator Start()\n        {')
edit('Assets/SkyBeast/Validation/SkyBeastPresencePlayTest.cs',
     'SkyBeastPresence.Begin(10);yield return null;',
     'SkyBeastPresence.Begin(10);FireBreathCycle.BeginLevel(10);FireBreathCycle.Instance.AutoAdvance=false;yield return null; // Same initialization contract as LevelDirector.Begin (P13/P14).')
edit('Assets/Levels/Validation/LevelFlowPlayTest.cs',
     'Mathf.Approximately(first.MaxHealth, 60) && first.scaling.aiTier == 0, "Level 1 monsters have base health 60 and AI tier 0"',
     'Mathf.Approximately(first.MaxHealth, first.archetype.baseHealth*level1.healthMultiplier) && first.scaling.aiTier == level1.aiTier, "P12 level1 health and AI match the authored archetype and scaling"')
edit('Assets/Levels/Validation/LevelFlowPlayTest.cs',
     'director.Kills == 32 && director.SpawnedTotal == 32, "All 32 monsters are spawned once and killed (" + director.Kills + "/" + director.SpawnedTotal + ")"',
     'director.Kills == 32 && killEvents==32 && director.TotalPlanned==32 && director.Remaining==0 && director.SpawnedTotal>=32, "P19 all32 counted wave enemies killed once; total spawns include uncounted summoned minions (" + director.Kills + "/" + director.SpawnedTotal + ")"')
edit('Assets/Levels/Validation/LevelFlowPlayTest.cs',
     'yield return Frames(2);\n            Check(director.State == LevelDirector.Phase.Won && wonEvents == 1, "Level8 wins',
     'director.CompleteSkySword(); // HeavenSwordUltimate performs this after its cinematic (P15/P16).\n            yield return Frames(2);\n            Check(director.State == LevelDirector.Phase.Won && wonEvents == 1, "Level8 wins')
edit('Assets/Skills/Core/Validation/SkillSet1EdgePlayTest.cs',
     'world.player.cameraHeight=.8f;world.Look(90,0);',
     'world.player.cameraHeight=.8f;Place(0,world.origin-Vector3.right*2);world.Look(90,0); // Camera crosses a stationary rear target before dash knockback can displace it.')
p=Path('Assets/Editor/StabilizeLiftSmoke.cs');s=p.read_text(encoding='utf-8')
s=s.replace('string result=ShabanHunterValidation.Lifts();', 'var validator=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ShabanHunterValidation")).First(t=>t!=null);\n        string result=(string)validator.GetMethod("Lifts",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static).Invoke(null,null);')
s=s.replace('using System.IO;', 'using System.IO;\nusing System.Linq;')
dest=Path('Assets/Levels/Validation/StabilizeLiftSmoke.cs');dest.write_text(s,encoding='utf-8')
shutil.move(str(p)+'.meta',str(dest)+'.meta');p.unlink()
p=Path('Tools/StabilizePolishCapture.cs.txt');s=p.read_text(encoding='utf-8').replace('using CampusRift.Skills;', 'using CampusRift.Skills;\nusing CampusRift.Progression;').replace('FindObjectsByType<TMPro.TMP_Text>()','FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None)')
Path('Assets/Levels/Validation/StabilizePolishCapture.cs').write_text(s,encoding='utf-8')

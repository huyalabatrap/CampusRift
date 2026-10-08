from pathlib import Path
root=Path(__file__).resolve().parents[1]
def edit(name,pairs):
 p=root/name;s=p.read_text(encoding='utf-8-sig')
 for a,b in pairs:
  assert a in s,(name,a[:80]);s=s.replace(a,b)
 p.write_text(s,encoding='utf-8')

edit('Assets/CampusRiftUI/Validation/SkyVictoryPlayTest.cs',[
 ('State.OpenHub();yield return null;State.OpenSettings();','if(State.State==UIState.Hub)State.Back();yield return null;State.OpenSettings();'),
 ('P09 Hub exposes the same saved sky setting','P09 Hub → Main Menu exposes the saved sky setting')])

edit('Assets/Levels/Validation/LevelFlowPlayTest.cs',[
 ('Check(aiConfig != null && !aiConfig.enableTimeEscalation, "Time escalation is off by default in V2");',
  'var bossProbe=EnemyPool.Ensure().Spawn(catalog.Get(5).bosses[0],player.transform.position+Vector3.forward*5,catalog.Get(5).Scaling);\n            Check(bossProbe!=null && !bossProbe.GetComponent<ShabanEnemyBridge>().RuntimeConfig.enableTimeEscalation, "P12 level Shaban runtime disables escalation without changing sandbox config");\n            if(bossProbe!=null)EnemyPool.Instance.Release(bossProbe);'),
 ('any.archetype.baseHealth * 6.4f','any.archetype.baseHealth * level8.healthMultiplier * any.archetype.lateHealthMultiplier * (any.Elite!=null&&any.Elite.IsElite?3:1)'),
 ('any.archetype.baseDamage * 5.8f','any.archetype.baseDamage * level8.damageMultiplier'),
 ('any.scaling.aiTier == 3','any.scaling.aiTier == level8.aiTier'),
 ('Level 8 monsters carry ×6.4 health, ×5.8 damage and AI tier 3','P12/P19 Level 8 monsters match authored scaling, late health and elite HP'),
 ('Check(director.State == LevelDirector.Phase.Won && wonEvents == 1, "The single-wave level is won once");',
  'Check(director.AwaitingSkySword && wonEvents==0,"P14/P16 cleared wave waits for Heaven Sword before victory");\n            var sky=CampusRift.SkyBeast.SkyBeastScheduler.Instance;sky.CinematicPaused=true;\n            while(!sky.Completed){Check(sky.ApplySkySwordHit(),"Level8 successful Heaven Sword segment");yield return null;}\n            yield return Frames(2);\n            Check(director.State == LevelDirector.Phase.Won && wonEvents == 1, "Level8 wins exactly once after wave and sky guardian clear");')])

edit('Assets/Enemies/Editor/EnemiesSetup.cs',[
 ('var ids = new HashSet<string>();','var ids = new Dictionary<string,EnemyArchetype>();'),
 ('else if (!ids.Add(a.id)) errors.Add(a.id + ": duplicate id");',
  'else if(ids.TryGetValue(a.id,out var canonical)) {\n                // P12 summon Resources alias is deliberate; all other duplicate ids are errors.\n                string path=AssetDatabase.GUIDToAssetPath(guid), prior=AssetDatabase.GetAssetPath(canonical);\n                bool summonAlias=a.id=="tieu-yeu" && new[]{path,prior}.Contains(Root+"Resources/P12/TieuYeu.asset") && new[]{path,prior}.Contains(Root+"Data/tieu-yeu.asset") && a.prefab==canonical.prefab;\n                if(!summonAlias)errors.Add(a.id+": duplicate id");\n            } else ids.Add(a.id,a);'),
 ('if (a.prefab.GetComponent<EnemyInstance>() == null || a.prefab.GetComponent<MinionBrain>() == null || a.prefab.GetComponent<MinionMotor>() == null) errors.Add(a.id + ": prefab lacks EnemyInstance/MinionBrain/MinionMotor");',
  'bool shaban=a.prefab.GetComponent<ShabanEnemyBridge>()!=null;\n                bool motor=shaban?a.prefab.GetComponent<CampusRift.Monsters.MonsterBrain>()!=null && a.prefab.GetComponent<CampusRift.Monsters.MonsterNavigation>()!=null:a.prefab.GetComponent<MinionBrain>()!=null && a.prefab.GetComponent<MinionMotor>()!=null;\n                if(a.prefab.GetComponent<EnemyInstance>()==null || !motor)errors.Add(a.id+": prefab lacks its minion or P12 Shaban bridge runtime");')])

edit('Assets/SkyBeast/Validation/SkyBeastPresencePlayTest.cs',[
 ('level==9?"026":"020"','level==9?"026":"023"'),
 ('SkyBeastPresence.Begin(10);yield return new WaitForSeconds(71);\n            var dragons=FindObjectsByType<SkyBeastController>();Check(dragons.Length==3&&dragons.Select(c=>c.definition.id).Distinct().Count()==3,"level10 timed three-beast return at 0/35/70 seconds");',
  'SkyBeastPresence.Begin(10);yield return null;\n            var scheduler=SkyBeastScheduler.Instance;\n            Check(scheduler.Beasts.Count==2 && scheduler.Beasts[0].definition.id=="023" && scheduler.Beasts[1].definition.id=="026","P14/P16 level10 begins with 023 and 026");\n            scheduler.CinematicPaused=true;Check(scheduler.ApplySkySwordHit(),"first sky sword hit accepted");yield return null;\n            Check(scheduler.Phase==2 && scheduler.Beasts.Count==1 && scheduler.Beasts[0].definition.id=="026","first sword advances to 026 phase2");\n            Check(scheduler.ApplySkySwordHit(),"second sky sword hit accepted");yield return null;\n            Check(scheduler.Phase==3 && scheduler.Beasts.Count==1 && scheduler.Beasts[0].definition.id=="020","second sword advances to Long Vuong020 phase3");\n            Check(!scheduler.SwordAllowed,"Long No gates final sword rather than a timed dragon return");')])
print('Updated level/menu/sky/enemy validator contracts to P09/P12/P14/P16/P19.')

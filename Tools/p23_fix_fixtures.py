"""Apply P23 QA fixture corrections only while the Editor is idle."""
from pathlib import Path
import shutil

if 'int VisibleEnemies()' in Path('Assets/CampusRiftUI/Validation/P23Performance.cs').read_text(encoding='utf-8'):
 raise RuntimeError('This one-time fixture patch is already applied. Do not replay it.')

p=Path('Assets/Learning/Validation/LearningPlayTest.cs')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('State.EnterScene(true);\n            var run = Run();', 'State.EnterScene(true);\n            // LevelHUD is authored by LevelDirector; a bare sandbox has no level body.\n            var levelFixture = CampusRift.Levels.LevelDirector.Ensure();\n            levelFixture.Begin(CampusRift.Levels.LevelCatalog.Instance.Get(1));\n            levelFixture.enabled = false;\n            var run = Run();')
p.write_text(s,encoding='utf-8')

p=Path('Assets/CampusRiftUI/Validation/P23Performance.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('using CampusRift.SkyBeast;', 'using CampusRift.SkyBeast;\nusing CampusRift.Combat;\nusing UnityEngine.AI;')
s=s.replace('public int frames,enemies,particles,swords;', 'public int frames,enemies,visibleEnemies,particles,swords;public Vector3 camera,focus;')
s=s.replace('public string error;', 'public string error;public int stressSpawned,stressArchetypes,stressElites;')
s=s.replace('int cap,sync;', 'int cap,sync;Vector3 focus;')
s=s.replace('enemies=d.Alive.Count,particles=', 'enemies=EnemyPool.Instance.ActiveCount,visibleEnemies=VisibleEnemies(),camera=world.camera.transform.position,focus=focus,particles=')
s=s.replace('world.player.enabled=false;', 'world.player.enabled=false;world.player.followCamera=null;')
start=s.index('            d=LevelDirector.Ensure();')
end=s.index('            d.Begin(LevelCatalog.Instance.Get(8));',start)
s=s[:start]+'''            d=LevelDirector.Ensure();d.introSeconds=.01f;d.spawnInterval=.005f;d.portalLead=0;
            var level=LevelCatalog.Instance.Get(10);d.Begin(level);d.enabled=false;
            focus=FindOpenCourt(level.spawnPoint);world.PlacePlayer(focus);
            var roster=level.spawnTable.roster.Select(r=>r.archetype).Where(a=>a!=null).Distinct().ToArray();
            var types=new HashSet<EnemyArchetype>();
            for(int i=0;i<30;i++)
            {
                float angle=i*137.5f*Mathf.Deg2Rad;float radius=3.5f+(i%3)*1.25f;
                var point=focus+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                var enemy=EnemyPool.Ensure().Spawn(roster[i%roster.Length],point,level.Scaling,false);
                if(enemy==null)throw new Exception("Stress spawn failed at "+point);
                types.Add(enemy.archetype);report.stressSpawned++;
                if(i<2){enemy.Elite.Configure(10,2304+i,i==0?EliteAffixKind.FireHeart:EliteAffixKind.Guardian);report.stressElites++;}
            }
            report.stressArchetypes=types.Count;FrameCourt(focus);
            yield return null;if(VisibleEnemies()<20)throw new Exception("Crowd view is occluded: "+VisibleEnemies()+" visible");
            report.method="Each scenario: one 1s warmup + 5s unscaled sample, no balance bot. Crowd is a controlled 30-monster stress fixture (above normal cap14), full current roster and 2 elites on existing outdoor NavMesh; AI active, player invulnerable. Fire uses real VFX, no manual damage ticks. Cinematic held at gather.";
            yield return Measure("level10-crowd");d.End();
''' + s[end:]
s=s.replace('world.PlacePlayer(world.origin);world.camera.transform.position=world.origin+new Vector3(-3,2,-3);world.camera.transform.LookAt(world.origin+Vector3.up);', 'world.PlacePlayer(focus);FrameCourt(focus);')
s=s.replace('d.End();d.Begin(LevelCatalog.Instance.Get(8));d.enabled=true;deadline=', 'd.End();d.Begin(LevelCatalog.Instance.Get(8));d.enabled=true;float deadline=')
index=s.index('        void Write()')
s=s[:index]+'''        int VisibleEnemies()
        {
            var planes=GeometryUtility.CalculateFrustumPlanes(world.camera);int n=0;
            foreach(var e in EnemyDirector.Ensure().Active)
            {
                if(e==null||!e.Alive)continue;var renderer=e.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.enabled&&GeometryUtility.TestPlanesAABB(planes,r.bounds));
                if(renderer!=null&&CombatLine.Clear(world.camera.transform.position,renderer.bounds.center,world.player.transform))n++;
            }
            return n;
        }
        Vector3 FindOpenCourt(Vector3 preferred)
        {
            for(int step=0;step<40;step++)
            {
                var candidate=preferred+new Vector3((step%5-2)*5,0,-(step/5)*5);
                if(!NavMesh.SamplePosition(candidate,out var hit,.5f,NavMesh.AllAreas)||ShelterDetector.AtFeet(hit.position)!=Shelter.Outdoor)continue;
                bool clear=true;
                for(int i=0;i<16;i++){var offset=Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*7;var edge=hit.position+offset;
                    if(!NavMesh.SamplePosition(edge,out var h,.5f,NavMesh.AllAreas)||!CombatLine.Clear(hit.position+Vector3.up,edge+Vector3.up,world.player.transform)){clear=false;break;}}
                if(clear)return hit.position;
            }
            throw new Exception("No clear outdoor stress court on existing NavMesh");
        }
        void FrameCourt(Vector3 target)
        {
            for(int i=0;i<16;i++)
            {
                var position=target+Quaternion.Euler(0,i*22.5f,0)*new Vector3(0,7,-12);
                if(!CombatLine.Clear(position,target+Vector3.up,world.player.transform))continue;
                world.camera.transform.position=position;world.camera.transform.LookAt(target+Vector3.up);world.camera.fieldOfView=58;return;
            }
            throw new Exception("No unoccluded stress camera");
        }
''' + s[index:]
p.write_text(s,encoding='utf-8')

diagnostic=Path('task/p23/diagnostics/performance-occluded')
if not diagnostic.exists():shutil.copytree('task/p23/perf-editor',diagnostic)
print('Learning current-LevelHUD initialization and visible full-roster stress fixture corrected; original samples preserved.')

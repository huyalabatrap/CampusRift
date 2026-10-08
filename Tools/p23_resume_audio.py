"""Complete interrupted audio assertions and retain the pre-audio LevelFlow pass."""
from pathlib import Path
import json, shutil

root = Path.cwd()
backup = root / 'Backups/P23-resume-20261004'
paths = ['Assets/Levels/Validation/LevelFlowPlayTest.cs',
         'Assets/Levels/Validation/Level8to10PlayTest.cs',
         'Tools/p23_regressions.py', 'task/polish-notes.md',
         'task/P23-phat-hanh-day-du.md', 'task/README.md']
for rel in paths:
    dest = backup / rel
    dest.parent.mkdir(parents=True, exist_ok=True)
    if not dest.exists(): shutil.copy2(root / rel, dest)

p = root / paths[0]
s = p.read_text(encoding='utf-8-sig')
old = '''            Check(director.AliveCount >= 9 && audible == distinct && audible <= 3, director.AliveCount + " monsters make only " + audible + " audible growls (one per clip, " + distinct + " clips drawn)");'''
assert old in s
s = s.replace(old, '''            Check(director.AliveCount >= 9 && audible == 1 && distinct == 1, director.AliveCount + " monsters share exactly one clip and one audible voice");
            CheckSmallVoices(8, "Level8 crowd");
            var summoned = EnemyPool.Ensure().Spawn(catalog.Get(1).waves[0].entries[0].archetype, player.transform.position + Vector3.forward * 4, level8.Scaling, false);
            Check(summoned != null && !summoned.countsForSwordIntent && summoned.VoiceClip == EnemyDirector.Instance.SmallMonsterVoice, "Summoned minion inherits the level8 clip");
            EnemyDirector.Instance.UpdateVoices(); CheckSmallVoices(8, "Level8 with summoned minion");
            EnemyPool.Instance.Release(summoned);''')
needle = '            // ---- Level 1 ----'
assert needle in s
s = s.replace(needle, '''            var voices = EnemyDirector.Ensure();
            var tower = EndgameFactory.Tower(13, DateTime.UtcNow); voices.BeginLevelVoice(tower);
            Check(voices.SmallMonsterSourceLevel == 3 && voices.SmallMonsterVoice == GameSfx.SmallMonsterForLevel(3), "Tower13 uses source level3 growl");
            var nightmare = EndgameFactory.Nightmare(8); voices.BeginLevelVoice(nightmare);
            Check(voices.SmallMonsterSourceLevel == 8 && voices.SmallMonsterVoice == GameSfx.SmallMonsterForLevel(8), "Nightmare8 uses source level8 growl");
            Destroy(tower); Destroy(nightmare);

''' + needle)
p.write_text(s, encoding='utf-8')

p = root / paths[1]
s = p.read_text(encoding='utf-8-sig')
s = s.replace('int swords=0;', 'int swords=0;var voiceClips=new HashSet<string>();bool nearestOnly=true;int voiceSamples=0;')
needle = '                    foreach(var e in director.Alive)if(e!=null)roster.Add(e.archetype.id);Hold();V2DevTools.KillCurrentWave();'
assert needle in s
s = s.replace(needle, '''                    EnemyDirector.Instance.UpdateVoices();
                    var voiced=EnemyDirector.Instance.Active.Where(e=>e!=null&&e.Alive&&e.Voice!=null&&e.VoiceClip!=null).ToArray();
                    if(voiced.Length>0)
                    {
                        voiceSamples++;foreach(var e in voiced)voiceClips.Add(e.VoiceClip.name);
                        var nearest=voiced.OrderBy(e=>(e.transform.position-world.player.transform.position).sqrMagnitude).First();
                        var audible=voiced.Where(e=>e.Voice.isPlaying&&!e.Voice.mute).ToArray();
                        nearestOnly &= audible.Length==1&&audible[0]==nearest;
                    }
''' + needle)
needle = '                Check(director.State==LevelDirector.Phase.Won&&director.Kills=='
assert needle in s
s = s.replace(needle, '''                Check(voiceSamples>0&&voiceClips.Count==1&&voiceClips.Contains("quai-nho-"+((level-1)%3+1)), "L"+level+" every observed ordinary monster uses one stable clip");
                Check(voiceSamples>0&&nearestOnly,"L"+level+" exactly one ordinary voice is audible at its nearest monster");
''' + needle)
p.write_text(s, encoding='utf-8')

out = root / 'Artifacts/V2/P23-regression'
rows = json.loads((out / 'Summary.json').read_text(encoding='utf-8'))
old = next(r for r in rows if r['name']=='LevelFlow')
dest = root / 'task/p23/diagnostics/level-flow-before-audio'
assert not dest.exists()
shutil.copytree(out / 'runs/LevelFlow', dest)
(dest / 'original-summary-row.json').write_text(json.dumps(old,ensure_ascii=False,indent=2),encoding='utf-8')
(out / 'Summary.json').write_text(json.dumps([r for r in rows if r['name']!='LevelFlow'],ensure_ascii=False,indent=2),encoding='utf-8')
with (root / 'task/p23/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## Mốc 9 — tiếp quản và hoàn thiện assertion âm thanh\n- Đọc brief/resume/context và trạng thái đĩa; Android/Edit/SampleScene idle, chưa có build P23. Giữ LevelFlow59/0 trước audio ở diagnostics/level-flow-before-audio. Hoàn thiện assertion Level8 một clip/một voice, summon và nguồn Tháp/Ác Mộng; Level8to10 quan sát clip/nearest ở cả10màn. Runtime chọn clip theo màn và DSP phase đã có trên đĩa. Backup bổ sung P23-resume-20261004; bắt đầu đúng hai suite sau audio một lượt.\n')
print('Audio assertions completed; old pass preserved')

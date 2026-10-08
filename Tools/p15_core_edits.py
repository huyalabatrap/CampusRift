from pathlib import Path
p=Path('Assets/Levels/Runtime/LevelDirector.cs')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('public float RestRemaining { get; private set; }','public bool AwaitingSkySword { get; private set; }\n        public bool CinematicPaused { get; set; }\n        public float RestRemaining { get; private set; }')
s=s.replace('Level = definition; State = Phase.Intro;', 'AwaitingSkySword = CinematicPaused = false;\n            Level = definition; State = Phase.Intro;')
s=s.replace('Subscribe();\n            phaseUntil', '''if(definition.index>=8 && definition.index<=10)
            {
                var intent=GetComponent<SkyBeast.SwordIntent>()??gameObject.AddComponent<SkyBeast.SwordIntent>();intent.Consume();
                SkyBeast.HeavenSwordUltimate.Ensure(player.gameObject).Begin(this,intent);
            }
            else SkyBeast.HeavenSwordUltimate.Instance?.StopUltimate();
            Subscribe();
            phaseUntil''')
s=s.replace('if (!Gameplaying()) return;', 'if (!Gameplaying() || CinematicPaused || AwaitingSkySword) return;')
s=s.replace('nextSpawn = Time.time + 0.25f;', '''var intent=GetComponent<SkyBeast.SwordIntent>();
            if(Level.index>=8 && intent!=null){int weight=0;foreach(var r in queue)weight+=r.archetype.swordIntentWeight;intent.BeginWave(weight,queue.Count);}
            nextSpawn = Time.time + 0.25f;''')
s=s.replace('if (enemy == null) { TotalPlanned = Mathf.Max(0, TotalPlanned - 1); return; }', 'if (enemy == null) { TotalPlanned = Mathf.Max(0, TotalPlanned - 1); GetComponent<SkyBeast.SwordIntent>()?.SpawnFailed(request.archetype.swordIntentWeight); return; }\n            if(Level.index>=8)GetComponent<SkyBeast.SwordIntent>()?.Track(enemy);')
s=s.replace('public void TrackSummoned(EnemyInstance enemy)', '''public void ForgetSummoned(EnemyInstance enemy){if(alive.Remove(enemy)){TotalPlanned=Mathf.Max(Kills,TotalPlanned-1);}}
        public void TrackSummoned(EnemyInstance enemy)''')
s=s.replace('if (queue.Count == 0 && pending.Count == 0 && alive.Count == 0) WaveCleared();','if (queue.Count == 0 && pending.Count == 0 && CountedAlive()==0) WaveCleared();')
s=s.replace('void WaveCleared()\n', '''int CountedAlive(){if(Level.index<8)return alive.Count;int count=0;foreach(var e in alive)if(e!=null&&e.countsForSwordIntent)count++;return count;}
        void WaveCleared()
''')
s=s.replace('if (WaveIndex + 1 >= Level.waves.Count) { CheckWin(); return; }','''if(Level.index>=8 && SkyBeast.SkyBeastScheduler.Instance!=null && !SkyBeast.SkyBeastScheduler.Instance.Completed)
            {AwaitingSkySword=true;GetComponent<SkyBeast.SwordIntent>()?.MarkWaveCleared();return;}
            if (WaveIndex + 1 >= Level.waves.Count) { CheckWin(); return; }''')
pos='        void CheckWin()'
s=s.replace(pos,'''        public void CompleteSkySword()
        {
            AwaitingSkySword=false;
            if(SkyBeast.SkyBeastScheduler.Instance!=null && SkyBeast.SkyBeastScheduler.Instance.Completed){Win();return;}
            State=Phase.Rest;RestRemaining=SkyBeast.HeavenSwordConfig.Load().restSeconds;phaseUntil=Time.time+RestRemaining;
            if(spirit!=null)spirit.Restore(spirit.Max*.2f);
        }
        public void Win(){if(Level!=null && State!=Phase.Lost)CheckWin();}

'''+pos)
s=s.replace('Unsubscribe(); State = Phase.Idle;', 'SkyBeast.HeavenSwordUltimate.Instance?.StopUltimate();AwaitingSkySword=CinematicPaused=false;\n            Unsubscribe(); State = Phase.Idle;')
p.write_text(s,encoding='utf-8')
p=Path('Assets/Controls/Runtime/CampusInput.cs');s=p.read_text(encoding='utf-8-sig').replace('public bool Allowed =>','public bool UltimateLocked {get;set;}\n        public bool Allowed => !UltimateLocked &&');p.write_text(s,encoding='utf-8')
p=Path('Assets/Progression/Runtime/ProfileData.cs');s=p.read_text(encoding='utf-8-sig').replace('public List<string> seenReactions', 'public bool heavenSwordSeen;\n        public List<string> seenReactions');p.write_text(s,encoding='utf-8')

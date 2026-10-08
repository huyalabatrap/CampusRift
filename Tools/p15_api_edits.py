from pathlib import Path
p=Path('Assets/SkyBeast/Runtime/FireBreathCycle.cs');s=p.read_text(encoding='utf-8-sig').replace('public bool AutoAdvance=true;', 'public bool AutoAdvance=true;\n        public bool CinematicPaused {get;set;}\n        float swordRest;\n        public void PostSwordRest(float seconds){Ground.Clear();IsFury=false;swordRest=seconds;Enter(Phase.Rest);}')
s=s.replace('if(!AutoAdvance||State==Phase.Disabled)', 'if(CinematicPaused||!AutoAdvance||State==Phase.Disabled)')
s=s.replace('Mathf.Max(0,Profile.cycleSeconds-', '(swordRest>0?swordRest:Mathf.Max(0,Profile.cycleSeconds-').replace('Profile.afterfireSeconds);\n        public bool StartFury()', 'Profile.afterfireSeconds));\n        public bool StartFury()')
s=s.replace('int guard=0;','if(CinematicPaused)return;\n            int guard=0;').replace('State=phase;elapsed=0;', 'if(phase==Phase.Warning)swordRest=0;\n            State=phase;elapsed=0;').replace('IsFury=false;elapsed=0;Ground?.Clear();','IsFury=false;elapsed=0;swordRest=0;CinematicPaused=false;Ground?.Clear();')
p.write_text(s,encoding='utf-8')
p=Path('Assets/SkyBeast/Runtime/SkyBeastScheduler.cs');s=p.read_text(encoding='utf-8-sig').replace('public bool Completed{','public bool CinematicPaused {get;set;}\n        public bool Completed{').replace('&&!cycle.IsBreathing&&','&&(!cycle.IsBreathing||CinematicPaused)&&')
s=s.replace('dead.gameObject.SetActive(false);Destroy(dead.definition);Destroy(dead.gameObject,1);','if(CinematicPaused){dead.BeginSwordDeath();Destroy(dead.definition,4);Destroy(dead.gameObject,4);}else{dead.gameObject.SetActive(false);Destroy(dead.definition);Destroy(dead.gameObject,1);}')
p.write_text(s,encoding='utf-8')
p=Path('Assets/SkyBeast/Runtime/SkyBeastController.cs');s=p.read_text(encoding='utf-8-sig').replace('public void HoldCinematic(Vector3 position,Quaternion rotation)', '''float swordFlightSeconds;bool swordHeld;
        public void BeginSwordCinematic(){swordFlightSeconds=Time.time-born;swordHeld=true;cinematic=true;}
        public void EndSwordCinematic(){if(!swordHeld)return;swordHeld=false;cinematic=false;born=Time.time-swordFlightSeconds;lastPosition=transform.position;ReturnToOrbit();}
        public void BeginSwordDeath(){if(pose!=null)StopCoroutine(pose);pose=null;cinematic=true;EndWarning();StartCoroutine(SwordDeath());}
        IEnumerator SwordDeath(){var start=transform.position;var rotation=transform.rotation;var renders=GetComponentsInChildren<Renderer>();var block=new MaterialPropertyBlock();float t=0;
            while(t<2.2f){t+=Time.unscaledDeltaTime;float u=Mathf.Clamp01(t/2.2f);transform.position=start+Vector3.down*(55*u*u);transform.rotation=rotation*Quaternion.Euler(u*38,0,u*55);
                foreach(var r in renders)if(r!=null){r.GetPropertyBlock(block);block.SetFloat("_Dissolve",Mathf.Clamp01((u-.25f)/.75f));r.SetPropertyBlock(block);}yield return null;}gameObject.SetActive(false);}
        public void HoldCinematic(Vector3 position,Quaternion rotation)''')
p.write_text(s,encoding='utf-8')

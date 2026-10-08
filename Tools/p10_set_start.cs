if(!UnityEditor.EditorApplication.isPlaying) return "Play required"; var go=new UnityEngine.GameObject("P10 Skill Set QA"); go.AddComponent<CampusRift.Skills.SkillSet1PlayTest>(); return "started";

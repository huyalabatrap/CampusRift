using UnityEngine;
using CampusRift.Progression;
namespace CampusRift.Skills
{
    [DefaultExecutionOrder(-45),DisallowMultipleComponent]
    public sealed class SkillProgressBridge:MonoBehaviour
    {
        SkillRuntime[] runtimes;ProfileService profile;
        void Awake(){runtimes=GetComponents<SkillRuntime>();profile=ProfileService.Ensure();}
        void OnEnable(){if(profile!=null){profile.Changed+=Apply;Apply();}}
        void Apply(){foreach(var runtime in runtimes)runtime.ApplyRank(profile.Skills.GetRank(runtime.Id));}
        void OnDisable(){if(profile!=null)profile.Changed-=Apply;}
    }
}

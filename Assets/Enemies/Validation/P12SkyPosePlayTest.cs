#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.SkyBeast;
namespace CampusRift.Validation
{
 public sealed class P12SkyPosePlayTest:P12PlayTest
 {
  protected override IEnumerator Run(){Begin();foreach(string id in new[]{"020","023","026"}){var c=SkyBeastPresence.Spawn(id);var animator=c.GetComponentInChildren<Animator>();
   foreach(bool spell in new[]{false,true}){int events=0;System.Action<SkyBeastController> received=x=>events++;c.BreathRequested+=received;var states=new HashSet<string>();float deadline=Time.time+15;
    if(spell)c.RequestBreath();else c.RequestDive();
    while(c.PoseActive&&Time.time<deadline){states.Add(c.AnimationState);yield return null;}
    string prefix=spell?"Spell":"Dive";Check(!c.PoseActive&&states.Contains(prefix+"_Start")&&states.Contains(prefix+"_Loop")&&states.Contains(prefix+(spell?"_End":"_Recover")),id+" "+prefix+" authored three-state pose sequence");
    Check(!animator.applyRootMotion&&(!spell||events==1),id+" "+prefix+" pose keeps root motion off / breath hook once");c.BreathRequested-=received;
   }SkyBeastPresence.StopAll();yield return null;}
  }
 }
}
#endif
